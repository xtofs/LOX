namespace Lox;

public readonly ref struct Token<TEnum>(TEnum kind, int start, int length)
    where TEnum : struct
{
    public TEnum Kind { get; } = kind;
    public int Start { get; } = start;
    public int Length { get; } = length;

    public static implicit operator Token<TEnum>((TEnum Kind, int Start, int Length) tuple) => new Token<TEnum>(tuple.Kind, tuple.Start, tuple.Length);

    public void Deconstruct(out TEnum kind, out int start, out int length)
    {
        kind = Kind;
        start = Start;
        length = Length;
    }

    public override string ToString() => $"({Kind}, {Start}, {Length})";

    public (int Line, int Column) GetPosition(List<int> lineIndex)
    {
        var ix = lineIndex.BinarySearch(Start);
        if (ix < 0) { ix = ~ix; }
        var line = ix + 1;
        var column = ix == 0 ? Start + 1 : Start - lineIndex[ix - 1];
        return (line, column);
    }

    public string GetValue(string source)
    {
        return source.Substring(Start, Length);
    }
}