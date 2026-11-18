using System.CommandLine;
using System.Diagnostics.CodeAnalysis;

using Lox;
using Lox.Parsing;
using Lox.Scanning;
using Lox.Syntax;

using Microsoft.Extensions.Logging;

internal partial class Program
{
    private static int Main(string[] args)
    {
        var rootCommand = new RootCommand("Lox command line");
        var fileArgument = new Argument<FileInfo>("file") { Arity = ArgumentArity.ExactlyOne, Description = "The input file to scan" };
        var levelOption = new Option<LogLevel>("--level")
        {
            Description = "The log level",
            DefaultValueFactory = _ => LogLevel.Warning
        };

        AddSubcommand("scan", "tokenize the input file and display the tokens.", ScanAndShow);
        AddSubcommand("syntax", "parse the input file into a syntax tree and display it.", SyntaxAndShow);
        AddSubcommand("parse", "parse the input file into the AST and display it.", ParseAndShow);
        AddSubcommand("run", "parse and run the input file.", Run);

        var result = rootCommand.Parse(args);
        return result.Invoke();


        Command AddSubcommand(string name, string description, Action<FileInfo, LogLevel> action)
        {
            var command = new Command(name, description) { fileArgument, levelOption };
            command.SetAction(parseResult => action(
                parseResult.GetValue(fileArgument)!,
                parseResult.GetValue(levelOption)!));
            rootCommand.Subcommands.Add(command);
            return command;
        }
    }

    private static void ScanAndShow(FileInfo fileInfo, LogLevel level)
    {
        var fileName = fileInfo.GetRelativePath();
        var content = fileInfo.OpenText().ReadToEnd();
        var scanner = new Scanner(content);
        foreach (var token in scanner)
        {
            scanner.GetKindValuePosition(token, out var kind, out var value, out var position);

            Console.WriteLine($"{kind,-20} {fileName}({position.Line},{position.Column}) `{value.ReplaceControlCharacters()}`");
        }
    }

    private static void SyntaxAndShow(FileInfo fileInfo, LogLevel level)
    {
        var fileName = fileInfo.GetRelativePath();

        if (Parser.TryParse(fileName, out var program, out var lineIndex, out var error))
        {
            Console.WriteLine(AstFormatter.Format(program));
        }
        else
        {
            Console.Error.WriteLine($"{fileName}{error}");
        }
    }

    private static void ParseAndShow(FileInfo fileInfo, LogLevel level)
    {
        var fileName = fileInfo.GetRelativePath();

        if (Parser.TryParse(fileName, out var program, out var lineIndex, out var error))
        {
            Console.WriteLine(AstFormatter.Format(program));
        }
        else
        {
            Console.Error.WriteLine($"{fileName}{error}");
        }
    }

    private static void Run(FileInfo fileInfo, LogLevel level)
    {
        var fileName = fileInfo.GetRelativePath();
        var logger = new Logger(level);

        if (Parser.TryParse(fileName, out var program, out var lineIndex, out var error))
        {
            var interpreter = new Interpreter(logger, fileName, lineIndex);
            interpreter.Eval(program);
        }
        else
        {
            Console.Error.WriteLine($"{fileName}{error}");
        }
    }
}


static class FileInfoExtensions
{
    public static string GetRelativePath(this FileInfo fileInfo)
    {
        var cwd = Environment.CurrentDirectory;
        return Path.GetRelativePath(cwd, fileInfo.FullName);
    }
}