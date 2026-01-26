namespace Paperless_API.Entities
{
    public class DocumentAccessLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid DocumentId { get; set; }
        public Document Document { get; set; } = null!;
        public DateTime AccessDate { get; set; }
        public int AccessCount { get; set; }
    }
}
