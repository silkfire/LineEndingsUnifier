namespace LineEndingsUnifier
{
    using System.Text.RegularExpressions;

    public static class LineEndingsChanger
    {
        public enum LineEnding
        {
            Windows,
            Linux,
            Macintosh,
            Dominant,
            None
        }

        public enum LineEndingList
        {
            Windows,
            Linux,
            Macintosh,
            Dominant
        }

        private const string LineEndingPattern = "\r\n?|\n";

        private const string WindowsLineEnding = "\r\n";

        private const string LinuxLineEnding = "\n";

        private const string MacintoshLineEnding = "\r";

        public static string ChangeLineEndings(string text, LineEnding lineEnding, ref int numberOfChanges, out int numberOfIndividualChanges, out int numberOfAllLineEndings)
        {
            numberOfIndividualChanges = 0;

            var replacementString = string.Empty;

            numberOfAllLineEndings = Regex.Matches(text, LineEndingPattern).Count;
            var numberOfWindowsLineEndings = Regex.Matches(text, WindowsLineEnding).Count;
            var numberOfLinuxLineEndings = Regex.Matches(text, LinuxLineEnding).Count - numberOfWindowsLineEndings;
            var numberOfMacintoshLineEndings = Regex.Matches(text, MacintoshLineEnding).Count - numberOfWindowsLineEndings;

            switch (lineEnding)
            {
                case LineEnding.Linux:
                    replacementString = LinuxLineEnding;
                    numberOfIndividualChanges = numberOfWindowsLineEndings + numberOfMacintoshLineEndings;
                    break;
                case LineEnding.Windows:
                    replacementString = WindowsLineEnding;
                    numberOfIndividualChanges = numberOfLinuxLineEndings + numberOfMacintoshLineEndings;
                    break;
                case LineEnding.Macintosh:
                    replacementString = MacintoshLineEnding;
                    numberOfIndividualChanges = numberOfWindowsLineEndings + numberOfLinuxLineEndings;
                    break;
                case LineEnding.Dominant:
                    if (numberOfWindowsLineEndings > numberOfLinuxLineEndings && numberOfWindowsLineEndings > numberOfMacintoshLineEndings)
                    {
                        replacementString = WindowsLineEnding;
                        numberOfIndividualChanges = numberOfLinuxLineEndings + numberOfMacintoshLineEndings;
                    }
                    else if (numberOfLinuxLineEndings > numberOfWindowsLineEndings && numberOfLinuxLineEndings > numberOfMacintoshLineEndings)
                    {
                        replacementString = LinuxLineEnding;
                        numberOfIndividualChanges = numberOfWindowsLineEndings + numberOfMacintoshLineEndings;
                    }
                    else
                    {
                        replacementString = MacintoshLineEnding;
                        numberOfIndividualChanges = numberOfWindowsLineEndings + numberOfLinuxLineEndings;
                    }

                    break;
            }

            var modifiedText = Regex.Replace(text, LineEndingPattern, replacementString);

            numberOfChanges += numberOfIndividualChanges;

            return modifiedText;
        }
    }
}
