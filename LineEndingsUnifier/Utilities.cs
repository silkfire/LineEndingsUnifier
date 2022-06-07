namespace LineEndingsUnifier
{
    public static class Utilities
    {
        public static string GetNewlineString(LineEndingsChanger.LineEndings lineEnding)
        {
            switch (lineEnding)
            {
                case LineEndingsChanger.LineEndings.Macintosh:
                    return "\r";
                case LineEndingsChanger.LineEndings.Windows:
                    return "\r\n";
                case LineEndingsChanger.LineEndings.Linux:
                default:
                    return "\n";
            }
        }
    }
}
