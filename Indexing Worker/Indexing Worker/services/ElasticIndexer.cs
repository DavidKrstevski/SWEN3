using Elastic.Clients.Elasticsearch;
using Indexing_Worker.messaging;
using Microsoft.Extensions.Configuration;

namespace Indexing_Worker.services
{
    public sealed class ElasticIndexer : IElasticIndexer
    {
        private readonly ElasticsearchClient _client;
        private readonly string _indexName;

        public ElasticIndexer(ElasticsearchClient client, IConfiguration cfg)
        {
            _client = client;
            _indexName = cfg["Elasticsearch:IndexName"] ?? "documents";
        }

        public async Task IndexAsync(DocumentMessage msg, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(msg.Id))
                throw new ArgumentException("DocumentMessage.Id is required for indexing");

            IndexName index = _indexName;
            Id id = msg.Id;

            var response = await _client.IndexAsync(msg, index, id, ct);

            if (!response.IsValidResponse)
                throw new Exception($"Elasticsearch index failed: {response.ElasticsearchServerError}");
        }

    }
}
