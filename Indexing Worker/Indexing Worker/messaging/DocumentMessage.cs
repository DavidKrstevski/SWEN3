using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Indexing_Worker.messaging
{
    public class DocumentMessage
    {
        public string? Id { get; set; }
        public string? FileName { get; set; }
        public string? Text { get; set; }
        public long Size { get; set; }
        public DateTimeOffset UploadDate { get; set; }
    }
}
