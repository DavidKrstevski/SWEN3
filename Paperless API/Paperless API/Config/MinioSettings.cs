namespace Paperless_API.Config
{
    public class MinioSettings
    {
        public string Endpoint { get; set; } = default!;
        public string AccessKey { get; set; } = default!;
        public string SecretKey { get; set; } = default!;
        public string Bucket { get; set; } = default!;
    }
}
