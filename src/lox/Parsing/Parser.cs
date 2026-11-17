namespace Lox.Parsing;

using System.Diagnostics.CodeAnalysis;

using Lox.Syntax;

public class Parser
{
    public static bool TryParse(string fileName, [MaybeNullWhen(false)] out Program program, out int[] lineIndex)
    {
        var scanner = new Scanner(File.ReadAllText(fileName));
        var tokens = scanner.GetEnumerator();

        tokens.MoveNext(); // position on the first token
        if (LoxGrammar.Program(tokens, out var result, out var remaining))
        {
            lineIndex = scanner.LineIndex;
            program = AstBuilder.ToProgram(result, lineIndex);

            return true;
        }
        program = null;
        lineIndex = [];
        return false;
    }
}