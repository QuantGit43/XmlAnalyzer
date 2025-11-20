using System.Xml;
using XmlAnalyzer.Models;

namespace XmlAnalyzer.Services
{
    public class DomSearchStrategy : ISearchStrategy
    {
        public List<Software> Search(string filePath, string? keyword, string? category)
        {
            var results = new List<Software>();
            var doc = new XmlDocument();
            doc.Load(filePath);

            // Отримуємо всі категорії
            XmlNodeList? categories = doc.SelectNodes("//Category");
            if (categories == null) return results;

            foreach (XmlNode catNode in categories)
            {
                string catName = catNode.Attributes?["Name"]?.Value ?? "";
                
                // Перевірка фільтру категорії
                if (!string.IsNullOrEmpty(category) && catName != category) continue;

                foreach (XmlNode softNode in catNode.ChildNodes)
                {
                    if (softNode.Name != "Software") continue;

                    string name = softNode.Attributes?["Name"]?.Value ?? "";
                    string author = softNode.Attributes?["Author"]?.Value ?? "";

                    // Перевірка фільтру назви
                    if (!string.IsNullOrEmpty(keyword) && !name.Contains(keyword, StringComparison.OrdinalIgnoreCase)) continue;

                    // Обробка різних атрибутів
                    string desc = "";
                    if (softNode.Attributes?["LicenseKey"] != null)
                        desc = $"License: {softNode.Attributes["LicenseKey"]?.Value}";
                    else if (softNode.Attributes?["RepoUrl"] != null)
                        desc = $"Repo: {softNode.Attributes["RepoUrl"]?.Value}";

                    results.Add(new Software { Name = name, Author = author, Category = catName, Description = desc });
                }
            }
            return results;
        }
    }
}