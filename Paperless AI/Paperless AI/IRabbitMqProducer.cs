using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paperless_AI
{
    public interface IRabbitMqProducer
    {
        string Host { get; }
        string Queue { get; }

        Task<string> PublishAsync<T>(T item, string hostName, string queueName);
    }
}
