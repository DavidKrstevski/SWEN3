using Microsoft.Extensions.Options;
using Paperless_OCRWorker.Config;
using Paperless_OCRWorker.Messaging;
using Paperless_OCRWorker.Services;

var minioSettings = new MinioSettings();
var rabbitSettings = new RabbitMqSettings
{
    HostName = "rabbitmq",
    QueueName = "ocr_jobs",
    CompletedQueueName = "ocr_completed"
};

var minioService = new MinioService(minioSettings);

var rabbitOptions = Options.Create(rabbitSettings);
var producer = new RabbitMqProducer(rabbitOptions);

var ocrService = new OcrService(minioService, producer);

var listener = new RabbitMqListener(rabbitSettings, ocrService);

await listener.StartAsync();
