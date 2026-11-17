namespace Lox;

using System.Runtime.CompilerServices;
using System.Text;

using Microsoft.Extensions.Logging;


public class Logger(LogLevel enabledLevel = LogLevel.Trace)
{
    public LogLevel EnabledLevel { get; } = enabledLevel;

    public void LogMessage(LogLevel level, LogInterpolatedStringHandler builder)
    {
        if (level < EnabledLevel) return;
        var color = level switch
        {
            LogLevel.Debug => "\e[34m",
            LogLevel.Information => "\e[32m",
            LogLevel.Error => "\e[31m",
            _ => "\e[0m"
        };
        Console.Error.WriteLine($"{color}{level}\e[0m: {builder.ToString()}");
    }


    public void LogDebug(LogInterpolatedStringHandler message) =>
        LogMessage(LogLevel.Debug, message);

    public void LogInfo(LogInterpolatedStringHandler message) =>
        LogMessage(LogLevel.Information, message);

    public void LogError(LogInterpolatedStringHandler message) =>
        LogMessage(LogLevel.Error, message);

    [InterpolatedStringHandler]
    public readonly struct LogInterpolatedStringHandler(int literalLength, int formattedCount)
    {
        readonly StringBuilder _builder = new StringBuilder(literalLength);
        private readonly int _formattedCount = formattedCount;

        public void AppendLiteral(string s)
        {
            _builder.Append(s);
        }

        public void AppendFormatted<T>(T t)
        {
            _builder.Append(t?.ToString());
        }

        public void AppendFormatted<T>(T t, string format)
            where T : IFormattable
        {
            _builder.Append(t?.ToString(format, null));
        }

        public override string ToString() => _builder.ToString();
    }
}
