using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Minio.DataModel.Args;
using Minio;
using Moq;
using Paperless_API.Controllers;
using Paperless_API.Data.Repositories;
using Paperless_API.Exceptions;
using Paperless_API.Messaging;
using Paperless_API.Services;
using System.Text;
using Document = Paperless_API.Entities.Document;
using Microsoft.EntityFrameworkCore.Metadata;
using RabbitMQ.Client;
using Microsoft.Extensions.Options;
using Paperless_API.Config;

namespace Paperless_Tests
{
    public class DocumentsControllerTests
    {
        private Mock<IDocumentRepository> _repo;
        private Mock<IRabbitMqProducer> _producer;
        private Mock<ILogger<DocumentsController>> _logger;
        private DocumentsController _docController;
        private readonly CancellationToken _ct = CancellationToken.None;

        [SetUp]
        public void SetUp()
        {
            _repo = new Mock<IDocumentRepository>();
            _producer = new Mock<IRabbitMqProducer>();
            _logger = new Mock<ILogger<DocumentsController>>();
            _docController = new DocumentsController(_repo.Object, _producer.Object, _logger.Object);
        }

        [Test]
        public async Task CreateReturns201()
        {
            var repo = new Mock<IDocumentRepository>();
            var producer = new Mock<IRabbitMqProducer>();
            var minio = new Mock<MinioService>(MockBehavior.Loose, new ConfigurationBuilder().Build());
            var logger = new Mock<ILogger<DocumentsController>>();
            var controller = new DocumentsController(repo.Object, producer.Object, logger.Object);

            var content = new MemoryStream(Encoding.UTF8.GetBytes("dummy pdf content"));
            var file = new FormFile(content, 0, content.Length, "file", "demo.pdf");

            repo.Setup(r => r.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Document doc, CancellationToken ct) => doc);

            var result = await controller.Create(file, minio.Object, CancellationToken.None);

            var created = result as CreatedAtActionResult;
            Assert.IsNotNull(created);
            var body = created.Value as Document;
            Assert.IsNotNull(body);
            Assert.AreEqual("demo.pdf", body.FileName);
        }

        [Test]
        public async Task CreateReturns400EmptyFilename()
        {
            var emptyFile = new FormFile(Stream.Null, 0, 0, "file", "empty.pdf");
            var minio = new Mock<MinioService>(MockBehavior.Loose, new ConfigurationBuilder().Build()).Object;
            var result = await _docController.Create(emptyFile, minio, _ct);
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        [Test]
        public async Task CreateReturns400NoFilename()
        {
            var result = await _docController.Create(null, new Mock<MinioService>(MockBehavior.Loose, new ConfigurationBuilder().Build()).Object, _ct);
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        [Test]
        public async Task GetByIdReturns200()
        {
            var id = Guid.NewGuid();
            var doc = new Document { Id = id, FileName = "demo.pdf", Size = 123, UploadDate = DateTimeOffset.UtcNow };
            _repo.Setup(r => r.GetAsync(id, _ct)).ReturnsAsync(doc);

            var result = await _docController.GetById(id, _ct);

            var ok = result as OkObjectResult;
            Assert.IsNotNull(ok);
            var body = ok.Value as Document;
            Assert.IsNotNull(body);
            Assert.AreEqual(id, body.Id);
        }

        [Test]
        public async Task GetByIdReturns404NoFile()
        {
            var id = Guid.NewGuid();
            _repo.Setup(r => r.GetAsync(id, _ct)).ThrowsAsync(new DocumentNotFoundException(id));

            var result = await _docController.GetById(id, _ct);

            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task DeleteReturns204()
        {
            var id = Guid.NewGuid();
            _repo.Setup(r => r.DeleteAsync(id, _ct)).Returns(Task.CompletedTask);

            var result = await _docController.Delete(id, _ct);

            Assert.IsInstanceOf<NoContentResult>(result);
        }

        [Test]
        public async Task UploadFileCallsMinioAndPublishesMessage()
        {
            var repo = new Mock<IDocumentRepository>();
            var producer = new Mock<IRabbitMqProducer>();
            var minio = new Mock<MinioService>(MockBehavior.Strict, (IConfiguration)null!); 
            var logger = new Mock<ILogger<DocumentsController>>();


            var fileBytes = new byte[] { 1, 2, 3, 4 };
            var fileStream = new MemoryStream(fileBytes);
            var formFile = new FormFile(fileStream, 0, fileBytes.Length, "file", "test.pdf");

            repo.Setup(r => r.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Document d, CancellationToken _) => d);
            producer.Setup(p => p.PublishAsync(It.IsAny<Document>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(() => Task.CompletedTask);
            minio.Setup(m => m.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>()))
                .Returns(Task.CompletedTask);

            var controller = new DocumentsController(repo.Object, producer.Object, logger.Object);

            var result = await controller.Create(formFile, minio.Object, CancellationToken.None);

            Assert.IsInstanceOf<CreatedAtActionResult>(result);
            minio.Verify(m => m.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>()), Times.Once);
            producer.Verify(p => p.PublishAsync(It.IsAny<Document>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task UploadFileNoFileReturnsBadRequest()
        {
            var repo = new Mock<IDocumentRepository>();
            var producer = new Mock<IRabbitMqProducer>();
            var minio = new Mock<MinioService>(MockBehavior.Loose, (IConfiguration)null!);
            var logger = new Mock<ILogger<DocumentsController>>();

            var controller = new DocumentsController(repo.Object, producer.Object, logger.Object);

            var result = await controller.Create(null, minio.Object, CancellationToken.None);

            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        [Test]
        public async Task MinioServiceUploadAsyncEnsuresBucketAndUploads()
        {
            var mockClient = new Mock<IMinioClient>();
            mockClient.Setup(c => c.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(false);
            mockClient.Setup(c => c.MakeBucketAsync(It.IsAny<MakeBucketArgs>(), It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask);
            mockClient.Setup(c => c.PutObjectAsync(It.IsAny<PutObjectArgs>(), It.IsAny<CancellationToken>()))
                      .Returns(() => Task.CompletedTask);

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Minio:Endpoint"] = "minio:9000",
                    ["Minio:AccessKey"] = "key",
                    ["Minio:SecretKey"] = "secret",
                    ["Minio:Bucket"] = "documents"
                })
                .Build();

            var service = new MinioService(config);

            typeof(MinioService)
                .GetField("_client", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .SetValue(service, mockClient.Object);

            await service.UploadAsync("file.pdf", new MemoryStream(new byte[10]));

            mockClient.Verify(c => c.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()), Times.Once);
            mockClient.Verify(c => c.PutObjectAsync(It.IsAny<PutObjectArgs>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task RabbitMqProducerPublishesMessage()
        {
            var factory = new Mock<ConnectionFactory>();
            var channel = new Mock<IModel>();

            var options = Options.Create(new RabbitMqSettings
            {
                Host = "rabbitmq",
                Queue = "ocr_jobs"
            });

            var producer = new RabbitMqProducer(options);

            await producer.PublishAsync(
                new Document { Id = Guid.NewGuid(), FileName = "demo.pdf" },
                "localhost",
                "ocr_jobs"
            );

            Assert.Pass("PublishAsync executed without errors");
        }
    }
}