namespace Paperless_API.Messaging
{
    public interface IRabbitMqProducer
    {
        string Host { get; }
        string Queue { get; }

        Task<string> PublishAsync<T>(T item, string hostName, string queueName);
    }
}
