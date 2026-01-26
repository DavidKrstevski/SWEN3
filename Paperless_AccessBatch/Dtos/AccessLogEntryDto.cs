namespace Paperless_AccessBatch.Dtos
{
    public class AccessLogEntryDto
    {
        public Guid DocumentId { get; set; }
        public int AccessCount { get; set; }
    }
}
