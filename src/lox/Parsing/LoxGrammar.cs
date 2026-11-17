namespace Lox;

/// <summary>
/// The Lox grammar, producing a full-fidelity concrete syntax tree (CST):
/// every token — keywords, brackets, punctuation — appears in the tree, and each
/// token carries its leading whitespace as trivia, so the CST reproduces the
/// source text exactly. Lowering to an AST (dropping structure tokens, assigning
/// roles to children) is the responsibility of <see cref="AstBuilder"/>.
/// </summary>
public static class LoxGrammar
{
    public static GrammarRule Program { get; }

    static LoxGrammar()
    {
        // ----- terminal helpers -----
        // Keywords are scanned as Identifier tokens; "<", ">", "=", "!" (and the
        // "="-suffixed forms) are scanned as Comparison tokens.
        static GrammarRule Kw(string word) => Grammar.Expect(TokenKind.Identifier, word);
        static GrammarRule Op(string symbol) => Grammar.Expect(TokenKind.Operator, symbol);
        static GrammarRule Cmp(string symbol) => Grammar.Expect(TokenKind.Comparison, symbol);
        static GrammarRule Br(string symbol) => Grammar.Expect(TokenKind.Bracket, symbol);
        static GrammarRule Punct(string symbol) => Grammar.Expect(TokenKind.Punctuation, symbol);

        var identifier = Grammar.Expect(TokenKind.Identifier);
        var number = Grammar.Expect(TokenKind.Number);
        var quotedString = Grammar.Expect(TokenKind.QuotedString);

        // ----- forward references, resolved via Lazy -----
        GrammarRule expression = null!;
        GrammarRule assignment = null!;
        GrammarRule unary = null!;
        GrammarRule statement = null!;
        GrammarRule declaration = null!;

        var expressionRef = Grammar.Lazy(() => expression);
        var assignmentRef = Grammar.Lazy(() => assignment);
        var unaryRef = Grammar.Lazy(() => unary);
        var statementRef = Grammar.Lazy(() => statement);
        var declarationRef = Grammar.Lazy(() => declaration);

        // ----- expressions (bottom-up) -----

        // arguments      = expression ( "," expression )* ;
        var arguments = Grammar.SeparatedBy("args", expressionRef, Punct(","));

        // primary        = "true" | "false" | "nil" | "this"
        //                | NUMBER | STRING | IDENTIFIER
        //                | "(" expression ")" | "super" "." IDENTIFIER ;
        var primary = Grammar.Alt(
            Kw("true"),
            Kw("false"),
            Kw("nil"),
            Kw("this"),
            Grammar.Seq("super", Kw("super"), Punct("."), identifier),
            number,
            quotedString,
            identifier,
            Grammar.Seq("group", Br("("), expressionRef, Br(")")));

        // call           = primary ( "(" arguments? ")" | "." IDENTIFIER )* ;
        var call = Grammar.Seq("call",
            primary,
            Grammar.ZeroOrMore(Grammar.Alt(
                Grammar.Seq("invoke", Br("("), Grammar.Opt(arguments), Br(")")),
                Grammar.Seq("member", Punct("."), identifier))));

        // unary          = ( "!" | "-" ) unary | call ;
        unary = Grammar.Alt(
            Grammar.Seq("unary", Grammar.Alt(Cmp("!"), Op("-")), unaryRef),
            call);

        // factor         = unary ( ( "/" | "*" ) unary )* ;
        var factor = Grammar.SeparatedBy("factor", unary, Grammar.Alt(Op("/"), Op("*")));

        // term           = factor ( ( "-" | "+" ) factor )* ;
        var term = Grammar.SeparatedBy("term", factor, Grammar.Alt(Op("-"), Op("+")));

        // comparison     = term ( ( ">" | ">=" | "<" | "<=" ) term )* ;
        var comparison = Grammar.SeparatedBy("comparison", term,
            Grammar.Alt(Cmp(">"), Cmp(">="), Cmp("<"), Cmp("<=")));

        // equality       = comparison ( ( "!=" | "==" ) comparison )* ;
        var equality = Grammar.SeparatedBy("equality", comparison,
            Grammar.Alt(Cmp("!="), Cmp("==")));

        // logic_and      = equality ( "and" equality )* ;
        var logicAnd = Grammar.SeparatedBy("and", equality, Kw("and"));

        // logic_or       = logic_and ( "or" logic_and )* ;
        var logicOr = Grammar.SeparatedBy("or", logicAnd, Kw("or"));

        // assignment     = ( call "." )? IDENTIFIER "=" assignment | logic_or ;
        // Implemented as logic_or ( "=" assignment )? because the greedy call rule
        // already consumes the "." IDENTIFIER chain of an assignment target.
        // Shape: (assign lhs (rhs = value)); collapses to lhs when no "=" follows.
        assignment = Grammar.Seq("assign", logicOr, Grammar.Opt(Grammar.Seq("rhs", Cmp("="), assignmentRef)));

        // expression     = assignment ;
        expression = assignment;

        // ----- statements -----

        // block          = "{" declaration* "}" ;
        var block = Grammar.Seq("block", Br("{"), Grammar.ZeroOrMore(declarationRef), Br("}"));

        // exprStmt       = expression ";" ;
        var exprStmt = Grammar.Seq("exprStmt", expression, Punct(";"));

        // printStmt      = "print" expression ";" ;
        var printStmt = Grammar.Seq("print", Kw("print"), expression, Punct(";"));

        // returnStmt     = "return" expression? ";" ;
        var returnStmt = Grammar.Seq("return", Kw("return"), Grammar.Opt(expression), Punct(";"));

        // whileStmt      = "while" "(" expression ")" statement ;
        var whileStmt = Grammar.Seq("while", Kw("while"), Br("("), expression, Br(")"), statementRef);

        // ifStmt         = "if" "(" expression ")" statement ( "else" statement )? ;
        var ifStmt = Grammar.Seq("if",
            Kw("if"), Br("("), expression, Br(")"), statementRef,
            Grammar.Opt(Grammar.Seq("else", Kw("else"), statementRef)));

        // varDecl        = "var" IDENTIFIER ( "=" expression )? ";" ;
        var varDecl = Grammar.Seq("var",
            Kw("var"), identifier,
            Grammar.Opt(Grammar.Seq("init", Cmp("="), expression)),
            Punct(";"));

        // forStmt        = "for" "(" ( varDecl | exprStmt | ";" )
        //                  expression? ";" expression? ")" statement ;
        var forStmt = Grammar.Seq("for",
            Kw("for"), Br("("),
            Grammar.Alt(varDecl, exprStmt, Punct(";")),
            Grammar.Opt(expression), Punct(";"),
            Grammar.Opt(expression), Br(")"),
            statementRef);

        // statement      = exprStmt | forStmt | ifStmt | printStmt
        //                | returnStmt | whileStmt | block ;
        // Keyword-led statements are tried before exprStmt so that e.g. "print (x);"
        // parses as a print statement rather than a call to an identifier "print".
        statement = Grammar.Alt(forStmt, ifStmt, printStmt, returnStmt, whileStmt, block, exprStmt);

        // ----- declarations -----

        // parameters     = IDENTIFIER ( "," IDENTIFIER )* ;
        var parameters = Grammar.SeparatedBy("params", identifier, Punct(","));

        // function       = IDENTIFIER "(" parameters? ")" block ;
        var function = Grammar.Seq("function", identifier, Br("("), Grammar.Opt(parameters), Br(")"), block);

        // funDecl        = "fun" function ;
        var funDecl = Grammar.Seq("fun", Kw("fun"), function);

        // classDecl      = "class" IDENTIFIER ( "<" IDENTIFIER )? "{" function* "}" ;
        var classDecl = Grammar.Seq("class",
            Kw("class"), identifier,
            Grammar.Opt(Grammar.Seq("extends", Cmp("<"), identifier)),
            Br("{"), Grammar.ZeroOrMore(function), Br("}"));

        // declaration    = classDecl | funDecl | varDecl | statement ;
        declaration = Grammar.Alt(classDecl, funDecl, varDecl, statement);

        // program        = declaration* EOF ;
        // The EndOfInput token stays in the tree: it carries the file's trailing
        // whitespace as trivia, completing the exact source round-trip.
        Program = Grammar.Seq("program", Grammar.ZeroOrMore(declaration), Grammar.Expect(TokenKind.EndOfInput));
    }
}
