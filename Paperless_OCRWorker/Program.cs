using Paperless_OCRWorker.Config;
using Paperless_OCRWorker.Messaging;
using Paperless_OCRWorker.Services;

var minioSettings = new MinioSettings();
var rabbitSettings = new RabbitMqSettings();

var minioService = new MinioService(minioSettings);
var ocrService = new OcrService(minioService);
var listener = new RabbitMqListener(rabbitSettings, ocrService);

await listener.StartAsync();
