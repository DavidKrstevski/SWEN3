namespace Paperless_AccessBatch.Dtos
{
    public class AccessLogFileDto
    {
        public DateTime Date { get; set; }
        public List<AccessLogEntryDto> Entries { get; set; } = new();
    }
}
