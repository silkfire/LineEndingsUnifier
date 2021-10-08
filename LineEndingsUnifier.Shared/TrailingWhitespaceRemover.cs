using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JakubBielawa.LineEndingsUnifier
{
    public static class TrailingWhitespaceRemover
    {
        private static readonly char[] lineSepChars = { '\r', '\n' };

        public static string RemoveTrailingWhitespace(string text)
        {
            int pos = 0;
            while (pos < text.Length)
            {
                char c;
                int x = text.IndexOfAny(lineSepChars, pos);
                if (x < 0)
                {
                    // File did not have any line terminators preceded by whitespace,
                    // but there might still be other whitespace preceding EOF.
                    x = text.Length;
                    while (x > pos && char.IsWhiteSpace(text[x - 1]))
                    {
                        --x;
                    }
                    return x == text.Length ? text : text.Substring(0, x);
                }
                else if (x > pos && char.IsWhiteSpace(text[x - 1]))
                {
                    var sb = new StringBuilder();
                    int start = 0;
                    while (true)
                    {
                        int end = x < 0 ? text.Length : x;
                        while (end > pos && char.IsWhiteSpace(text[end - 1]))
                        {
                            --end;
                        }
                        sb.Append(text, start, end - start);
                        if (x < 0)
                        {
                            break;
                        }
                        sb.Append(text[x]);
                        pos = x + 1;
                        while (pos < text.Length && ((c = text[pos]) == '\n' || c == '\r'))
                        {
                            sb.Append(c);
                            ++pos;
                        }
                        if (pos >= text.Length)
                        {
                            break;
                        }
                        x = text.IndexOfAny(lineSepChars, start = pos);
                    }
                    return sb.ToString();
                }
                pos = x + 1;
                while (pos < text.Length && ((c = text[pos]) == '\n' || c == '\r'))
                {
                    ++pos;
                }
            }
            return text;
        }
    }
}
