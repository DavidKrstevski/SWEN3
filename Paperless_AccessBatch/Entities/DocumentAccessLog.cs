namespace Paperless_AccessBatch.Entities
{
    public class DocumentAccessLog
    {
        public Guid Id { get; set; }

        public Guid DocumentId { get; set; }

        public DateTime AccessDate { get; set; }

        public int AccessCount { get; set; }
    }
}
