using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paperless_AI
{
    internal class RabbitMqSettings
    {
        public string HostName { get; set; } = "rabbitmq";
        public string CompletedQueueName { get; set; } = "ocr_completed_legal";
    }
}
