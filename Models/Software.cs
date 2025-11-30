using System.Xml.Serialization; // Не забудь цей using!

namespace XmlAnalyzer.Models
{
    public class Software
    {
        [XmlAttribute]
        public string? Name { get; set; }

        [XmlAttribute]
        public string? Author { get; set; }

        [XmlAttribute]
        public string? Category { get; set; }

        [XmlAttribute]
        public string? Description { get; set; }
    }
}