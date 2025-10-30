using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Paperless_API.Config;

namespace Paperless_API.Messaging
{
    public class RabbitMqProducer : IRabbitMqProducer
    {
        private readonly RabbitMqSettings _settings;

        public RabbitMqProducer(IOptions<RabbitMqSettings> options)
        {
            _settings = options.Value;
        }

        public string Host => _settings.Host;
        public string Queue => _settings.Queue;

        public async Task<string> PublishAsync<T>(T item, string? hostName = null, string? queueName = null)
        {
            hostName ??= _settings.Host;
            queueName ??= _settings.Queue;

            var factory = new ConnectionFactory { HostName = hostName };

            await using var connection = await factory.CreateConnectionAsync("PaperlessPublisher");
            await using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            var json = JsonSerializer.Serialize(item);
            var body = new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(json));

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: queueName,
                mandatory: false,
                body: body,
                cancellationToken: CancellationToken.None
            );

            Console.WriteLine($"[x] Sent message to queue '{queueName}': {json}");
            return json;
        }
    }
}
