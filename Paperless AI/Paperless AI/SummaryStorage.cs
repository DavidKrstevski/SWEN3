using System.Text;
using GenAIWorker.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;

namespace GenAIWorker;

public class SummaryStorage
{
    private readonly IMinioClient _minio;
    private readonly ILogger<SummaryStorage> _logger;
    private readonly string _bucket;
    private readonly string _summaryPrefix;

    public SummaryStorage(IConfiguration config, ILogger<SummaryStorage> logger)
    {
        _logger = logger;

        var endpoint = config["MINIO_ENDPOINT"] ?? "minio:9000";
        var accessKey = config["MINIO_ACCESS_KEY"] ?? "minioadmin";
        var secretKey = config["MINIO_SECRET_KEY"] ?? "minioadmin";
        _bucket = config["MINIO_BUCKET"] ?? "documents";

        bool useSsl = endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        var cleanEndpoint = endpoint.Replace("http://", "").Replace("https://", "");

        _minio = new MinioClient()
            .WithEndpoint(cleanEndpoint)
            .WithCredentials(accessKey, secretKey)
            .WithSSL(useSsl)
            .Build();
    }

    public async Task<string> StoreSummaryAsync(Guid documentId, string summary, CancellationToken ct)
    {
        var objectKey = $"{documentId}-summary.txt";

        var bytes = Encoding.UTF8.GetBytes(summary);
        using var ms = new MemoryStream(bytes);

        bool exists = await _minio.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(_bucket), ct);

        if (!exists)
        {
            _logger.LogInformation("Bucket {Bucket} does not exist, creating...", _bucket);
            await _minio.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(_bucket), ct);
        }

        var putArgs = new PutObjectArgs()
            .WithBucket(_bucket)
            .WithObject(objectKey)
            .WithStreamData(ms)
            .WithObjectSize(ms.Length)
            .WithContentType("text/plain; charset=utf-8");

        await _minio.PutObjectAsync(putArgs, ct);

        _logger.LogInformation("Stored summary in MinIO: {Bucket}/{Key}", _bucket, objectKey);

        return objectKey;
    }

    public async Task<string> LoadOcrTextAsync(OcrCompletedMessage msg, CancellationToken ct)
    {
        var objectName = $"{msg.Id}.txt";

        using var ms = new MemoryStream();

        var getArgs = new GetObjectArgs()
            .WithBucket(_bucket)
            .WithObject(objectName)
            .WithCallbackStream(stream => stream.CopyTo(ms));

        await _minio.GetObjectAsync(getArgs, ct);

        ms.Position = 0;
        using var sr = new StreamReader(ms, Encoding.UTF8);
        return await sr.ReadToEndAsync(ct);
    }

}
