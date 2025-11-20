using XmlAnalyzer.Models;

namespace XmlAnalyzer.Services
{
    public interface ISearchStrategy
    {
        List<Software> Search(string filePath, string? keyword, string? category);
    }
}