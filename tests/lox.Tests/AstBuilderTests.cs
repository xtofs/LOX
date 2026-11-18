using Lox;
using Lox.Parsing;
using Lox.Scanning;
using Lox.Syntax;

public class AstBuilderTests
{
    private static Program Parse(string source)
    {
        var scanner = new Scanner(source);
        var tokens = scanner.GetEnumerator();
        tokens.MoveNext();
        Assert.True(LoxGrammar.Program(tokens, out var cst, out _), $"failed to parse: {source}");
        return AstBuilder.ToProgram(cst!, scanner.LineIndex);
    }

    private static Stmt Single(string source) => Assert.Single(Parse(source).Declarations);

    [Fact]
    public void Empty_program_has_no_declarations()
    {
        Assert.Empty(Parse("").Declarations);
    }

    [Fact]
    public void Member_access_becomes_Get()
    {
        var stmt = Assert.IsType<Stmt.ExprStmt>(Single("a.b;"));
        var get = Assert.IsType<Expr.GetProperty>(stmt.Expression);
        Assert.Equal(new Expr.Variable("a") { Start = 0 }, get.Object);
        Assert.Equal("b", get.Name);
    }

    [Fact]
    public void Invocation_becomes_Call()
    {
        var stmt = Assert.IsType<Stmt.ExprStmt>(Single("a(b);"));
        var call = Assert.IsType<Expr.Call>(stmt.Expression);
        Assert.Equal(new Expr.Variable("a") { Start = 0 }, call.Callee);
        var arg = Assert.Single(call.Arguments);
        Assert.Equal(new Expr.Variable("b") { Start = 2 }, arg);
    }

    [Fact]
    public void Call_without_arguments_has_empty_argument_list()
    {
        var stmt = Assert.IsType<Stmt.ExprStmt>(Single("f();"));
        var call = Assert.IsType<Expr.Call>(stmt.Expression);
        Assert.Empty(call.Arguments);
    }

    [Fact]
    public void Return_without_value_has_null_Value()
    {
        var fun = Assert.IsType<Stmt.FunDecl>(Single("fun g() { return; }"));
        var ret = Assert.IsType<Stmt.Return>(Assert.Single(fun.Body.Statements));
        Assert.Null(ret.Value);
    }

    [Fact]
    public void Class_without_superclass_has_null_Superclass()
    {
        var cls = Assert.IsType<Stmt.ClassDecl>(Single("class C {}"));
        Assert.Equal("C", cls.Name);
        Assert.Null(cls.Superclass);
        Assert.Empty(cls.Methods);
    }

    [Fact]
    public void Class_with_superclass_and_super_call()
    {
        var cls = Assert.IsType<Stmt.ClassDecl>(Single("class A < B { m() { return super.m(); } }"));
        Assert.Equal("A", cls.Name);
        Assert.Equal("B", cls.Superclass);
        var method = Assert.Single(cls.Methods);
        Assert.Equal("m", method.Name);
        var ret = Assert.IsType<Stmt.Return>(Assert.Single(method.Body.Statements));
        var call = Assert.IsType<Expr.Call>(ret.Value);
        var super = Assert.IsType<Expr.Super>(call.Callee);
        Assert.Equal("m", super.Method);
    }

    [Fact]
    public void For_with_empty_clauses_has_null_slots()
    {
        var loop = Assert.IsType<Stmt.For>(Single("for (;;) print 1;"));
        Assert.Null(loop.Initializer);
        Assert.Null(loop.Condition);
        Assert.Null(loop.Increment);
        Assert.IsType<Stmt.Print>(loop.Body);
    }

    [Fact]
    public void For_with_all_clauses_fills_all_slots()
    {
        var loop = Assert.IsType<Stmt.For>(Single("for (var i = 0; i < 3; i = i + 1) {}"));
        var init = Assert.IsType<Stmt.VarDecl>(loop.Initializer);
        Assert.Equal("i", init.Name);
        Assert.IsType<Expr.Binary>(loop.Condition);
        Assert.IsType<Expr.Assign>(loop.Increment);
        Assert.Empty(Assert.IsType<Stmt.Block>(loop.Body).Statements);
    }

    [Fact]
    public void For_with_condition_only()
    {
        var loop = Assert.IsType<Stmt.For>(Single("for (; x < 3;) print x;"));
        Assert.Null(loop.Initializer);
        Assert.IsType<Expr.Binary>(loop.Condition);
        Assert.Null(loop.Increment);
    }

    [Theory]
    [InlineData("print 1 + 2 + 3;", "print ((1 + 2) + 3);")]              // left associative
    [InlineData("print 1 + 2 * 3;", "print (1 + (2 * 3));")]              // precedence
    [InlineData("print a or b and c;", "print (a or (b and c));")]        // and binds tighter
    [InlineData("a = b = 1;", "(a = (b = 1));")]                          // right associative
    [InlineData("print -x;", "print -x;")]
    [InlineData("print !true;", "print !true;")]
    [InlineData("print (1 + 2) * 3;", "print ((1 + 2) * 3);")]            // grouping dropped
    [InlineData("print nil;", "print nil;")]
    [InlineData("print \"hi\";", "print \"hi\";")]
    [InlineData("if (a) if (b) print 1; else print 2;",
                "if (a) if (b) print 1; else print 2;")]                  // dangling else binds inner
    [InlineData("this.name = name;", "(this.name = name);")]
    [InlineData("duck.quack();", "duck.quack();")]
    public void Ast_shape_via_printer(string source, string expected)
    {
        Assert.Equal(expected, Parse(source).ToString());
    }

    [Fact]
    public void Function_parameters_are_collected()
    {
        var fun = Assert.IsType<Stmt.FunDecl>(Single("fun f(a, b, c) { }"));
        Assert.Equal(["a", "b", "c"], fun.Parameters.Select(p => p.Name));
    }

    [Fact]
    public void Ast_contains_no_structure_tokens_by_construction()
    {
        // The typed records cannot represent keyword/bracket/punctuation tokens;
        // this test exists to document the property the CST/AST split guarantees.
        var program = Parse("class A < B { m(x) { if (x) return super.m(); else return nil; } }");
        var cls = Assert.IsType<Stmt.ClassDecl>(Assert.Single(program.Declarations));
        Assert.Equal("A", cls.Name);
    }
}
