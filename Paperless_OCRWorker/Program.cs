using Microsoft.Extensions.Options;
using Paperless_OCRWorker.Config;
using Paperless_OCRWorker.Messaging;
using Paperless_OCRWorker.Services;

// Settings (hier kannst du später HostName, CompletedQueueName etc. aus Config/Env setzen)
var minioSettings = new MinioSettings();
var rabbitSettings = new RabbitMqSettings
{
    HostName = "rabbitmq",          // wie im Docker-Compose
    QueueName = "ocr_jobs",         // Eingangsqueue, die dein Listener hört
    CompletedQueueName = "ocr_completed" // Queue, auf die der GenAI-Worker hört
};

// Services
var minioService = new MinioService(minioSettings);

// IOptions<RabbitMqSettings> aus deinem rabbitSettings bauen
var rabbitOptions = Options.Create(rabbitSettings);
var producer = new RabbitMqProducer(rabbitOptions);

// OcrService bekommt jetzt auch den Producer
var ocrService = new OcrService(minioService, producer);

// Listener bleibt wie gehabt
var listener = new RabbitMqListener(rabbitSettings, ocrService);

await listener.StartAsync();
