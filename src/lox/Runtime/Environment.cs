namespace Lox.Runtime;

public readonly struct Environment : IFormattable
{
    private readonly Dictionary<string, object?> _values;

    public Environment()
    {
        _values = [];
    }

    public Environment(Environment env)
    {
        _values = new Dictionary<string, object?>(env._values);
    }

    public bool TryGetValue(string name, out object? value) => _values.TryGetValue(name, out value);

    // [Obsolete("Use the TryGetValue instead.")]
    // public object? this[string name]
    // {
    //     get
    //     {
    //         return _values.TryGetValue(name, out var value) ? value : throw new Exception($"Undefined variable '{name}' {string.Join(", ", _values.Keys)}");
    //     }
    // }

    internal void SetVar(string var, object? value)
    {
        _values[var] = value;
    }

    // [Obsolete("Use the setter `this[name] = value` instead.")]
    // public Environment WithVar(string var, object? val) => throw new NotImplementedException();

    public bool Equals(Environment other)
    {
        foreach (var kvp in _values)
        {
            if (!other._values.TryGetValue(kvp.Key, out var otherValue) || !Equals(kvp.Value, otherValue))
            {
                return false;
            }
        }
        return true;
    }

    public override string ToString() => $"Environment({string.Join(", ", _values.Keys)})";

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        switch (format)
        {
            case null:
            case "K":
                return $"Environment{{ {string.Join(", ", _values.Keys)}}})";
            case "KV":
                return $"Environment{{ {string.Join(", ", _values.Select(kv => $"{kv.Key}: {kv.Value}"))}}}";
            default:
                return ToString();
        }
    }
}
