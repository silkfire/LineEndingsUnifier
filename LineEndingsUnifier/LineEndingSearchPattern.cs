namespace LineEndingsUnifier
{
    internal static class LineEndingSearchPattern
    {
        public const string Any = "\r\n?|\n";
        public const string ConsecutiveWhiteSpaceFollowedByAny = @"[^\S\r\n]+(?=\r\n?|\n)";

        public const string Windows = "\r\n";
        public const string Linux = "\n";
        public const string Macintosh = "\r";

        public const string NonWindows = "\r(?!\n)|(?<!\r)\n";
        public const string NonLinux = "\r\n|\r(?!\n)";
        public const string NonMacintosh = "\r\n|(?<!\r)\n";
    }
}
