using System.Diagnostics;
using Minio;
using Minio.DataModel.Args;

namespace Paperless_OCRWorker.Services
{
    public class OCRProcessor
    {
        private readonly IMinioClient _minio;
        private readonly string _bucket;

        public OCRProcessor(IMinioClient minioClient, string bucketName = "documents")
        {
            _minio = minioClient;
            _bucket = bucketName;
        }

        public async Task<string> ProcessPdfAsync(string objectName)
        {
            var localPdfPath = Path.Combine(Path.GetTempPath(), objectName);
            var ocrTextPath = Path.Combine(Path.GetTempPath(), $"{Path.GetFileNameWithoutExtension(objectName)}.txt");
            using (var fileStream = File.Create(localPdfPath))
            {
                await _minio.GetObjectAsync(new GetObjectArgs()
                    .WithBucket(_bucket)
                    .WithObject(objectName)
                    .WithCallbackStream(stream => stream.CopyTo(fileStream)));
            }

            var tesseract = new ProcessStartInfo
            {
                FileName = "tesseract",
                Arguments = $"{localPdfPath} {ocrTextPath.Replace(".txt", "")} -l eng",
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            var process = Process.Start(tesseract);
            await process!.WaitForExitAsync();

            if (File.Exists(ocrTextPath))
            {
                using var resultStream = File.OpenRead(ocrTextPath);
                await _minio.PutObjectAsync(new PutObjectArgs()
                    .WithBucket(_bucket)
                    .WithObject($"{Path.GetFileNameWithoutExtension(objectName)}.txt")
                    .WithStreamData(resultStream)
                    .WithObjectSize(resultStream.Length)
                    .WithContentType("text/plain"));
            }

            return ocrTextPath;
        }
    }
}
