namespace Lox;

public static class StringExtensions
{
    extension(string value)
    {
        public string ReplaceControlCharacters()
        {
            return string.Create(value.Length, value, (span, value) =>
            {
                for (int i = 0; i < value.Length; i++)
                {
                    span[i] = value[i] <= 32 ? (char)(value[i] + 0x2400) : value[i];
                }
            });
        }

        public string Quoted(string suffix = "") => @$"""{value.ReplaceControlCharacters()}""{suffix}";
    }
}