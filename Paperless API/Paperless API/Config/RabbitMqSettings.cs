namespace Paperless_API.Config
{
    public class RabbitMqSettings
    {
        public string Host { get; set; } = default!;
        public string Queue { get; set; } = default!;
    }
}
