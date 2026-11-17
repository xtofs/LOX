namespace Lox.Runtime;


public class Class : ICallable
{
    public required String Name { get; init; }
    public required Class? Superclass { get; init; }
    public required IReadOnlyDictionary<string, Function> Methods { get; init; }

    // When the class is called:
    //  Create a LoxInstance.
    //  Find the init method.
    //  Bind the instance as this.
    //  Invoke init.
    //  Return the instance.

    public object? Call(Interpreter interpreter, params object?[] arguments)
    {
        return interpreter.CreateInstance(this, arguments);
        
    }
}
