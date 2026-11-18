namespace Lox.Scanning;


public enum TokenKind
{
    // Single-character tokens.
    LEFT_PAREN, RIGHT_PAREN, LEFT_BRACE, RIGHT_BRACE,
    COMMA, DOT, MINUS, PLUS, SEMICOLON, SLASH, STAR,

    // One or two character tokens.
    BANG, BANG_EQUAL,
    EQUAL, EQUAL_EQUAL,
    GREATER, GREATER_EQUAL,
    LESS, LESS_EQUAL,

    // Literals.
    IDENTIFIER, STRING, NUMBER,

    // Keywords.
    AND, CLASS, ELSE, FALSE, FUN, FOR, IF, NIL, OR,
    PRINT, RETURN, SUPER, THIS, TRUE, VAR, WHILE,

    EOF
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

// public enum TokenCategory
// {
//     EndOfInput,
//     Whitespace,
//     Comment,
//     Number,
//     Identifier,
//     Comparison,
//     Operator,
//     Bracket,
//     Punctuation,
//     QuotedString
// }

public static class TokenKindExtensions
{
    extension(TokenKind kind)
    {
        public static TokenKind Parse(string name) => Enum.Parse<TokenKind>(name, true);
    }
}