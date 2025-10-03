using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Paperless_API.Controllers;
using Paperless_API.Data.Repositories;
using Paperless_API.Exceptions;
using Paperless_API.Messaging;
using Document = Paperless_API.Entities.Document;

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


            repo.Setup(r => r.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Document doc, CancellationToken ct) => doc);

            var controller = new DocumentsController(repo.Object, producer.Object, _logger.Object);
            var input = new Document { FileName = "demo.pdf", Size = 123 };

            var result = await controller.Create(input, CancellationToken.None);

            var created = result as CreatedAtActionResult;
            var body = created.Value as Document;
            Assert.IsNotNull(body, "Expected Document body");
            Assert.AreEqual("demo.pdf", body.FileName);
        }

        [Test]
        public async Task CreateReturns400EmptyFilename()
        {
            var input = new Document { FileName = "   ", Size = 1234 };
            var result = await _docController.Create(input, _ct);

            Assert.IsInstanceOf<BadRequestResult>(result);
        }

        [Test]
        public async Task CreateReturns400NoFilename()
        {
            var result = await _docController.Create(null, _ct);

            Assert.IsInstanceOf<BadRequestResult>(result);
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
    }
}