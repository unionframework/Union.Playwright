namespace Union.Playwright.Extensions
{
    public static class StringExtensions
    {
        public static string CutFirst(this string s)
        {
            return s.Substring(1, s.Length - 1);
        }

        public static string CutFirst(this string s, char symbol)
        {
            return s.StartsWith(symbol.ToString()) ? s.Substring(1, s.Length - 1) : s;
        }

        public static string CutLast(this string s) => s.Substring(0, s.Length - 1);

        public static string CutLast(this string s, char symbol)
        {
            return s.EndsWith(symbol.ToString()) ? s.Substring(0, s.Length - 1) : s;
        }
    }
}