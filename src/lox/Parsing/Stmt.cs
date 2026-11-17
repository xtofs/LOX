namespace Lox;

public abstract record Stmt
{
    public sealed record ExprStmt(Expr Expression) : Stmt
    {
        public override string ToString() => $"{Expression};";
    }

    public sealed record Print(Expr Expression) : Stmt
    {
        public override string ToString() => $"print {Expression};";
    }

    public sealed record Return(Expr? Value) : Stmt
    {
        public override string ToString() => Value is null ? "return;" : $"return {Value};";
    }

    public sealed record Block(IReadOnlyList<Stmt> Statements) : Stmt
    {
        public static Block Empty { get; } = new Block(Array.Empty<Stmt>());

        public override string ToString() => $"{{ {string.Join(" ", Statements)} }}";
    }

    public sealed record If(Expr Condition, Stmt Then, Stmt? Else) : Stmt
    {
        public override string ToString() =>
            Else is null ? $"if ({Condition}) {Then}" : $"if ({Condition}) {Then} else {Else}";
    }

    public sealed record While(Expr Condition, Stmt Body) : Stmt
    {
        public override string ToString() => $"while ({Condition}) {Body}";
    }

    public sealed record For(Stmt? Initializer, Expr? Condition, Expr? Increment, Stmt Body) : Stmt
    {
        public override string ToString() => $"for ({Initializer?.ToString() ?? ";"} {Condition}; {Increment}) {Body}";
    }

    public sealed record VarDecl(string Name, Expr? Init) : Stmt
    {
        public override string ToString() => Init is null ? $"var {Name};" : $"var {Name} = {Init};";
    }

    public sealed record FunDecl(string Name, IReadOnlyList<(string Name, int Start)> Parameters, Block Body) : Stmt
    {
        public override string ToString() => $"fun {Name}({string.Join(", ", Parameters)}) {Body}";
    }

    public sealed record ClassDecl(string Name, string? Superclass, IReadOnlyList<FunDecl> Methods) : Stmt
    {
        public override string ToString() =>
            $"class {Name}{(Superclass is null ? "" : $" < {Superclass}")} {{ {string.Join(" ", Methods)} }}";
    }
}
