using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Paperless_AI;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace GenAIWorker;

public class RabbitMqConsumer : IDisposable
{
    private readonly ILogger<RabbitMqConsumer> _logger;
    private readonly ConnectionFactory _factory;
    private IConnection? _connection;
    private readonly List<IChannel> _channels = new();

    public RabbitMqConsumer(IConfiguration config, ILogger<RabbitMqConsumer> logger)
    {
        _logger = logger;

        var host = config["RABBITMQ_HOST"] ?? "rabbitmq";
        var user = config["RABBITMQ_USER"] ?? "guest";
        var pass = config["RABBITMQ_PASS"] ?? "guest";

        _factory = new ConnectionFactory
        {
            HostName = host,
            UserName = user,
            Password = pass        };
    }

    private async Task EnsureConnectedAsync(CancellationToken ct)
    {
        if (_connection is { IsOpen: true })
            return;

        _connection = await _factory.CreateConnectionAsync(ct);
        _logger.LogInformation("Connected to RabbitMQ at {Host}", _factory.HostName);
    }

    public async Task ConsumeManyAsync(
        IEnumerable<string> queueNames,
        Func<string, AICompletedMessage, Task> handler,
        CancellationToken ct)
    {
        await EnsureConnectedAsync(ct);

        foreach (var queue in queueNames.Distinct())
        {
            var channel = await _connection!.CreateChannelAsync(cancellationToken: ct);
            _channels.Add(channel);

            await channel.QueueDeclareAsync(
                queue: queue,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: ct);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                _logger.LogInformation("Received message from {Queue}: {Json}", queue, json);

                try
                {
                    var msg = JsonSerializer.Deserialize<AICompletedMessage>(
                        json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (msg == null)
                    {
                        _logger.LogWarning("Invalid message on {Queue}: {Json}", queue, json);
                        await channel.BasicAckAsync(ea.DeliveryTag, false, ct);
                        return;
                    }

                    await handler(queue, msg);
                    await channel.BasicAckAsync(ea.DeliveryTag, false, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to handle message on {Queue}: {Json}", queue, json);

                    await channel.BasicNackAsync(
                        deliveryTag: ea.DeliveryTag,
                        multiple: false,
                        requeue: false,
                        cancellationToken: ct);
                }
            };

            await channel.BasicConsumeAsync(
                queue: queue,
                autoAck: false,
                consumer: consumer,
                cancellationToken: ct);

            _logger.LogInformation("Started consuming {Queue}", queue);
        }

        // Läuft bis Stop
        await Task.Delay(Timeout.Infinite, ct);
    }

    public void Dispose()
    {
        try
        {
            foreach (var ch in _channels)
                ch.Dispose();

            _connection?.Dispose();
        }
        catch { }
    }
}
