namespace Paperless_API.Entities
{
    public class Document
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = default!;
        public long Size { get; set; }
        public DateTimeOffset UploadDate { get; set; } = DateTimeOffset.UtcNow;
    }
}
