namespace Lox;

using System.Text.RegularExpressions;

public static class RegexExtensions
{
    extension(Regex regex)
    {
        public ContiguousMatchEnumerator<TEnum> ContiguousMatch<TEnum>(string source, Action<int> action)
           where TEnum : struct
        {
            return new ContiguousMatchEnumerator<TEnum>(regex, source, action);
        }
    }
}

public ref struct ContiguousMatchEnumerator<TEnum>(Regex regex, string source, Action<int> action)
    where TEnum : struct
{
    private Match _match = regex.Match(source);

    private readonly Action<int> _action = action;

    public string Source { get; } = source;

    public readonly ContiguousMatchEnumerator<TEnum> GetEnumerator() => this;

    private bool _isEndOfInput;

    public Token<TEnum> Current { get; private set; }

    public bool MoveNext()
    {
        if (!_match.Success)
        {
            if (_isEndOfInput) return false;
            _isEndOfInput = true;
            Current = (default(TEnum),
            Source.Length, 0); return true;
        }

        var group = _match.Groups.Values.Skip(1).Single(g => g.Success)!;
        var kind = Enum.Parse<TEnum>(group.Name, true);

        Current = (kind, _match.Index, _match.Length);
        ScanForNewLine();

        _match = _match.NextMatch();
        return true;
    }

    private readonly void ScanForNewLine()
    {
        for (var i = _match.Index; i < _match.Index + _match.Length; i++)
        {
            if (Source[i] == '\n')
            {
                _action(i);
            }
        }
    }

}
