using Moq;
using Minio;
using Minio.DataModel.Args;
using OCRWorker_Tests;
using Paperless_OCRWorker.Services;

namespace OCRWorker_Tests
{
    public class Tests
    {
        [TestFixture]
        public class OcrProcessorTests
        {
            private Mock<IMinioClient> _mockMinio = null!;
            private OCRProcessor _processor = null!;
            private string _tempFile = null!;

            [SetUp]
            public void Setup()
            {
                _mockMinio = new Mock<IMinioClient>();
                _processor = new OCRProcessor(_mockMinio.Object, "documents");

                _tempFile = Path.Combine(Path.GetTempPath(), "test.pdf");
                File.WriteAllText(_tempFile, "fake pdf content");
            }

            [TearDown]
            public void Cleanup()
            {
                if (File.Exists(_tempFile))
                    File.Delete(_tempFile);
            }

            [Test]
            public async Task ProcessPdfAsync_DownloadsAndUploadsFile()
            {
                var objectName = "test.pdf";

                _mockMinio.Setup(m => m.GetObjectAsync(It.IsAny<GetObjectArgs>(), It.IsAny<CancellationToken>()))
                     .Callback<GetObjectArgs, CancellationToken>((args, ct) =>
                     {
                         var field = typeof(GetObjectArgs).GetField("callbackStream", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                         var callback = (Action<Stream>?)field?.GetValue(args);

                         if (callback != null)
                         {
                             using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("fake pdf content"));
                             callback(stream);
                         }
                     })
                     .Returns(() => Task.CompletedTask);

                _mockMinio.Setup(m => m.PutObjectAsync(It.IsAny<PutObjectArgs>(), It.IsAny<CancellationToken>()))
                          .Returns(() => Task.CompletedTask);

                var resultPath = await _processor.ProcessPdfAsync(objectName);

                _mockMinio.Verify(m => m.GetObjectAsync(It.IsAny<GetObjectArgs>(), It.IsAny<CancellationToken>()), Times.Once);
                _mockMinio.Verify(m => m.PutObjectAsync(It.IsAny<PutObjectArgs>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
                Assert.That(resultPath, Does.Contain(".txt"));
            }

            [Test]
            public void ProcessPdfAsync_ThrowsOnMissingObjectName()
            {
                Assert.ThrowsAsync<ArgumentException>(async () =>
                {
                    await _processor.ProcessPdfAsync("");
                });
            }
        }
    }
}