namespace Lox;

public closed record Node(string Tag)
{
    /// <summary>Result of an Opt rule whose inner rule didn't match.</summary>
    public static Node Empty => EmptyNode.Instance;

    public abstract int Start { get; }
}

public sealed record EmptyNode : Node
{
    public static EmptyNode Instance { get; } = new();

    private EmptyNode() : base("Empty") { }

    public override int Start => 0;

    public override string ToString() => "ε";
}

/// <summary>A single matched token (produced by Expect). LeadingTrivia holds the
/// whitespace between the previous token and this one, so that concatenating
/// LeadingTrivia + Value over the tree's tokens reproduces the source exactly.</summary>
public sealed record TokenNode(TokenKind Kind, string Value, int start, string LeadingTrivia = "") : Node(Kind.ToString())
{
    public override string ToString() => Value;
    public override int Start => start;
}

/// <summary>
/// An ordered list of child nodes (produced by Seq and Rep).
/// </summary>
public sealed record SequenceNode(string Tag, IReadOnlyList<Node> Children) : Node(Tag)
{
    public override int Start => Children[0].Start;

    /// <summary>
    /// Creates a simplified sequence: empty children (ε and empty repetitions)
    /// are dropped and a single remaining child is returned unwrapped.
    /// </summary>
    public static Node Of(string tag, List<Node> children)
    {
        children.RemoveAll(static child => child is EmptyNode);
        return children.Count switch
        {
            0 => Empty,
            1 => children[0],
            _ => new SequenceNode(tag, children),
        };
    }

    public override string ToString() => $"({string.Join(" ", Children)})";
}
