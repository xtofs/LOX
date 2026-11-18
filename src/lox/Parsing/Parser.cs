namespace Lox.Parsing;

using System.Diagnostics.CodeAnalysis;

using Lox.Scanning;
using Lox.Syntax;

public class Parser
{
    private static bool TryParseNode(string fileName, [MaybeNullWhen(false)] out Node result, out int[] lineIndex,
        [NotNullWhen(false)] out ParseError? error)
    {
        var scanner = new Scanner(File.ReadAllText(fileName));
        var tokens = scanner.GetEnumerator();
        tokens.MoveNext(); // position on the first token
        lineIndex = scanner.LineIndex;
        return LoxGrammar.Program.TryParse(tokens, out result, out _, out error);
    }

    public static bool TryParse(string fileName, [MaybeNullWhen(false)] out Program program, out int[] lineIndex)
    {
        return TryParse(fileName, out program, out lineIndex, out _);
    }

    public static bool TryParse(string fileName, [MaybeNullWhen(false)] out Program program, out int[] lineIndex,
        [NotNullWhen(false)] out ParseError? error)
    {
        if (TryParseNode(fileName, out var result, out lineIndex, out error))
        {
            program = AstBuilder.ToProgram(result, lineIndex);

            return true;
        }
        program = null;
        lineIndex = [];
        return false;
    }
}