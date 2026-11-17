public class CstRoundTripTests
{
    private static Node Parse(string source)
    {
        var scanner = new Scanner(source);
        var tokens = scanner.GetEnumerator();
        tokens.MoveNext();
        Assert.True(LoxGrammar.Program(tokens, out var cst, out _), $"failed to parse: {source}");
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
