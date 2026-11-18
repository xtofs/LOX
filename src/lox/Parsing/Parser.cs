namespace Lox.Parsing;

using System.Diagnostics.CodeAnalysis;

using Lox.Scanning;
using Lox.Syntax;

public class Parser
{
    private static bool TryParseNode(string fileName, [MaybeNullWhen(false)] out Node result, out int[] lineIndex)
    {
        var scanner = new Scanner(File.ReadAllText(fileName));
        var tokens = scanner.GetEnumerator();
        tokens.MoveNext(); // position on the first token
        lineIndex = scanner.LineIndex;
        return LoxGrammar.Program(tokens, out result, out var remaining);
    }

    public static bool TryParse(string fileName, [MaybeNullWhen(false)] out Program program, out int[] lineIndex)
    {
        if (TryParseNode(fileName, out var result, out lineIndex))
        {
            program = AstBuilder.ToProgram(result, lineIndex);

            return true;
        }
        program = null;
        lineIndex = [];
        return false;
    }
}