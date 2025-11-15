namespace GenAIWorker.Models;

public sealed class OcrCompletedMessage
{
    public string? Id { get; set; }
    public string? FileName { get; set; }
    public long Size { get; set; }
    public DateTimeOffset UploadDate { get; set; }
}
