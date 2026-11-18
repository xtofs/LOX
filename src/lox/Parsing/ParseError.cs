namespace Lox.Parsing;

using Lox.Scanning;

public readonly record struct TokenExpectation(TokenKind Kind, string? Value = null)
{
    public override string ToString() => Value is null ? Kind.ToString() : $"{Kind} {Value.Quoted()}";
}

public sealed record ParseRuleContext(TokenExpectation Expected, IReadOnlyList<string> RulePath);

public sealed record ParseToken(TokenKind Kind, string Value, int Start)
{
    public override string ToString() => Kind == TokenKind.EOF ? "EOF" : $"{Kind} {Value.Quoted()}";
}

/// <summary>A snapshot of the furthest parse failure; line and column are one-based, Start is zero-based.</summary>
public sealed record ParseError(
    int Start,
    int Line,
    int Column,
    IReadOnlyList<TokenExpectation> Expected,
    IReadOnlyList<ParseToken> Remaining)
{
    public IReadOnlyList<ParseRuleContext> Contexts { get; init; } = [];

    public override string ToString()
    {
        var expected = Expected.Count == 0 ? "a matching grammar rule" : string.Join(" or ", Expected);
        var contexts = string.Concat(Contexts
            .Where(static context => context.RulePath.Count > 0)
            .Select(static context => $"{Environment.NewLine}Context: {string.Join(" > ", context.RulePath)} (expected {context.Expected})"));
        return $"({Line},{Column}): expected {expected}; found {Remaining[0]}."
            + contexts
            + $"{Environment.NewLine}Remaining: [{string.Join(", ", Remaining)}]";
    }
}

/// <summary>Per-parse state shared by combinators; use Grammar.TryParse to collect a ParseError.</summary>
public sealed class ParseDiagnostics
{
    private int _start = -1;
    private readonly List<string> _rules = [];
    private readonly List<ParseRuleContext> _contexts = [];

    internal ParseDiagnostics() { }

    internal void EnterRule(string name) => _rules.Add(name);

    internal void ExitRule() => _rules.RemoveAt(_rules.Count - 1);

    internal void Record(int start, TokenExpectation expected)
    {
        if (start < _start)
        {
            return;
        }
        if (start > _start)
        {
            _start = start;
            _contexts.Clear();
        }
        if (!_contexts.Any(context => context.Expected == expected && context.RulePath.SequenceEqual(_rules)))
        {
            _contexts.Add(new ParseRuleContext(expected, _rules.ToArray()));
        }
    }

    internal ParseError CreateError(TokenEnumerator tokens)
    {
        while (tokens.Current.Start < _start && tokens.MoveNext()) { }

        var start = tokens.Current.Start;
        var source = tokens.Source.AsSpan(0, start);
        var line = source.Count('\n') + 1;
        var column = start - source.LastIndexOf('\n');
        var remaining = new List<ParseToken>();
        do
        {
            var token = tokens.Current;
            remaining.Add(new ParseToken(token.Kind, tokens.GetValue(token), token.Start));
            if (token.Kind == TokenKind.EOF)
            {
                break;
            }
        } while (tokens.MoveNext());

        return new ParseError(start, line, column,
            _contexts.Select(static context => context.Expected).Distinct().ToArray(), remaining.ToArray())
        {
            Contexts = _contexts.ToArray()
        };
    }
}
