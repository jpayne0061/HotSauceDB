namespace HotSauceDb.Helpers
{
    public static class ParserExtensions
    {
        public static string RemoveNewLines(this string str)
        {
            str = str.Replace("\r\n", "");
            str = str.Replace("\n", "");
            str = str.Replace("\r", "");

            return str;
        }
    }
}
