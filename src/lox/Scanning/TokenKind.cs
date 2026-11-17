namespace Lox;

public enum TokenKind
{
    EndOfInput,
    Whitespace,
    Number,
    Identifier,
    Comparison,
    Operator,
    Bracket,
    Punctuation,
    QuotedString
}

public static class TokenKindExtensions
{
    extension(TokenKind kind)
    {
        public static TokenKind Parse(string name) => Enum.Parse<TokenKind>(name, true);
    }
}