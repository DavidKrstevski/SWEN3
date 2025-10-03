using RabbitMQ.Client;
using System.Text.Json;
using System.Text;

namespace Paperless_API.Messaging
{
    public class RabbitMqProducer : IRabbitMqProducer
    {
        public string Host { get; }
        public string Queue { get; }

        public RabbitMqProducer(IConfiguration config)
        {
            Host = config["RabbitMq:HostName"];
            Queue = config["RabbitMq:QueueName"];
        }

        public async Task<string> PublishAsync<T>(T item, string hostName, string queueName)
        {
            var factory = new ConnectionFactory { HostName = hostName };

            await using var connection = await factory.CreateConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var json = JsonSerializer.Serialize(item);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = new BasicProperties
            {
                Persistent = true
            };

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: queueName,
                mandatory: false,
                basicProperties: properties,
                body: new ReadOnlyMemory<byte>(body));

            return $"Processed item of type {typeof(T).Name}: {item}";
        }
    }
}
