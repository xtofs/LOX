using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Lox;

public partial class Scanner(string source)
{
    private readonly string _source = source;

    private readonly List<int> _lineIndex = [0];


    public TokenEnumerator GetEnumerator()
    {
        var inner = MyRegex.ContiguousMatch<TokenKind>(_source, i => _lineIndex.Add(i));
        return new TokenEnumerator(inner);
    }

    internal string GetValue(Token<TokenKind> token)
    {
        return _source.Substring(token.Start, token.Length);
    }


    public static (int Line, int Column) GetPosition(int start, List<int> lineIndex)
    {
        return GetPosition(start, CollectionsMarshal.AsSpan(lineIndex));
    }

    public static (int Line, int Column) GetPosition(int start, ReadOnlySpan<int> lineIndex)
    {
        var ix = lineIndex.BinarySearch(start);
        if (ix < 0) { ix = ~ix; }
        var line = ix;
        var column = ix == 0 ? start + 1 : start - lineIndex[ix - 1];
        return (line, column);
    }

    public void GetKindValuePosition(Token<TokenKind> token, out TokenKind kind, out string value, out (int Line, int Column) position)
    {
        kind = token.Kind;
        value = token.GetValue(_source);
        position = Scanner.GetPosition(token.Start, _lineIndex);
    }


    /* lang=regex */
    const string Pattern = """
    \G(
          (?'Whitespace'\s+)
        | (?'Number'\d+)
        | (?'Identifier'[a-zA-Z_]\w*)
        | (?'Comparison'[<>=!]=?)
        | (?'Operator'[+\-*/])
        | (?'Bracket'[{}[\]<>()])
        | (?'Punctuation'[;,.])
        | (?'QuotedString'"[^"]*")
    )
    """;

    [GeneratedRegex(Pattern, RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture)]
    static partial Regex MyRegex { get; }

    public int[] LineIndex => [.. _lineIndex];
}


public ref struct TokenEnumerator(ContiguousMatchEnumerator<TokenKind> contiguousMatchEnumerator)
{
    private ContiguousMatchEnumerator<TokenKind> _contiguousMatchEnumerator = contiguousMatchEnumerator;
    private int _triviaStart;
    private int _triviaLength;

    public bool MoveNext()
    {
        var triviaStart = -1;
        do
        {
            var success = _contiguousMatchEnumerator.MoveNext();
            if (!success) { return false; }
            if (triviaStart < 0) { triviaStart = _contiguousMatchEnumerator.Current.Start; }
        } while (_contiguousMatchEnumerator.Current.Kind == TokenKind.Whitespace);
        _triviaStart = triviaStart;
        _triviaLength = _contiguousMatchEnumerator.Current.Start - triviaStart;
        return true;
    }

    public readonly Token<TokenKind> Current => _contiguousMatchEnumerator.Current;

    /// <summary>The whitespace between the previous token and Current.</summary>
    internal readonly string GetLeadingTrivia()
    {
        return _contiguousMatchEnumerator.Source.Substring(_triviaStart, _triviaLength);
    }

    internal string GetValue(Token<TokenKind> token)
    {
        return _contiguousMatchEnumerator.Source.Substring(token.Start, token.Length);
    }

}
