using System.Xml;
using XmlAnalyzer.Models;

namespace XmlAnalyzer.Services
{
    public class SaxSearchStrategy : ISearchStrategy
    {
        public List<Software> Search(string filePath, string? keyword, string? category)
        {
            var results = new List<Software>();
            var settings = new XmlReaderSettings { IgnoreWhitespace = true };

            using (var reader = XmlReader.Create(filePath, settings))
            {
                string currentCategory = "";

                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (reader.Name == "Category")
                        {
                            currentCategory = reader.GetAttribute("Name") ?? "";
                        }
                        else if (reader.Name == "Software")
                        {
                            // Якщо категорія обрана, але не співпадає - пропускаємо
                            if (!string.IsNullOrEmpty(category) && currentCategory != category) continue;

                            string name = reader.GetAttribute("Name") ?? "";
                            string author = reader.GetAttribute("Author") ?? "";

                            if (!string.IsNullOrEmpty(keyword) && !name.Contains(keyword, StringComparison.OrdinalIgnoreCase)) continue;

                            // Різні атрибути
                            string desc = "";
                            if (reader.GetAttribute("LicenseKey") != null)
                                desc = $"License: {reader.GetAttribute("LicenseKey")}";
                            else if (reader.GetAttribute("RepoUrl") != null)
                                desc = $"Repo: {reader.GetAttribute("RepoUrl")}";

                            results.Add(new Software { Name = name, Author = author, Category = currentCategory, Description = desc });
                        }
                    }
                }
            }
            return results;
        }
    }
}