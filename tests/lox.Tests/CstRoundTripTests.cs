using Lox;
using Lox.Parsing;
using Lox.Scanning;

public class CstRoundTripTests
{
    private static Node Parse(string source)
    {
        var scanner = new Scanner(source);
        var tokens = scanner.GetEnumerator();
        tokens.MoveNext();
        var success = LoxGrammar.Program.TryParse(tokens, out var cst, out _, out var error);
        Assert.True(success, $"failed to parse: {source.ReplaceControlCharacters()}{Environment.NewLine}{error}");
        return cst!;
    }

    internal static IEnumerable<TokenNode> Tokens(Node node) => node switch
    {
        TokenNode token => [token],
        SequenceNode sequence => sequence.Children.SelectMany(Tokens),
        _ => [],
    };

    [Theory]
    [InlineData("")]
    [InlineData("   \n\t  ")]
    [InlineData("print 1;")]
    [InlineData("  print  1 ;\n\n")]
    [InlineData("\tvar x\t=\t10;\r\n")]
    [InlineData("var duck = Duck(\"Waddles\");\nduck.quack();\n")]
    [InlineData("if (a and b or !c) print a; else print b;")]
    [InlineData("while (i < 10) { i = i + 1; }")]
    [InlineData("for (var i = 0; i < 5; i = i + 1) { print i * i; }")]
    [InlineData("for (;;) print 1;")]
    [InlineData("class A < B { m() { return super.m(); } }")]
    [InlineData("class C {}")]
    [InlineData("fun f(a, b) { return a + b; }\nprint f(1, 2);")]
    [InlineData("fun g() { return; }")]
    [InlineData("print (1 + 2) * 3 >= 4 != true;")]
    [InlineData("var x = nil;\nx = this;\n\n\nprint  x ;   ")]
    public void Cst_reproduces_source_exactly(string source)
    {
        var cst = Parse(source);
        var reconstructed = string.Concat(Tokens(cst).Select(static t => t.LeadingTrivia + t.Value));
        Assert.Equal(source, reconstructed);
    }

    [Fact]
    public void Cst_reproduces_multiline_program_with_trailing_whitespace()
    {
        var source = """
            for (var i = 1; i < 5; i = i + 1) {
              print i * i;
            }

            class Duck {
              init(name) {
                this.name = name;
              }

              quack() {
                print this.name + " quacks";
              }
            }

            """;
        var cst = Parse(source);
        var reconstructed = string.Concat(Tokens(cst).Select(static t => t.LeadingTrivia + t.Value));
        Assert.Equal(source, reconstructed);
    }
}

public class NegativeParseTests
{
    private static bool TryParse(string source)
    {
        var scanner = new Scanner(source);
        var tokens = scanner.GetEnumerator();
        tokens.MoveNext();
        return LoxGrammar.Program(tokens, out _, out _);
    }

    [Theory]
    [InlineData("var x = ;")]
    [InlineData("f(a,);")]
    [InlineData("{ print 1;")]
    [InlineData("class A < { }")]
    [InlineData("print 1 +;")]
    [InlineData("(1;")]
    [InlineData("if (x print 1;")]
    public void Invalid_programs_fail_to_parse(string source)
    {
        Assert.False(TryParse(source));
    }
}

public class ParseDiagnosticsTests
{
    public static TheoryData<string, int, int, TokenKind, TokenKind> InvalidPrograms => new()
    {
        { "print 1", 1, 8, TokenKind.EOF, TokenKind.SEMICOLON },
        { "print 1;\r\n  var x = ;", 2, 11, TokenKind.SEMICOLON, TokenKind.NUMBER },
        { "f(a,);", 1, 5, TokenKind.RIGHT_PAREN, TokenKind.IDENTIFIER },
        { "{ print 1;", 1, 11, TokenKind.EOF, TokenKind.RIGHT_BRACE },
    };

    [Theory]
    [MemberData(nameof(InvalidPrograms))]
    public void Reports_furthest_failure_after_backtracking(
        string source, int line, int column, TokenKind actual, TokenKind expected)
    {
        var scanner = new Scanner(source);
        var tokens = scanner.GetEnumerator();
        tokens.MoveNext();

        Assert.False(LoxGrammar.Program.TryParse(tokens, out var cst, out _, out var error));

        Assert.Null(cst);
        Assert.NotNull(error);
        Assert.Equal((line, column), (error.Line, error.Column));
        Assert.Equal(actual, error.Remaining[0].Kind);
        Assert.Equal(error.Start, error.Remaining[0].Start);
        Assert.Contains(new TokenExpectation(expected), error.Expected);
        Assert.Equal(TokenKind.EOF, error.Remaining[^1].Kind);
        Assert.StartsWith($"({line},{column}): expected ", error.ToString());
    }

    [Fact]
    public void Combines_expectations_and_snapshots_current_token_without_changing_remainder()
    {
        var left = Grammar.Seq("left", Grammar.Expect(TokenKind.IDENTIFIER), Grammar.Expect(TokenKind.LEFT_PAREN));
        var right = Grammar.Seq("right", Grammar.Expect(TokenKind.IDENTIFIER), Grammar.Expect(TokenKind.RIGHT_PAREN));
        var rule = Grammar.Seq("program",
            Grammar.ZeroOrMore(Grammar.Alt(left, right, left, left.Named("other"))), Grammar.Expect(TokenKind.EOF));
        var scanner = new Scanner("f x;");
        var tokens = scanner.GetEnumerator();
        tokens.MoveNext();

        Assert.False(rule.TryParse(tokens, out _, out var remaining, out var error));

        Assert.NotNull(error);
        Assert.Equal(0, remaining.Current.Start);
        Assert.Equal(
            [new TokenExpectation(TokenKind.LEFT_PAREN), new TokenExpectation(TokenKind.RIGHT_PAREN)],
            error.Expected.OrderBy(static expectation => expectation.Kind));
        Assert.Collection(error.Contexts,
            context =>
            {
                Assert.Equal(new TokenExpectation(TokenKind.LEFT_PAREN), context.Expected);
                Assert.Equal(["program", "left"], context.RulePath);
            },
            context =>
            {
                Assert.Equal(new TokenExpectation(TokenKind.RIGHT_PAREN), context.Expected);
                Assert.Equal(["program", "right"], context.RulePath);
            },
            context =>
            {
                Assert.Equal(new TokenExpectation(TokenKind.LEFT_PAREN), context.Expected);
                Assert.Equal(["program", "other", "left"], context.RulePath);
            });
        Assert.Equal(
            [new ParseToken(TokenKind.IDENTIFIER, "x", 2),
             new ParseToken(TokenKind.SEMICOLON, ";", 3),
             new ParseToken(TokenKind.EOF, "", 4)],
            error.Remaining);
        Assert.Contains("found IDENTIFIER \"x\"", error.ToString());
    }

    public static TheoryData<string, TokenKind, string[]> RuleContexts => new()
    {
        { "(a,)", TokenKind.IDENTIFIER, ["root", "program", "group", "items"] },
        { "print (", TokenKind.SEMICOLON, ["root", "program", "header"] },
        { "print; (a) x", TokenKind.LEFT_PAREN, ["root", "program", "group"] },
        { "print; (a) x", TokenKind.EOF, ["root", "program"] },
    };

    [Theory]
    [MemberData(nameof(RuleContexts))]
    public void Captures_named_rule_path_at_failure_without_leaking_completed_rules(
        string source, TokenKind expected, string[] rulePath)
    {
        var header = Grammar.Seq("header", Grammar.Expect(TokenKind.PRINT), Grammar.Expect(TokenKind.SEMICOLON));
        var items = Grammar.SeparatedBy("items", Grammar.Expect(TokenKind.IDENTIFIER), Grammar.Expect(TokenKind.COMMA));
        var group = Grammar.Seq("group",
            Grammar.Expect(TokenKind.LEFT_PAREN), Grammar.Opt(items), Grammar.Expect(TokenKind.RIGHT_PAREN));
        var rule = Grammar.Seq("program",
            Grammar.Opt(header), Grammar.ZeroOrMore(group), Grammar.Expect(TokenKind.EOF)).Named("root");
        var scanner = new Scanner(source);
        var tokens = scanner.GetEnumerator();
        tokens.MoveNext();

        Assert.False(rule.TryParse(tokens, out _, out _, out var error));

        Assert.NotNull(error);
        var context = Assert.Single(error.Contexts, context => context.Expected == new TokenExpectation(expected));
        Assert.Equal(rulePath, context.RulePath);
        Assert.Contains($"Context: {string.Join(" > ", rulePath)} (expected {expected})", error.ToString());
    }

    [Theory]
    [InlineData("other")]
    [InlineData("")]
    public void Reports_expected_value_as_well_as_kind(string source)
    {
        var scanner = new Scanner(source);
        var tokens = scanner.GetEnumerator();
        tokens.MoveNext();
        var rule = Grammar.Expect(TokenKind.IDENTIFIER, "wanted");

        Assert.False(rule.TryParse(tokens, out _, out _, out var error));

        Assert.NotNull(error);
        Assert.Equal(new TokenExpectation(TokenKind.IDENTIFIER, "wanted"), Assert.Single(error.Expected));
        Assert.Contains("expected IDENTIFIER \"wanted\"", error.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("print 1;")]
    [InlineData("class C {}")]
    public void Successful_parse_discards_speculative_failures(string source)
    {
        var scanner = new Scanner(source);
        var tokens = scanner.GetEnumerator();
        tokens.MoveNext();

        Assert.True(LoxGrammar.Program.TryParse(tokens, out var cst, out _, out var error));

        Assert.NotNull(cst);
        Assert.Null(error);
    }
}
