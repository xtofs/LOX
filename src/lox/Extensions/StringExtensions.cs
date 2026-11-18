namespace Lox;

public static class StringExtensions
{
    extension(string value)
    {
        public string ReplaceControlCharacters(bool includeSpace = false)
        {
            return string.Create(value.Length, value, (span, value) =>
            {
                var threshold = includeSpace ? 32 : 31;
                for (int i = 0; i < value.Length; i++)
                {
                    span[i] = value[i] <= threshold ? (char)(value[i] + 0x2400) : value[i];
                }
            });
        }

        public string Quoted(string suffix = "") => @$"""{value.ReplaceControlCharacters()}""{suffix}";
    }

    public static string StripPrefix(this string str, string prefix)
    {
        return str.StartsWith(prefix) ? str[prefix.Length..] : str;
    }
}