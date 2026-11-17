using System.Diagnostics.CodeAnalysis;

namespace Lox;

public delegate bool GrammarRule(TokenEnumerator tokens, [MaybeNullWhen(false)] out Node result, out TokenEnumerator remaining);

public static class Grammar
{
    public static GrammarRule Seq(string tag, GrammarRule first, GrammarRule second, params GrammarRule[] remainder)
    {
        GrammarRule[] rules = [first, second, .. remainder];
        return (tokens, [MaybeNullWhen(false)] out result, out remaining) =>
        {
            var currentTokens = tokens;
            var results = new List<Node>();
            foreach (var rule in rules)
            {
                if (!rule(currentTokens, out var res, out var rem))
                {
                    result = null;
                    remaining = tokens;
                    return false;
                }
                results.Add(res);
                currentTokens = rem;
            }
            result = SequenceNode.Of(tag, results);
            remaining = currentTokens;
            return true;
        };
    }

    public static GrammarRule Alt(GrammarRule first, GrammarRule second, params GrammarRule[] remainder)
    {
        GrammarRule[] rules = [first, second, .. remainder];
        return (tokens, [MaybeNullWhen(false)] out result, out remaining) =>
        {
            foreach (var rule in rules)
            {
                if (rule(tokens, out var res, out var rem))
                {
                    result = res;
                    remaining = rem;
                    return true;
                }
            }
            result = null;
            remaining = tokens;
            return false;
        };
    }

    public static GrammarRule Opt(GrammarRule rule)
    {
        return (tokens, [MaybeNullWhen(false)] out result, out remaining) =>
        {
            if (rule(tokens, out var res, out var rem))
            {
                result = res;
                remaining = rem;
                return true;
            }
            result = Node.Empty;
            remaining = tokens;
            return true;
        };
    }

    public static GrammarRule ZeroOrMore(GrammarRule rule)
    {
        return (tokens, [MaybeNullWhen(false)] out result, out remaining) =>
        {
            var currentTokens = tokens;
            var results = new List<Node>();
            while (rule(currentTokens, out var res, out var rem))
            {
                results.Add(res);
                currentTokens = rem;
            }
            result = SequenceNode.Of("rep", results);
            remaining = currentTokens;
            return true;
        };
    }

    public static GrammarRule Select(this GrammarRule rule, Func<Node, bool> predicate)
    {
        return (tokens, [MaybeNullWhen(false)] out result, out remaining) =>
        {
            if (rule(tokens, out var res, out var rem))
            {
                if (predicate(res))
                {
                    result = res;
                    remaining = rem;
                    return true;
                }
            }
            result = null;
            remaining = tokens;
            return false;
        };
    }

    public static GrammarRule OneOrMore(GrammarRule rule)
    {
        return ZeroOrMore(rule).Select(node => node is SequenceNode seq && seq.Children.Count > 0);
    }

    /// <summary>
    /// Parses 'element ( separator element )*' and produces a single flat
    /// sequence: element, separator, element, ... A dangling separator with no
    /// element after it is left unconsumed. A lone element collapses to itself.
    /// </summary>
    public static GrammarRule SeparatedBy(string tag, GrammarRule element, GrammarRule separator)
    {
        return (tokens, [MaybeNullWhen(false)] out result, out remaining) =>
        {
            if (!element(tokens, out var first, out var currentTokens))
            {
                result = null;
                remaining = tokens;
                return false;
            }
            var results = new List<Node> { first };
            while (separator(currentTokens, out var sep, out var afterSeparator)
                && element(afterSeparator, out var next, out var afterElement))
            {
                results.Add(sep);
                results.Add(next);
                currentTokens = afterElement;
            }
            result = SequenceNode.Of(tag, results);
            remaining = currentTokens;
            return true;
        };
    }

    public static GrammarRule Expect(TokenKind tokenKind)
    {
        return (tokens, [MaybeNullWhen(false)] out result, out remaining) =>
        {
            if (tokens.Current.Kind == tokenKind)
            {
                var tok = tokens.Current;
                result = new TokenNode(tokenKind, tokens.GetValue(tok), tok.Start, tokens.GetLeadingTrivia());
                remaining = tokens; remaining.MoveNext();
                return true;
            }
            result = null;
            remaining = tokens;
            return false;
        };
    }

    public static GrammarRule Expect(TokenKind expectedKind, string expectedValue)
    {
        return (tokens, [MaybeNullWhen(false)] out result, out remaining) =>
        {
            if (tokens.Current.Kind == expectedKind)
            {
                var value = tokens.GetValue(tokens.Current);
                var start = tokens.Current.Start;
                if (value == expectedValue)
                {
                    result = new TokenNode(expectedKind, value, start, tokens.GetLeadingTrivia());
                    remaining = tokens; remaining.MoveNext();
                    return true;
                }
            }
            result = null;
            remaining = tokens;
            return false;
        };
    }

    public static GrammarRule Lazy(Func<GrammarRule> ruleFactory)
    {
        return (tokens, [MaybeNullWhen(false)] out result, out remaining) =>
        {
            var rule = ruleFactory();
            return rule(tokens, out result, out remaining);
        };
    }

}