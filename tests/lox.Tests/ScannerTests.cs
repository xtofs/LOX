using Lox.Scanning;

public class ScannerTests
{

    [Theory]
    [InlineData("identifiers.lox")]
    [InlineData("keywords.lox")]
    [InlineData("numbers.lox")]
    [InlineData("punctuators.lox")]
    [InlineData("strings.lox")]
    [InlineData("whitespace.lox")]

    public void ExampleFileTokenization(string filePath)
    {
        filePath = Path.Combine("../../../../../data/", filePath);
        var expected = File
            .ReadLines(filePath)
            .Where(line => line.StartsWith("//"))
            .Select(line => line.StripPrefix("// expect: "))
            .Select(line => line.Split(' ', StringSplitOptions.TrimEntries))
            .Select(parts => (Enum.Parse<TokenKind>(parts[0]), parts[1]))
            .ToArray();

        var actualTokens = new List<(TokenKind Kind, string Value)>();
        var source = File.ReadAllText(filePath);
        var scanner = new Scanner(source);
        foreach (var token in scanner)
        {
            var (kind, value) = (token.Kind, token.GetValue(source));
            actualTokens.Add((kind, value));
        }

        Assert.Equal(expected.Length, actualTokens.Count); //  , "The number of expected tokens does not match the number of actual tokens.");

        for (var i = 0; i < expected.Length; i++)
        {
            var (expectedKind, expectedValue) = (expected[i].Item1, expected[i].Item2);
            var (actualKind, actualValue) = actualTokens[i];
            Assert.Equal(expectedKind, actualKind);
            Assert.Equal(expectedValue, actualValue);
        }
    }
}
