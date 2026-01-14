using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Indexing_Worker.messaging;
using Indexing_Worker.services;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;

namespace Indexing_Worker_Test;

[TestFixture]
public class ElasticIndexerTests
{
    private static IConfiguration Cfg(string? indexName)
    {
        var dict = new Dictionary<string, string?>();
        if (indexName != null)
            dict["Elasticsearch:IndexName"] = indexName;

        return new ConfigurationBuilder()
            .AddInMemoryCollection(dict!)
            .Build();
    }

    [Test]
    public void IndexAsync_Throws_When_IdMissing()
    {
        var client = new Mock<ElasticsearchClient>();
        var sut = new ElasticIndexer(client.Object, Cfg("documents"));

        var msg = new DocumentMessage { Id = "   " };

        Assert.ThrowsAsync<ArgumentException>(() => sut.IndexAsync(msg, CancellationToken.None));
    }

    [Test]
    public async Task IndexAsync_Uses_DefaultIndex_And_Passes_Id()
    {
        var ok = TestableResponseFactory.CreateSuccessfulResponse(new IndexResponse(), 201);

        IndexName capturedIndex = default;
        Id? capturedId = null;

        var client = new Mock<ElasticsearchClient>();
        client.Setup(c => c.IndexAsync(
                It.IsAny<DocumentMessage>(),
                It.IsAny<IndexName>(),
                It.IsAny<Id?>(),
                It.IsAny<CancellationToken>()))
            .Callback<DocumentMessage, IndexName, Id?, CancellationToken>((_, idx, id, __) =>
            {
                capturedIndex = idx;
                capturedId = id;
            })
            .ReturnsAsync(ok);

        var sut = new ElasticIndexer(client.Object, Cfg(indexName: null)); // default: "documents"

        await sut.IndexAsync(new DocumentMessage { Id = "42" }, CancellationToken.None);

        Assert.That(capturedIndex.ToString(), Is.EqualTo("documents"));
        Assert.That(capturedId?.ToString(), Is.EqualTo("42"));
    }

    [Test]
    public async Task IndexAsync_Uses_Configured_IndexName()
    {
        var ok = TestableResponseFactory.CreateSuccessfulResponse(new IndexResponse(), 201);

        IndexName capturedIndex = default;

        var client = new Mock<ElasticsearchClient>();
        client.Setup(c => c.IndexAsync(
                It.IsAny<DocumentMessage>(),
                It.IsAny<IndexName>(),
                It.IsAny<Id?>(),
                It.IsAny<CancellationToken>()))
            .Callback<DocumentMessage, IndexName, Id?, CancellationToken>((_, idx, __, ___) =>
            {
                capturedIndex = idx;
            })
            .ReturnsAsync(ok);

        var sut = new ElasticIndexer(client.Object, Cfg("my_index"));

        await sut.IndexAsync(new DocumentMessage { Id = "abc" }, CancellationToken.None);

        Assert.That(capturedIndex.ToString(), Is.EqualTo("my_index"));
    }

    [Test]
    public void IndexAsync_Throws_When_Response_Invalid()
    {
        var bad = TestableResponseFactory.CreateResponse(new IndexResponse(), 500, false);

        var client = new Mock<ElasticsearchClient>();
        client.Setup(c => c.IndexAsync(
                It.IsAny<DocumentMessage>(),
                It.IsAny<IndexName>(),
                It.IsAny<Id?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(bad);

        var sut = new ElasticIndexer(client.Object, Cfg("documents"));

        var ex = Assert.ThrowsAsync<Exception>(() =>
            sut.IndexAsync(new DocumentMessage { Id = "1" }, CancellationToken.None));

        StringAssert.Contains("Elasticsearch index failed", ex!.Message);
    }
}
