namespace Lox.Runtime;

public interface ICallable
{
    object? Call(Interpreter interpreter, params object?[] arguments);
}
