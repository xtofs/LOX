using System.Text;

namespace Lox;

public static class TreeFormatter
{
    public static string Format(Node node)
    {
        var sb = new StringBuilder();
        Format(sb, node, "", true);
        return sb.ToString();
    }

    // Constants for drawing lines and spaces
    private const string _cross = " ├─";
    private const string _corner = " └─";
    private const string _vertical = " │ ";
    private const string _space = "   ";

    public static void Format(StringBuilder sb, Node node, string indent, bool isLast)
    {
        sb.Append(indent);
        if (isLast)
        {
            sb.Append(_corner);
            indent += _space;
        }
        else
        {
            sb.Append(_cross);
            indent += _vertical;
        }
        if (node is TokenNode tn)
        {
            sb.AppendLine($"{tn.Kind} {tn.Value.Quoted()}");
        }
        else
        {
            sb.AppendLine(node.Tag);
        }
        if (node is SequenceNode sequence)
        {
            for (int i = 0; i < sequence.Children.Count; i++)
            {
                var isLastChild = i == sequence.Children.Count - 1;
                Format(sb, sequence.Children[i], indent, isLastChild);
            }
        }
    }
}