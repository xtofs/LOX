namespace Lox;

/// <summary>
/// The abstract syntax tree: typed records with role-named properties, produced
/// from the CST by <see cref="AstBuilder"/>. Deliberately lossy — no structure
/// tokens, no trivia; formatting cannot be reconstructed from an AST.
/// </summary>
public closed record Expr
{
    /// <summary>The start position (character index) of this expression in the source code.</summary>
    public required int Start { get; init; }

    /// <summary>A literal value: number, string, true/false, or nil (null).</summary>
    public sealed record Literal(object? Value) : Expr
    {
        public override string ToString() => Value switch
        {
            null => "nil",
            bool b => b ? "true" : "false",
            string s => $"\"{s}\"",
            _ => Value.ToString()!,
        };
    }

    public sealed record Variable(string Name) : Expr
    {
        public override string ToString() => Name;
    }

    public sealed record This : Expr
    {
        public override string ToString() => "this";
    }

    public sealed record Super(string Method) : Expr
    {
        public override string ToString() => $"super.{Method}";
    }

    public sealed record Unary(string Op, Expr Operand) : Expr
    {
        public override string ToString() => $"{Op}{Operand}";
    }

    public sealed record Binary(Expr Left, string Op, Expr Right) : Expr
    {
        public override string ToString() => $"({Left} {Op} {Right})";
    }

    public sealed record Logical(Expr Left, string Op, Expr Right) : Expr
    {
        public override string ToString() => $"({Left} {Op} {Right})";
    }

    public sealed record Assign(Expr Target, Expr Value) : Expr
    {
        public override string ToString() => $"({Target} = {Value})";
    }

    public sealed record Call(Expr Callee, IReadOnlyList<Expr> Arguments) : Expr
    {
        public override string ToString() => $"{Callee}({string.Join(", ", Arguments)})";
    }

    /// <summary>Property access: Object.Name</summary>
    public sealed record GetProperty(Expr Object, string Name) : Expr
    {
        public override string ToString() => $"{Object}.{Name}";
    }
}
