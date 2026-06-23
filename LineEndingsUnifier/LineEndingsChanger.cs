namespace LineEndingsUnifier
{
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Operations;

    using System;
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

        // Restricted subset of LineEnding shown in the options dropdown (excludes None).
        // Members are anchored to their LineEnding counterparts so a (LineEnding) cast stays
        // correct even if either enum is reordered.
        public enum LineEndingList
        {
            Windows = LineEnding.Windows,
            Linux = LineEnding.Linux,
            Macintosh = LineEnding.Macintosh,
            Dominant = LineEnding.Dominant
        }

        public static void ChangeLineEndings(LineEndingFinderFactoryProvider lineEndingFinderFactoryProvider, ITextBuffer textBuffer, IWpfTextView textView, LineEnding desiredLineEnding, out int? numberOfChangedLineEndings, out int? numberOfLineEndingsOfAnyType, bool writeReport)
        {
            var lineEndingFinderFactory = lineEndingFinderFactoryProvider.GetLineEndingFinderFactory();
            var lineEndingFinder = lineEndingFinderFactory.Create(textBuffer.CurrentSnapshot);
            var allLineEndingMatches = lineEndingFinder.FindAll().ToArray();

            int? numberOfChangedLineEndingsInternal = null;
            numberOfLineEndingsOfAnyType = writeReport ? allLineEndingMatches.Length : null as int?;

            if (allLineEndingMatches.Length > 0)
            {
                string desiredLineEndingReplacement;
                IFinderFactory unexpectedLineEndingFinderFactory;

                switch (desiredLineEnding)
                {
                    case LineEnding.Windows:
                        desiredLineEndingReplacement = LineEndingSearchPattern.Windows;
                        unexpectedLineEndingFinderFactory = lineEndingFinderFactoryProvider.GetNonWindowsLineEndingFinderFactory();

                        break;
                    case LineEnding.Linux:
                        desiredLineEndingReplacement = LineEndingSearchPattern.Linux;
                        unexpectedLineEndingFinderFactory = lineEndingFinderFactoryProvider.GetNonLinuxLineEndingFinderFactory();

                        break;
                    case LineEnding.Macintosh:
                        desiredLineEndingReplacement = LineEndingSearchPattern.Macintosh;
                        unexpectedLineEndingFinderFactory = lineEndingFinderFactoryProvider.GetNonMacintoshLineEndingFinderFactory();

                        break;
                    case LineEnding.Dominant:
                        // Classify the matches we already found in a single pass instead of
                        // running three additional full-buffer regex scans. A "\r\n" match has
                        // length 2 (Windows); a length-1 match is either "\r" (Macintosh) or "\n" (Linux).
                        var snapshot = textBuffer.CurrentSnapshot;
                        var numberOfWindowsLineEndings = 0;
                        var numberOfLinuxLineEndings = 0;
                        var numberOfMacintoshLineEndings = 0;

                        foreach (var lineEndingMatch in allLineEndingMatches)
                        {
                            if (lineEndingMatch.Length == 2)
                            {
                                numberOfWindowsLineEndings++;
                            }
                            else if (snapshot[lineEndingMatch.Start] == '\r')
                            {
                                numberOfMacintoshLineEndings++;
                            }
                            else
                            {
                                numberOfLinuxLineEndings++;
                            }
                        }

                        // Each style wins only on a strict majority; Linux is the tie-break default
                        // (including the all-zero case), since converting an ambiguous file to Linux
                        // is far less surprising than converting it to Macintosh CRs.
                        if (numberOfWindowsLineEndings > numberOfLinuxLineEndings &&
                            numberOfWindowsLineEndings > numberOfMacintoshLineEndings)
                        {
                            desiredLineEndingReplacement = LineEndingSearchPattern.Windows;
                            unexpectedLineEndingFinderFactory = lineEndingFinderFactoryProvider.GetNonWindowsLineEndingFinderFactory();
                        }
                        else if (numberOfMacintoshLineEndings > numberOfWindowsLineEndings &&
                                 numberOfMacintoshLineEndings > numberOfLinuxLineEndings)
                        {
                            desiredLineEndingReplacement = LineEndingSearchPattern.Macintosh;
                            unexpectedLineEndingFinderFactory = lineEndingFinderFactoryProvider.GetNonMacintoshLineEndingFinderFactory();
                        }
                        else
                        {
                            desiredLineEndingReplacement = LineEndingSearchPattern.Linux;
                            unexpectedLineEndingFinderFactory = lineEndingFinderFactoryProvider.GetNonLinuxLineEndingFinderFactory();
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(desiredLineEnding), desiredLineEnding, "Unsupported line ending enum value");
                }


                var unexpectedLineEndingFinder = unexpectedLineEndingFinderFactory.Create(textBuffer.CurrentSnapshot);
                var unexpectedLineEndingMatches = unexpectedLineEndingFinder.FindAll().ToArray();
                if (unexpectedLineEndingMatches.Length > 0)
                {
                    var caretPositionBeforeEdit = textView.Caret.Position.BufferPosition;

                    var undoManager = textBuffer.Properties.GetProperty<ITextBufferUndoManager>(typeof(ITextBufferUndoManager));
                    using (var textEdit = undoManager.TextBuffer.CreateEdit(EditOptions.DefaultMinimalChange, 0, null))
                    using (var undo = undoManager.TextBufferUndoHistory.CreateTransaction("Unify Line Endings"))
                    {
                        foreach (var unexpectedLineEndingMatch in unexpectedLineEndingMatches)
                        {
                            textEdit.Replace(unexpectedLineEndingMatch, desiredLineEndingReplacement);
                        }

                        textEdit.Apply();
                        undo.Complete();
                    }

                    var caretPositionAfterEdit = caretPositionBeforeEdit.TranslateTo(textView.TextSnapshot, PointTrackingMode.Positive);
                    textView.Caret.MoveTo(caretPositionAfterEdit);
                }

                numberOfChangedLineEndingsInternal = unexpectedLineEndingMatches.Length;
            }

            numberOfChangedLineEndings = writeReport ? numberOfChangedLineEndingsInternal : null;
        }
    }
}
