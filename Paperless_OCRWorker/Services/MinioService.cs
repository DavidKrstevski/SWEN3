using Minio;
using Minio.DataModel.Args;
using Paperless_OCRWorker.Config;

namespace Paperless_OCRWorker.Services;

public class MinioService
{
    private readonly IMinioClient _minio;
    private readonly string _bucket;

    public MinioService(MinioSettings settings)
    {
        _bucket = settings.BucketName;
        _minio = new MinioClient()
            .WithEndpoint(settings.Endpoint)
            .WithCredentials(settings.AccessKey, settings.SecretKey)
            .WithSSL(false)
            .Build();
    }

    public async Task EnsureBucketExistsAsync()
    {
        bool exists = await _minio.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucket));
        if (!exists)
        {
            await _minio.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucket));
            Console.WriteLine($"Created bucket '{_bucket}'");
        }
    }

    public async Task DownloadFileAsync(string objectName, string localPath)
    {
        await _minio.GetObjectAsync(new GetObjectArgs()
            .WithBucket(_bucket)
            .WithObject(objectName)
            .WithFile(localPath));
    }

    public async Task UploadFileAsync(string objectName, string filePath)
    {
        await using var stream = File.OpenRead(filePath);
        await _minio.PutObjectAsync(new PutObjectArgs()
            .WithBucket(_bucket)
            .WithObject(objectName)
            .WithStreamData(stream)
            .WithObjectSize(stream.Length)
            .WithContentType("text/plain"));
    }
}
