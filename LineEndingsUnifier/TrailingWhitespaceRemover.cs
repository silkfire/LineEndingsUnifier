namespace LineEndingsUnifier
{
    using System;
    using System.Text;

    public static class TrailingWhitespaceRemover
    {
        public static string RemoveTrailingWhitespace(string text)
        {
            var stringBuilder = new StringBuilder();

            var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            for (var i = 0; i < lines.Length - 1; i++)
            {
                stringBuilder.AppendLine(lines[i].TrimEnd());
            }
            stringBuilder.Append(lines[lines.Length - 1].TrimEnd());

            return stringBuilder.ToString();
        }
    }
}
