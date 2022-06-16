namespace LineEndingsUnifier
{
    using EnvDTE;
    using Microsoft.VisualStudio.Shell;

    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;

    public class ChangesManager
    {
        public Dictionary<string, LastChanges> GetLastChanges(Solution solution)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var result = new Dictionary<string, LastChanges>();

            var filePath = $"{Path.GetDirectoryName(solution.FullName)}.{OptionsPage.ChangeLogFileExtension}";
            if (!File.Exists(filePath))
            {
                return result;
            }

            using (var reader = XmlReader.Create(filePath))
            {
                while (reader.Read())
                {
                    if (reader.Name == "file")
                    {
                        if (Enum.TryParse(reader["lineEndings"], out LineEndingsChanger.LineEnding lineEndings))
                        {
                            result[reader["path"]] = new LastChanges(long.Parse(reader["dateUnified"]), lineEndings);
                        }
                    }
                }
            }

            return result;
        }

        public void SaveLastChanges(Solution solution, Dictionary<string, LastChanges> lastChanges)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (lastChanges != null && lastChanges.Keys.Count > 0)
            {
                var filePath = $"{Path.GetDirectoryName(solution.FullName)}.{OptionsPage.ChangeLogFileExtension}";

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
                            writer.WriteAttributeString("dateUnified", lastChanges[key].Ticks.ToString());
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
