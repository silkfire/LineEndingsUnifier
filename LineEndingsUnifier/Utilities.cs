namespace LineEndingsUnifier
{
    public static class Utilities
    {
        public static string GetNewlineString(LineEndingsChanger.LineEnding lineEnding)
        {
            switch (lineEnding)
            {
                case LineEndingsChanger.LineEnding.Macintosh:
                    return "\r";
                case LineEndingsChanger.LineEnding.Windows:
                    return "\r\n";
                case LineEndingsChanger.LineEnding.Linux:
                default:
                    return "\n";
            }
        }
    }
}
