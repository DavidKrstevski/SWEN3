namespace Paperless_OCRWorker.Messaging;

public record DocumentMessage
{
    public string? Id { get; set; }
    public string? FileName { get; set; }
    public long Size { get; set; }
    public DateTimeOffset UploadDate { get; set; }
}
