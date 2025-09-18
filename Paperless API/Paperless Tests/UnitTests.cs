using Microsoft.AspNetCore.Mvc;
using Moq;
using Paperless_API.Controllers;
using Paperless_API.Data.Repositories;
using Document = Paperless_API.Entities.Document;

namespace Paperless_Tests
{
    public class DocumentsControllerTests
    {
        private Mock<IDocumentRepository> _repo = null;
        private DocumentsController _docController = null;
        private readonly CancellationToken _ct = CancellationToken.None;


        [SetUp]
        public void SetUp()
        {
            _repo = new Mock<IDocumentRepository>();
            _docController = new DocumentsController(_repo.Object);
        }

        [Test]
        public async Task CreateReturns201()
        {
            _repo.Setup(r => r.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Document doc, CancellationToken ct) => doc);

            var input = new Document { FileName = "demo.pdf", Size = 123 };

            var result = await _docController.Create(input, _ct);

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
            _repo.Setup(r => r.GetAsync(id, _ct)).ReturnsAsync((Document?)null);

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