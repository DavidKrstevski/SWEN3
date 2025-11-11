using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Paperless_OCRWorker.Services;
using Paperless_OCRWorker.Messaging;
using Paperless_OCRWorker.Config;

namespace Paperless_OCRWorker.Messaging;

public class RabbitMqListener
{
    private readonly RabbitMqSettings _settings;
    private readonly OcrService _ocrService;

    public RabbitMqListener(RabbitMqSettings settings, OcrService ocrService)
    {
        _settings = settings;
        _ocrService = ocrService;
    }

    public async Task StartAsync()
    {
        var factory = new ConnectionFactory { HostName = _settings.HostName };
        IConnection? connection = null;
        IChannel? channel = null;

        for (int i = 1; i <= 10; i++)
        {
            try
            {
                Console.WriteLine($"Attempt {i}: Connecting to RabbitMQ...");
                connection = await factory.CreateConnectionAsync("OcrWorker");
                channel = await connection.CreateChannelAsync();
                Console.WriteLine("Connected to RabbitMQ");
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection attempt {i} failed: {ex.Message}");
                await Task.Delay(5000);
            }
        }

        if (connection == null || channel == null)
        {
            Console.WriteLine("Failed to connect to RabbitMQ. Exiting.");
            return;
        }

        await channel.QueueDeclareAsync(_settings.QueueName, durable: false, exclusive: false, autoDelete: false);
        Console.WriteLine("Listening for OCR messages...");

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var message = Encoding.UTF8.GetString(ea.Body.ToArray());
            Console.WriteLine($"Received message: {message}");
            var doc = JsonSerializer.Deserialize<DocumentMessage>(message);
            if (doc != null)
                await _ocrService.ProcessDocumentAsync(doc);
        };

        await channel.BasicConsumeAsync(queue: _settings.QueueName, autoAck: true, consumer: consumer);
        await Task.Delay(Timeout.Infinite);
    }
}
