using EnvDTE;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace JakubBielawa.LineEndingsUnifier
{
    public class ChangesManager
    {
        public Dictionary<string, LastChanges> GetLastChanges(Solution solution)
        {
            var result = new Dictionary<string, LastChanges>();

            var filePath = Path.GetDirectoryName(solution.FullName) + "\\.leu";
            if (!File.Exists(filePath))
            {
                return result;
            }

            using (XmlReader reader = XmlReader.Create(filePath))
            {
                while (reader.Read())
                {
                    if (reader.Name.Equals("file"))
                    {
                        LineEndingsChanger.LineEndings lineEndings;
                        if (Enum.TryParse(reader["lineEndings"], out lineEndings))
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
            if (lastChanges != null && lastChanges.Count > 0)
            {
                var filePath = Path.GetDirectoryName(solution.FullName) + "\\.leu";

                using (XmlWriter writer = XmlWriter.Create(filePath))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("files");

                    foreach (var entry in lastChanges)
                    {
                        if (File.Exists(entry.Key))
                        {
                            writer.WriteStartElement("file");

                            writer.WriteAttributeString("path", entry.Key);
                            writer.WriteAttributeString("dateUnified", entry.Value.Ticks.ToString());
                            writer.WriteAttributeString("lineEndings", entry.Value.LineEndings.ToString());

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
