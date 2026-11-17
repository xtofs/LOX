using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using Lox;
using Lox.Parsing;

using Microsoft.Extensions.Logging;

internal partial class Program
{
    private static int Main(string[] args)
    {
        if (!Args.TryParse(args, out var options))
        {
            return 1;
        }

        if (!Parser.TryParse(options.FileName, out var program, out var lineIndex))
        {
            return 2;
        }

        if (options.ShowSyntax)
        {
            ShowSyntax(program, lineIndex, options.FileName);
        }

        var logger = new Logger(LogLevel.Information);
        var interpreter = new Interpreter(logger, lineIndex, options.FileName);
        interpreter.Eval(program);
        return 0;
    }

    private static void ShowSyntax(Lox.Program program, int[] lineIndex, string fileName)
    {
        Console.WriteLine(AstFormatter.Format(program));
    }
}

public record class Args(string FileName, bool ShowSyntax)
{
    public static bool TryParse(string[] args, [MaybeNullWhen(false)] out Args result)
    {
        if (args.Length == 0)
        {
            result = null;
            return false;
        }

        result = new Args(
            FileName: (args.Length == 0) ? "" : args[0],
            ShowSyntax: args.Contains("--show-syntax")
        );
        return true;
    }
}
