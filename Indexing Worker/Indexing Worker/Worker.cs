using Indexing_Worker.messaging;
using Indexing_Worker.services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

public sealed class Worker : BackgroundService
{
    private readonly IConfiguration _cfg;
    private readonly IElasticIndexer _indexer;
    private readonly ILogger<Worker> _log;
    private IConnection? _conn;
    private IChannel? _channel;

    public Worker(IConfiguration cfg, IElasticIndexer indexer, ILogger<Worker> log)
    {
        _cfg = cfg;
        _indexer = indexer;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var host = _cfg["RabbitMQ:Host"] ?? "localhost";
            var user = _cfg["RabbitMQ:User"] ?? "guest";
            var pass = _cfg["RabbitMQ:Pass"] ?? "guest";
            var queue = _cfg["RabbitMQ:Queue"] ?? "ocr_completed_index";

            _log.LogInformation("Connecting to RabbitMQ host={Host} queue={Queue} user={User}", host, queue, user);

            var factory = new ConnectionFactory
            {
                HostName = host,
                UserName = user,
                Password = pass
            };

            _conn = await factory.CreateConnectionAsync(stoppingToken);
            _channel = await _conn.CreateChannelAsync(cancellationToken: stoppingToken);

            _log.LogInformation("Connected. Declaring queue {Queue}", queue);

            await _channel.QueueDeclareAsync(
                queue: queue,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: stoppingToken);

            await _channel.BasicQosAsync(0, 5, false, stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                    _log.LogInformation("Received message (bytes={Len})", ea.Body.Length);

                    var msg = JsonSerializer.Deserialize<DocumentMessage>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                              ?? throw new Exception("Message was null");

                    await _indexer.IndexAsync(msg, stoppingToken);
                    await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Error processing message - NACK requeue");
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, true, stoppingToken);
                }
            };

            await _channel.BasicConsumeAsync(queue, autoAck: false, consumer, stoppingToken);
            _log.LogInformation("Consuming queue {Queue}", queue);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (Exception ex)
        {
            _log.LogCritical(ex, "Worker crashed in ExecuteAsync");
            throw;
        }
    }
}

