namespace Lox;

public sealed record Program(IReadOnlyList<Stmt> Declarations)
{
    public override string ToString() => string.Join(System.Environment.NewLine, Declarations);
}
