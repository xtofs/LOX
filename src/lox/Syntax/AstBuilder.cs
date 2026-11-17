namespace Lox.Syntax;

/// <summary>
/// Lowers the concrete syntax tree produced by <see cref="LoxGrammar"/> into the
/// typed AST. This is where structure tokens (keywords, brackets, punctuation)
/// and trivia are dropped and children are assigned roles, keyed by the CST tags.
/// </summary>
public class AstBuilder
{
    public static Program ToProgram(Node cst, int[] lineIndex)
    {
        var builder = new AstBuilder(lineIndex);

        // An empty program collapses to the bare EndOfInput token.
        if (cst is TokenNode { Kind: TokenKind.EndOfInput })
        {
            return new Program([]);
        }

        var program = builder.ToProgram(Sequence(cst, "program"));
        return program;
    }

    private AstBuilder(int[] lineIndex)
    {
        _lineIndex = lineIndex;
    }

    private readonly int[] _lineIndex;

    private Program ToProgram(SequenceNode program)
    {
        return new Program([.. Rep(program.Children[0]).Select(ToStmt)]);
    }

    public Stmt ToStmt(Node node)
    {
        var s = (SequenceNode)node;
        var c = s.Children;
        return s.Tag switch
        {
            // (exprStmt expr ;)
            "exprStmt" => new Stmt.ExprStmt(ToExpr(c[0])),
            // (print "print" expr ;)
            "print" => new Stmt.Print(ToExpr(c[1])),
            // (return "return" expr? ;)
            "return" => new Stmt.Return(c.Count == 3 ? ToExpr(c[1]) : null),
            "block" => ToBlock(s),
            // (var "var" name (init = expr)? ;)
            "var" => new Stmt.VarDecl(
                Token(c[1]).Value,
                c.Count == 4 ? ToExpr(Sequence(c[2], "init").Children[1]) : null),
            // (while "while" ( cond ) body)
            "while" => new Stmt.While(ToExpr(c[2]), ToStmt(c[4])),
            // (if "if" ( cond ) then (else "else" stmt)?)
            "if" => new Stmt.If(
                ToExpr(c[2]),
                ToStmt(c[4]),
                c.Count == 6 ? ToStmt(Sequence(c[5], "else").Children[1]) : null),
            "for" => ToFor(c),
            // (fun "fun" function)
            "fun" => ToFunction(c[1]),
            "function" => ToFunction(s),
            "class" => ToClass(c),
            _ => throw new InvalidOperationException($"unexpected statement node '{s.Tag}'"),
        };
    }



    Expr ToExpr(Node node)
    {
        if (node is TokenNode token)
        {
            return token.Kind switch
            {
                TokenKind.Number => new Expr.Literal(double.Parse(token.Value, System.Globalization.CultureInfo.InvariantCulture)) { Start = token.Start },
                TokenKind.QuotedString => new Expr.Literal(token.Value[1..^1]) { Start = token.Start },
                TokenKind.Identifier => token.Value switch
                {
                    "true" => new Expr.Literal(true) { Start = token.Start },
                    "false" => new Expr.Literal(false) { Start = token.Start },
                    "nil" => new Expr.Literal(null) { Start = token.Start },
                    "this" => new Expr.This() { Start = token.Start },
                    _ => new Expr.Variable(token.Value) { Start = token.Start },
                },
                _ => throw new InvalidOperationException($"unexpected token {token.Kind} '{token.Value}' in expression"),
            };
        }

        var s = (SequenceNode)node;
        var c = s.Children;
        return s.Tag switch
        {
            // (group ( expr )) — parentheses dropped, precedence is already explicit
            "group" => ToExpr(c[1]),
            // (super "super" . method)
            "super" => new Expr.Super(Token(c[2]).Value) { Start = c[2].Start },
            // (unary op operand)
            "unary" => new Expr.Unary(Token(c[0]).Value, ToExpr(c[1])) { Start = c[0].Start },
            // flat chains: (tag e op e op e …), folded left-associatively
            "factor" or "term" or "comparison" or "equality" =>
                FoldChain(c, static (l, op, r) => new Expr.Binary(l, op, r) { Start = l.Start }),
            "and" or "or" =>
                FoldChain(c, static (l, op, r) => new Expr.Logical(l, op, r) { Start = l.Start }),
            // (assign lhs (rhs = value))
            "assign" => new Expr.Assign(ToExpr(c[0]), ToExpr(Sequence(c[1], "rhs").Children[1])) { Start = c[0].Start },
            "call" => ToCall(c),
            _ => throw new InvalidOperationException($"unexpected expression node '{s.Tag}'"),
        };
    }

    // (for "for" ( varDecl|exprStmt|";"  cond? ;  incr? ) body)
    private Stmt.For ToFor(IReadOnlyList<Node> c)
    {
        var i = 2;
        Stmt? init = IsPunct(c[i], ";") ? null : ToStmt(c[i]);
        i++;

        Expr? cond = null;
        if (!IsPunct(c[i], ";"))
        {
            cond = ToExpr(c[i]);
            i++;
        }
        i++; // the ";" after the condition slot

        Expr? incr = null;
        if (!IsBracket(c[i], ")"))
        {
            incr = ToExpr(c[i]);
            i++;
        }
        i++; // the ")"

        return new Stmt.For(init, cond, incr, ToStmt(c[i]));
    }

    // (class "class" name (extends < super)? { function* })
    private Stmt.ClassDecl ToClass(IReadOnlyList<Node> c)
    {
        var name = Token(c[1]).Value;
        var i = 2;
        string? superclass = null;
        if (c[i] is SequenceNode { Tag: "extends" } extends)
        {
            superclass = Token(extends.Children[1]).Value;
            i++;
        }
        i++; // the "{"
        var methods = IsBracket(c[i], "}")
            ? []
            : Rep(c[i]).Select(ToFunction).ToList();
        return new Stmt.ClassDecl(name, superclass, methods);
    }

    // (function name ( params? ) block)
    private Stmt.FunDecl ToFunction(Node node)
    {
        var s = Sequence(node, "function");
        var c = s.Children;
        IReadOnlyList<(string, int)> parameters = c.Count == 5 ? ToParameters(c[2]) : [];
        return new Stmt.FunDecl(Token(c[0]).Value, parameters, ToBlock(c[^1]));
    }

    // (params name , name …) — or a single identifier, or absent
    private List<(string, int)> ToParameters(Node node) => node switch
    {
        TokenNode { Kind: TokenKind.Identifier } single => [(single.Value, single.Start)],
        SequenceNode { Tag: "params" } list =>
            [.. list.Children
                .OfType<TokenNode>()
                .Where(static t => t.Kind == TokenKind.Identifier)
                .Select(static t => (t.Value, t.Start))],
        _ => throw new InvalidOperationException($"unexpected parameter list node"),
    };

    // (block { declaration* })
    private Stmt.Block ToBlock(Node node)
    {
        var c = Sequence(node, "block").Children;
        return new Stmt.Block(c.Count == 3 ? [.. Rep(c[1]).Select(ToStmt)] : []);
    }

    // (call primary postfix*) with postfix = (invoke ( args? )) | (member . name)
    private Expr ToCall(IReadOnlyList<Node> c)
    {
        var expr = ToExpr(c[0]);
        foreach (var postfix in Rep(c[1]))
        {
            var p = (SequenceNode)postfix;
            expr = p.Tag switch
            {
                "member" => new Expr.GetProperty(expr, Token(p.Children[1]).Value) { Start = expr.Start },
                "invoke" => new Expr.Call(expr, ToArguments(p.Children)) { Start = expr.Start },
                _ => throw new InvalidOperationException($"unexpected call postfix '{p.Tag}'"),
            };
        }
        return expr;
    }

    // (invoke ( (args e , e …)? )) — args may also be a single expression, or absent
    private List<Expr> ToArguments(IReadOnlyList<Node> invoke)
    {
        if (invoke.Count < 3)
        {
            return [];
        }
        return invoke[1] is SequenceNode { Tag: "args" } args
            ? [.. args.Children.Where(static n => !IsPunct(n, ",")).Select(ToExpr)]
            : [ToExpr(invoke[1])];
    }

    private Expr FoldChain(IReadOnlyList<Node> c, Func<Expr, string, Expr, Expr> combine)
    {
        var expr = ToExpr(c[0]);
        for (var i = 1; i + 1 < c.Count; i += 2)
        {
            expr = combine(expr, Token(c[i]).Value, ToExpr(c[i + 1]));
        }
        return expr;
    }

    private static TokenNode Token(Node node) => (TokenNode)node;

    private static bool IsPunct(Node node, string value) =>
        node is TokenNode { Kind: TokenKind.Punctuation } t && t.Value == value;

    private static bool IsBracket(Node node, string value) =>
        node is TokenNode { Kind: TokenKind.Bracket } t && t.Value == value;

    private static SequenceNode Sequence(Node node, string expectedTag)
    {
        if (node is SequenceNode s && s.Tag == expectedTag)
        {
            return s;
        }
        throw new InvalidOperationException($"expected '{expectedTag}' node, got {node}");
    }

    /// <summary>Expands an anonymous repetition node; a collapsed single element
    /// or any other node is returned as a one-element sequence.</summary>
    private static IEnumerable<Node> Rep(Node node) =>
        node is SequenceNode { Tag: "rep" } rep ? rep.Children : [node];
}
