namespace LineEndingsUnifier
{
    using EnvDTE;
    using Microsoft.VisualStudio.Shell;

    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Xml;

    internal static class ChangesManager
    {
        public static string GetChangeLogPath(string solutionFullName) =>
            $"{Path.GetDirectoryName(solutionFullName)}.{OptionsPage.ChangeLogFileExtension}";

        public static Dictionary<string, LastChanges> GetLastChanges(Solution solution)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var result = new Dictionary<string, LastChanges>();

            var filePath = GetChangeLogPath(solution.FullName);
            if (!File.Exists(filePath))
            {
                return result;
            }

            try
            {
                using (var reader = XmlReader.Create(filePath))
                {
                    while (reader.Read())
                    {
                        if (reader.Name == "file")
                        {
                            if (Enum.TryParse(reader["lineEndings"], out LineEndingsChanger.LineEnding lineEndings)
                                && long.TryParse(reader["dateUnified"], NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks)
                                && reader["path"] is string path)
                            {
                                result[path] = new LastChanges(ticks, lineEndings);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // A corrupt or unreadable change log must not abort unifying; treating it as
                // empty just means some files get re-unified, which is safe. The log is
                // rewritten by SaveLastChanges once the operation completes.
                return new Dictionary<string, LastChanges>();
            }

            return result;
        }

        public static void SaveLastChanges(Solution solution, Dictionary<string, LastChanges> lastChanges)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (lastChanges != null && lastChanges.Keys.Count > 0)
            {
                var filePath = GetChangeLogPath(solution.FullName);

                using (var writer = XmlWriter.Create(filePath))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("files");

                    foreach (var key in lastChanges.Keys)
                    {
                        if (File.Exists(key))
                        {
                            writer.WriteStartElement("file");

                            writer.WriteAttributeString("path", key);
                            writer.WriteAttributeString("dateUnified", lastChanges[key].Ticks.ToString(CultureInfo.InvariantCulture));
                            writer.WriteAttributeString("lineEndings", lastChanges[key].LineEnding.ToString());

                            writer.WriteEndElement();
                        }
                    }

                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                }
            }
        }
    }
}
