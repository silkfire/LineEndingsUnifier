namespace LineEndingsUnifier
{
    using Microsoft.VisualStudio.Text;

    using System.Linq;

    internal static class LineEndingsChanger
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

        public static void ChangeLineEndings(LineEndingFinderFactoryProvider lineEndingFinderFactoryProvider, ITextBuffer textBuffer, LineEnding lineEnding, out int? numberOfIndividualChanges, out int? numberOfAllLineEndings, bool writeReport)
        {
            numberOfIndividualChanges = 0;

            var lineEndingFinderFactory = lineEndingFinderFactoryProvider.GetLineEndingFinderFactory();
            var lineEndingFinder = lineEndingFinderFactory.Create(textBuffer.CurrentSnapshot);
            var allLineEndingMatches = lineEndingFinder.FindAll().ToArray();

            numberOfAllLineEndings = writeReport ? allLineEndingMatches.Length : null as int?;

            if (allLineEndingMatches.Length > 0)
            {
                var windowsLineEndingFinderFactory = lineEndingFinderFactoryProvider.GetWindowsLineEndingFinderFactory();
                var windowsLineEndingFinder = windowsLineEndingFinderFactory.Create(textBuffer.CurrentSnapshot);
                var numberOfWindowsLineEndings = windowsLineEndingFinder.FindAll().Count();

                var linuxLineEndingFinderFactory = lineEndingFinderFactoryProvider.GetLinuxLineEndingFinderFactory();
                var linuxLineEndingFinder = linuxLineEndingFinderFactory.Create(textBuffer.CurrentSnapshot);
                var numberOfLinuxLineEndings = linuxLineEndingFinder.FindAll().Count() - numberOfWindowsLineEndings;

                var macintoshLineEndingFinderFactory = lineEndingFinderFactoryProvider.GetMacinotshLineEndingFinderFactory();
                var macintoshLineEndingFinder = macintoshLineEndingFinderFactory.Create(textBuffer.CurrentSnapshot);
                var numberOfMacintoshLineEndings = macintoshLineEndingFinder.FindAll().Count() - numberOfWindowsLineEndings;

                string lineEndingReplacement = null;

                switch (lineEnding)
                {
                    case LineEnding.Windows:
                        lineEndingReplacement = LineEndingSearchPattern.Windows;
                        numberOfIndividualChanges = numberOfLinuxLineEndings + numberOfMacintoshLineEndings;

                        break;
                    case LineEnding.Linux:
                        lineEndingReplacement = LineEndingSearchPattern.Linux;
                        numberOfIndividualChanges = numberOfWindowsLineEndings + numberOfMacintoshLineEndings;

                        break;
                    case LineEnding.Macintosh:
                        lineEndingReplacement = LineEndingSearchPattern.Macintosh;
                        numberOfIndividualChanges = numberOfWindowsLineEndings + numberOfLinuxLineEndings;

                        break;
                    case LineEnding.Dominant:
                        if (numberOfWindowsLineEndings > numberOfLinuxLineEndings &&
                            numberOfWindowsLineEndings > numberOfMacintoshLineEndings)
                        {
                            lineEndingReplacement = LineEndingSearchPattern.Windows;
                            numberOfIndividualChanges = numberOfLinuxLineEndings + numberOfMacintoshLineEndings;
                        }
                        else if (numberOfLinuxLineEndings > numberOfWindowsLineEndings &&
                                 numberOfLinuxLineEndings > numberOfMacintoshLineEndings)
                        {
                            lineEndingReplacement = LineEndingSearchPattern.Linux;
                            numberOfIndividualChanges = numberOfWindowsLineEndings + numberOfMacintoshLineEndings;
                        }
                        else
                        {
                            lineEndingReplacement = LineEndingSearchPattern.Macintosh;
                            numberOfIndividualChanges = numberOfWindowsLineEndings + numberOfLinuxLineEndings;
                        }

                        break;
                }

                using (var textEdit = textBuffer.CreateEdit())
                {
                    foreach (var lineEndingMatch in allLineEndingMatches)
                    {
                        textEdit.Replace(lineEndingMatch, lineEndingReplacement);
                    }

                    textEdit.Apply();
                }
            }
        }
    }
}
