using System.Xml.Linq;
using XmlAnalyzer.Models;

namespace XmlAnalyzer.Services
{
    public class LinqSearchStrategy : ISearchStrategy
    {
        public List<Software> Search(string filePath, string? keyword, string? category)
        {
            var doc = XDocument.Load(filePath);
            var results = new List<Software>();

            // Знаходимо всі елементи <Software>
            var query = doc.Descendants("Software");

            foreach (var elem in query)
            {
                // Отримуємо батьківську категорію
                string cat = elem.Parent?.Attribute("Name")?.Value ?? "Unknown";
                string name = elem.Attribute("Name")?.Value ?? "";
                string author = elem.Attribute("Author")?.Value ?? "";
                
                // Логіка "Різних атрибутів" (Платне чи безкоштовне?)
                string desc = "";
                if (elem.Attribute("LicenseKey") != null)
                    desc = $"License: {elem.Attribute("LicenseKey")?.Value} | Cost: {elem.Attribute("Cost")?.Value}";
                else if (elem.Attribute("OpenSource")?.Value == "True")
                    desc = $"Open Source | Repo: {elem.Attribute("RepoUrl")?.Value}";

                // Фільтрація
                bool nameMatch = string.IsNullOrEmpty(keyword) || name.Contains(keyword, StringComparison.OrdinalIgnoreCase);
                bool catMatch = string.IsNullOrEmpty(category) || cat == category;

                if (nameMatch && catMatch)
                {
                    results.Add(new Software { Name = name, Author = author, Category = cat, Description = desc });
                }
            }

            return results;
        }
    }
}