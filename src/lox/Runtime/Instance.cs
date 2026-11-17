namespace Lox.Runtime;

public class Instance
{
    public required Class Class { get; init; }

    private readonly Dictionary<string, object> _fields = [];

    public bool TryGetValue(string name, out object? value)
    {
        return _fields.TryGetValue(name, out value);
    }

    public object? this[string name]
    {
        [Obsolete("use TryGetValue instead")]
        get => _fields.TryGetValue(name, out var value) ? value : null;
        internal set => _fields[name] = value!;
    }
}