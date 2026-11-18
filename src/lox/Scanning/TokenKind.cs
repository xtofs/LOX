namespace Lox.Scanning;


[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "TokenKind uses all caps naming convention")]
public enum TokenKind
{
    // Single-character tokens.
    //  bracket tokens
    LEFT_PAREN, RIGHT_PAREN, LEFT_BRACE, RIGHT_BRACE,
    //  punctuation tokens
    COMMA, DOT, SEMICOLON, COLON,
    //  operator tokens
    MINUS, PLUS, SLASH, STAR, BACKSLASH,

    // One or two character comparison tokens. /[<>=!]=?/
    BANG, BANG_EQUAL,
    EQUAL, EQUAL_EQUAL,
    GREATER, GREATER_EQUAL,
    LESS, LESS_EQUAL,

    // Literals.
    IDENTIFIER, STRING, NUMBER,

    // Keywords.
    AND, CLASS, ELSE, FALSE, FUN, FOR, IF, NIL, OR,
    PRINT, RETURN, SUPER, THIS, TRUE, VAR, WHILE,

    EOF,
}

public enum TokenCategory
{
    EndOfInput, // has to be the first category to correctly identify end of input

    Whitespace,
    Comment,

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