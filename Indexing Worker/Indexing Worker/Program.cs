using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Indexing_Worker;
using Indexing_Worker.services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var url = cfg["Elasticsearch:Url"] ?? "http://localhost:9200";
    var settings = new ElasticsearchClientSettings(new Uri(url));
    return new ElasticsearchClient(settings);
});

builder.Services.AddSingleton<IElasticIndexer, ElasticIndexer>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
