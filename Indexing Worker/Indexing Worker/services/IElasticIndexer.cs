using Indexing_Worker.messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Indexing_Worker.services
{
    public interface IElasticIndexer
    {
        Task IndexAsync(DocumentMessage msg, CancellationToken ct);
    }
}
