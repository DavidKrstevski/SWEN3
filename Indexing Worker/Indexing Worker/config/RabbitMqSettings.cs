using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Indexing_Worker.config
{
    internal class RabbitMqSettings
    {
        public string HostName { get; set; } = "rabbitmq";
        public string QueueName { get; set; } = "ocr_completed_index";    }
}
