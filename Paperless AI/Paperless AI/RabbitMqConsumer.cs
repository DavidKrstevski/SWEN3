using System.Text;
using System.Text.Json;
using GenAIWorker.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace GenAIWorker;

public class RabbitMqConsumer : IDisposable
{
    private readonly ILogger<RabbitMqConsumer> _logger;
    private readonly ConnectionFactory _factory;
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly string _queueName;

    public RabbitMqConsumer(IConfiguration config, ILogger<RabbitMqConsumer> logger)
    {
        _logger = logger;

        var host = config["RABBITMQ_HOST"] ?? "rabbitmq";
        var user = config["RABBITMQ_USER"] ?? "guest";
        var pass = config["RABBITMQ_PASS"] ?? "guest";

        _queueName = config["RABBITMQ_OCR_COMPLETED_QUEUE"] ?? "ocr.completed";

        _factory = new ConnectionFactory
        {
            HostName = host,
            UserName = user,
            Password = pass,
        };
    }

    private async Task EnsureConnectedAsync(CancellationToken ct)
    {
        if (_channel is { IsOpen: true })
            return;

        _connection = await _factory.CreateConnectionAsync(ct);
        _channel = await _connection.CreateChannelAsync();

        await _channel.QueueDeclareAsync(
            queue: _queueName,
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: ct);

        _logger.LogInformation(
            "Connected to RabbitMQ at {Host}, listening on queue {Queue}",
            _factory.HostName,
            _queueName);
    }

    public async Task ConsumeAsync(Func<OcrCompletedMessage, Task> handler, CancellationToken ct)
    {
        await EnsureConnectedAsync(ct);

        var channel = _channel!;
        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);

            _logger.LogInformation("Received message: {Json}", json);

            try
            {
                var msg = JsonSerializer.Deserialize<OcrCompletedMessage>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (msg == null)
                {
                    _logger.LogWarning("Received null or invalid message: {Json}", json);
                    await channel.BasicAckAsync(
                        deliveryTag: ea.DeliveryTag,
                        multiple: false,
                        cancellationToken: ct);
                    return;
                }

                await handler(msg);

                await channel.BasicAckAsync(
                    deliveryTag: ea.DeliveryTag,
                    multiple: false,
                    cancellationToken: ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle message: {Json}", json);

                await channel.BasicNackAsync(
                    deliveryTag: ea.DeliveryTag,
                    multiple: false,
                    requeue: false,
                    cancellationToken: ct);
            }
        };

        await channel.BasicConsumeAsync(
            queue: _queueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: ct);

        _logger.LogInformation("RabbitMqConsumer started. Waiting for messages on {Queue}", _queueName);

        try
        {
            await Task.Delay(Timeout.Infinite, ct);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Cancellation requested, stopping consumer");
        }

        Dispose();
    }

    public void Dispose()
    {
        try
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }
        catch
        {
        }
    }
}
