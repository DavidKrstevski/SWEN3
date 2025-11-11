namespace Paperless_OCRWorker.Config;

public class RabbitMqSettings
{
    public string HostName { get; set; } = "rabbitmq";
    public string QueueName { get; set; } = "ocr_jobs";
}
