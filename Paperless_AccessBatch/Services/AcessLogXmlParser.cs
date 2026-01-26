using System.Xml.Linq;
using Paperless_AccessBatch.Dtos;

namespace Paperless_AccessBatch.Services
{
    public class AccessLogXmlParser
    {
        public AccessLogFileDto Parse(string filePath)
        {
            var doc = XDocument.Load(filePath);

            var root = doc.Root
                ?? throw new InvalidOperationException("XML has no root element");

            var dateAttr = root.Attribute("date")?.Value
                ?? throw new InvalidOperationException("Missing 'date' attribute");

            var date = DateTime.Parse(dateAttr);

            var entries = root.Elements("Document")
                .Select(e => new AccessLogEntryDto
                {
                    DocumentId = Guid.Parse(e.Element("DocumentId")!.Value),
                    AccessCount = int.Parse(e.Element("AccessCount")!.Value)
                })
                .ToList();

            return new AccessLogFileDto
            {
                Date = date,
                Entries = entries
            };
        }
    }
}
