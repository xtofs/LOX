namespace Lox.Scanning;

using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

using Lox.Scanning;


public partial class Scanner(string source)
{
    private readonly string _source = source;

    private readonly List<int> _lineIndex = [0];


    public TokenEnumerator GetEnumerator()
    {
        var inner = MyRegex.ContiguousMatch<TokenCategory>(_source, i => _lineIndex.Add(i));
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
        | (?'Comment'//.*)
        | (?'Number'\d+(\.\d+)?)
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

public ref struct TokenEnumerator(ContiguousMatchEnumerator<TokenCategory> contiguousMatchEnumerator)
{
    internal readonly string Source => _contiguousMatchEnumerator.Source;

    private ContiguousMatchEnumerator<TokenCategory> _contiguousMatchEnumerator = contiguousMatchEnumerator;
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
        } while (_contiguousMatchEnumerator.Current.Kind is TokenCategory.Whitespace or TokenCategory.Comment);
        _triviaStart = triviaStart;
        _triviaLength = _contiguousMatchEnumerator.Current.Start - triviaStart;
        return true;
    }

    private static TokenKind GetTokenKind(TokenCategory category, System.ReadOnlySpan<char> value)
    {
        return category switch
        {
            TokenCategory.Comparison => value switch
            {
                "=" => TokenKind.EQUAL,
                "!" => TokenKind.BANG,
                ">=" => TokenKind.GREATER_EQUAL,
                ">" => TokenKind.GREATER,
                "<=" => TokenKind.LESS_EQUAL,
                "<" => TokenKind.LESS,
                "==" => TokenKind.EQUAL_EQUAL,
                "!=" => TokenKind.BANG_EQUAL,
                _ => throw new ArgumentException($"Unknown comparison operator: {value}")
            },
            TokenCategory.Operator => value switch
            {
                "+" => TokenKind.PLUS,
                "-" => TokenKind.MINUS,
                "*" => TokenKind.STAR,
                "/" => TokenKind.SLASH,
                _ => throw new ArgumentException($"Unknown operator: {value}")
            },
            TokenCategory.Bracket => value switch
            {
                "(" => TokenKind.LEFT_PAREN,
                ")" => TokenKind.RIGHT_PAREN,
                "{" => TokenKind.LEFT_BRACE,
                "}" => TokenKind.RIGHT_BRACE,
                "[" => TokenKind.LEFT_BRACE,
                "]" => TokenKind.RIGHT_BRACE,
                _ => throw new ArgumentException($"Unknown bracket: {value}")
            },
            TokenCategory.Punctuation => value switch
            {
                ";" => TokenKind.SEMICOLON,
                "," => TokenKind.COMMA,
                "." => TokenKind.DOT,
                _ => throw new ArgumentException($"Unknown punctuation: {value}")
            },
            TokenCategory.Identifier => value switch
            {
                "and" => TokenKind.AND,
                "class" => TokenKind.CLASS,
                "else" => TokenKind.ELSE,
                "false" => TokenKind.FALSE,
                "for" => TokenKind.FOR,
                "fun" => TokenKind.FUN,
                "if" => TokenKind.IF,
                "nil" => TokenKind.NIL,
                "or" => TokenKind.OR,
                "print" => TokenKind.PRINT,
                "return" => TokenKind.RETURN,
                "super" => TokenKind.SUPER,
                "this" => TokenKind.THIS,
                "true" => TokenKind.TRUE,
                "var" => TokenKind.VAR,
                "while" => TokenKind.WHILE,
                _ => TokenKind.IDENTIFIER
            },
            TokenCategory.Number => TokenKind.NUMBER,
            TokenCategory.EndOfInput => TokenKind.EOF,
            TokenCategory.QuotedString => TokenKind.STRING,
            _ => throw new ArgumentException($"Unknown token category: {category}")
        };
    }

    public readonly Token<TokenKind> Current
    {
        get
        {
            var current = _contiguousMatchEnumerator.Current;
            var value = _contiguousMatchEnumerator.Source.AsSpan(current.Start, current.Length);
            var kind = GetTokenKind(current.Kind, value);
            return new Token<TokenKind>(kind, current.Start, current.Length);
        }
    }

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
