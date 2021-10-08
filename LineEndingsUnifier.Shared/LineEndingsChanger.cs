using System;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace JakubBielawa.LineEndingsUnifier
{
    public static class LineEndingsChanger
    {
        public enum LineEndings
        {
            Windows,
            Linux,
            Macintosh,
            Dominant,
            None
        }

        public enum LineEndingsList
        {
            Windows,
            Linux,
            Macintosh,
            Dominant
        }

        private const string WindowsLineEndings = "\r\n";

        private const string LinuxLineEndings = "\n";

        private const string MacintoshLineEndings = "\r";

        public static string ChangeLineEndings(string text, LineEndings lineEndings, out int numberOfIndividualChanges, out int numberOfAllLineEndings)
        {
            switch (lineEndings)
            {
                case LineEndings.Linux:
                    return ChangeLineEndingsToLinux(text, out numberOfIndividualChanges, out numberOfAllLineEndings);
                case LineEndings.Windows:
                    return ChangeLineEndingsToWindows(text, out numberOfIndividualChanges, out numberOfAllLineEndings);
                case LineEndings.Macintosh:
                    return ChangeLineEndingsToMacintosh(text, out numberOfIndividualChanges, out numberOfAllLineEndings);
                default:
                    var numberOfWindowsLineEndings = CountOccurances(text, WindowsLineEndings);
                    var numberOfLinuxLineEndings = CountOccurances(text, LinuxLineEndings) - numberOfWindowsLineEndings;
                    var numberOfMacintoshLineEndings = CountOccurances(text, MacintoshLineEndings) - numberOfWindowsLineEndings;
                    if (numberOfWindowsLineEndings >= numberOfLinuxLineEndings && numberOfWindowsLineEndings >= numberOfMacintoshLineEndings)
                    {
                        goto case LineEndings.Windows;
                    }
                    else if (numberOfLinuxLineEndings >= numberOfMacintoshLineEndings)
                    {
                        goto case LineEndings.Linux;
                    }
                    else
                    {
                        goto case LineEndings.Macintosh;
                    }
            }
        }

        private static readonly char[] lineSepChars = { '\r', '\n' };

        private static string ChangeLineEndingsToWindows(string text, out int numberOfIndividualChanges, out int numberOfAllLineEndings)
        {
            int pos = numberOfAllLineEndings = 0;
            int x;
            while ((x = text.IndexOfAny(lineSepChars, pos)) >= 0)
            {
                ++numberOfAllLineEndings;
                pos = x + 1;
                if (text[x] == '\r' && pos < text.Length && text[pos] == '\n')
                {
                    ++pos;
                }
                else
                {
                    // Capacity is text.Length * ~1.06, to account for lengthened line endings.
                    var sb = new StringBuilder(text, 0, x, text.Length + (text.Length >> 4));
                    sb.Append("\r\n");
                    numberOfIndividualChanges = 1;
                    while ((x = text.IndexOfAny(lineSepChars, pos)) >= 0)
                    {
                        ++numberOfAllLineEndings;
                        sb.Append(text, pos, x - pos);
                        sb.Append("\r\n");
                        pos = x + 1;
                        if (text[x] == '\r' && pos < text.Length && text[pos] == '\n')
                        {
                            ++pos;
                        }
                        else
                        {
                            ++numberOfIndividualChanges;
                        }
                    }
                    sb.Append(text, pos, text.Length - pos);
                    return sb.ToString();
                }
            }
            numberOfIndividualChanges = 0;
            return text;
        }

        private static string ChangeLineEndingsToLinux(string text, out int numberOfIndividualChanges, out int numberOfAllLineEndings)
        {
            int pos = numberOfAllLineEndings = 0;
            int x;
            while ((x = text.IndexOfAny(lineSepChars, pos)) >= 0)
            {
                ++numberOfAllLineEndings;
                pos = x + 1;
                if (text[x] == '\r')
                {
                    var sb = new StringBuilder(text, 0, x, text.Length);
                    sb.Append('\n');
                    numberOfIndividualChanges = 1;
                    if (pos < text.Length && text[pos] == '\n')
                    {
                        ++pos;
                    }
                    while ((x = text.IndexOfAny(lineSepChars, pos)) >= 0)
                    {
                        ++numberOfAllLineEndings;
                        sb.Append(text, pos, x - pos);
                        sb.Append('\n');
                        pos = x + 1;
                        if (text[x] == '\r')
                        {
                            ++numberOfIndividualChanges;
                            if (pos < text.Length && text[pos] == '\n')
                            {
                                ++pos;
                            }
                        }
                    }
                    sb.Append(text, pos, text.Length - pos);
                    return sb.ToString();
                }
            }
            numberOfIndividualChanges = 0;
            return text;
        }

        private static string ChangeLineEndingsToMacintosh(string text, out int numberOfIndividualChanges, out int numberOfAllLineEndings)
        {
            int pos = numberOfAllLineEndings = 0;
            int x;
            while ((x = text.IndexOfAny(lineSepChars, pos)) >= 0)
            {
                ++numberOfAllLineEndings;
                pos = x + 1;
                if (text[x] == '\n' || (pos < text.Length && text[pos] == '\n'))
                {
                    var sb = new StringBuilder(text, 0, x, text.Length);
                    sb.Append('\r');
                    numberOfIndividualChanges = 1;
                    if (text[x] == '\r')
                    {
                        ++pos;
                    }
                    while ((x = text.IndexOfAny(lineSepChars, pos)) >= 0)
                    {
                        ++numberOfAllLineEndings;
                        sb.Append(text, pos, x - pos);
                        sb.Append('\r');
                        pos = x + 1;
                        if (text[x] == '\n' || (pos < text.Length && text[pos] == '\n'))
                        {
                            ++numberOfIndividualChanges;
                            if (text[x] == '\r')
                            {
                                ++pos;
                            }
                        }
                    }
                    sb.Append(text, pos, text.Length - pos);
                    return sb.ToString();
                }
            }
            numberOfIndividualChanges = 0;
            return text;
        }

        private static int CountOccurances(string text, string lineEnding)
        {
            int count = 0;
            int pos = 0;
            while (true)
            {
                int x = text.IndexOf(lineEnding, pos, StringComparison.Ordinal);
                if (x < 0)
                {
                    return count;
                }
                pos = x + lineEnding.Length;
                ++count;
            }
        }
    }
}
