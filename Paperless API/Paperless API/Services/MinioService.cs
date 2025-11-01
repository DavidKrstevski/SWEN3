using Minio;
using Minio.DataModel.Args;

namespace Paperless_API.Services
{
    public class MinioService
    {
        private readonly IMinioClient _client;
        private readonly string _bucket;

        public MinioService(IConfiguration config)
        {
            var endpoint = config["Minio:Endpoint"] ?? "minio:9000";
            var accessKey = config["Minio:AccessKey"] ?? "minio";
            var secretKey = config["Minio:SecretKey"] ?? "minio123";
            _bucket = config["Minio:Bucket"] ?? "paperless";
            var useSSL = bool.TryParse(config["Minio:UseSSL"], out var ssl) && ssl;

            endpoint = endpoint.Replace("http://", "").Replace("https://", "");

            var client = new MinioClient()
                .WithEndpoint(endpoint)
                .WithCredentials(accessKey, secretKey);

            if (useSSL)
                client = client.WithSSL();

            _client = client.Build();
        }


        public async Task EnsureBucketExistsAsync()
        {
            bool found = await _client.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(_bucket));

            if (!found)
                await _client.MakeBucketAsync(
                    new MakeBucketArgs().WithBucket(_bucket));
        }

        public async Task UploadAsync(string objectName, Stream fileStream)
        {
            await EnsureBucketExistsAsync();

            await _client.PutObjectAsync(
                new PutObjectArgs()
                    .WithBucket(_bucket)
                    .WithObject(objectName)
                    .WithStreamData(fileStream)
                    .WithObjectSize(fileStream.Length)
                    .WithContentType("application/pdf"));
        }
    }
}
