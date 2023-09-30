namespace LineEndingsUnifier
{
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Operations;

    using System.Linq;

    internal static class LineEndingsChanger
    {
        //private const int SB_VERT = 1;

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

        public static void ChangeLineEndings(LineEndingFinderFactoryProvider lineEndingFinderFactoryProvider, ITextBuffer textBuffer, IWpfTextView textView, LineEnding desiredLineEnding, out int? numberOfChangedLineEndings, out int? numberOfLineEndingsOfAnyType, bool writeReport)
        {
            var lineEndingFinderFactory = lineEndingFinderFactoryProvider.GetLineEndingFinderFactory();
            var lineEndingFinder = lineEndingFinderFactory.Create(textBuffer.CurrentSnapshot);
            var allLineEndingMatches = lineEndingFinder.FindAll().ToArray();

            int? numberOfChangedLineEndingsInternal = null;
            numberOfLineEndingsOfAnyType = writeReport ? allLineEndingMatches.Length : null as int?;

            if (allLineEndingMatches.Length > 0)
            {
                string desiredLineEndingReplacement = null;
                IFinderFactory unexpectedLineEndingFinderFactory = null;

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
                        var windowsLineEndingFinderFactory = lineEndingFinderFactoryProvider.GetWindowsLineEndingFinderFactory();
                        var windowsLineEndingFinder = windowsLineEndingFinderFactory.Create(textBuffer.CurrentSnapshot);
                        var numberOfWindowsLineEndings = windowsLineEndingFinder.FindAll().Count();

                        var linuxLineEndingFinderFactory = lineEndingFinderFactoryProvider.GetLinuxLineEndingFinderFactory();
                        var linuxLineEndingFinder = linuxLineEndingFinderFactory.Create(textBuffer.CurrentSnapshot);
                        var numberOfLinuxLineEndings = linuxLineEndingFinder.FindAll().Count();

                        var macintoshLineEndingFinderFactory = lineEndingFinderFactoryProvider.GetMacintoshLineEndingFinderFactory();
                        var macintoshLineEndingFinder = macintoshLineEndingFinderFactory.Create(textBuffer.CurrentSnapshot);
                        var numberOfMacintoshLineEndings = macintoshLineEndingFinder.FindAll().Count();


                        if (numberOfWindowsLineEndings > numberOfLinuxLineEndings &&
                            numberOfWindowsLineEndings > numberOfMacintoshLineEndings)
                        {
                            desiredLineEndingReplacement = LineEndingSearchPattern.Windows;
                            unexpectedLineEndingFinderFactory = lineEndingFinderFactoryProvider.GetNonWindowsLineEndingFinderFactory();
                        }
                        else if (numberOfLinuxLineEndings > numberOfWindowsLineEndings &&
                                 numberOfLinuxLineEndings > numberOfMacintoshLineEndings)
                        {
                            desiredLineEndingReplacement = LineEndingSearchPattern.Linux;
                            unexpectedLineEndingFinderFactory = lineEndingFinderFactoryProvider.GetNonLinuxLineEndingFinderFactory();
                        }
                        else
                        {
                            desiredLineEndingReplacement = LineEndingSearchPattern.Macintosh;
                            unexpectedLineEndingFinderFactory = lineEndingFinderFactoryProvider.GetNonMacintoshLineEndingFinderFactory();
                        }

                        break;
                }


                var unexpectedLineEndingFinder = unexpectedLineEndingFinderFactory.Create(textBuffer.CurrentSnapshot);
                var unexpectedLineEndingMatches = unexpectedLineEndingFinder.FindAll().ToArray();
                if (unexpectedLineEndingMatches.Length > 0)
                {
                    var caretPositionBeforeEdit = textView.Caret.Position.BufferPosition;
                    //var textViewLinesTopBeforeEdit = textView.TextViewLines.FirstVisibleLine.Top;
                    //var textViewLineCountTopBeforeEdit = textView.TextViewLines.Count;

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

                    //var textViewLineCountTopAfterEdit = textView.TextViewLines.Count;

                    ////textView.DisplayTextLineContainingBufferPosition(textView.TextViewLines.FirstVisibleLine.Start, textViewLinesTopBeforeEdit, ViewRelativePosition.Top);
                    ////textView.ViewScroller.ScrollViewportVerticallyByPixels(-16);
                    //textView.ViewScroller.ScrollViewportVerticallyByLines(ScrollDirection.Down, 1);
                }

                numberOfChangedLineEndingsInternal = unexpectedLineEndingMatches.Length;
            }

            numberOfChangedLineEndings = writeReport ? numberOfChangedLineEndingsInternal : null;
        }
    }
}
