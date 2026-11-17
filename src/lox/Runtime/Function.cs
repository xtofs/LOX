namespace Lox.Runtime;

using System.Diagnostics;

using Lox.Runtime;

public class Function : ICallable
{
    public required Stmt.FunDecl Declaration { get; init; }

    public required Environment Closure { get; init; }

    public required bool IsInitializer { get; init; }

    public void Deconstruct(out Stmt.FunDecl declaration, out Environment closure)
    {
        declaration = Declaration;
        closure = Closure;
    }

    public void Deconstruct(out Stmt.FunDecl declaration, out Environment closure, out bool isInitializer)
    {
        declaration = Declaration;
        closure = Closure;
        isInitializer = IsInitializer;
    }

    public static Function Empty { get; } = new Function { Declaration = new Stmt.FunDecl("empty", [], Stmt.Block.Empty), Closure = new Environment(), IsInitializer = false };

    public object? Call(Interpreter interpreter, params object?[] arguments)
    {

        return interpreter.CallFunction(this, arguments);
    }


    /// <summary>
    /// Binds the given instance to the function, creating a new closure with "this" set to the instance.
    /// </summary>
    /// <param name="instance"></param>
    /// <returns></returns>
    public Function Bind(Instance instance)
    {
        Closure.SetVar("this", instance);
        return this;
    }
}
