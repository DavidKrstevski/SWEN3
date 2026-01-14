using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paperless_AI
{
    public class AICompletedMessage
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = null!;
        public long Size { get; set; }
        public DateTimeOffset UploadDate { get; set; } = DateTimeOffset.UtcNow;
        public string? Summary { get; set; }
        public string? ChatHistory { get; set; }
        public string? RiskColor { get; set; }
    }
}
