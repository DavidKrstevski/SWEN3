using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using Paperless_API.Controllers;
using Paperless_API.Data.Repositories;
using Paperless_API.Entities;
using System;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Document = Paperless_API.Entities.Document;

namespace Paperless_Tests
{
    public class DocumentsControllerTests
    {
        private Mock<IDocumentRepository> _repo = null!;
        private DocumentsController _sut = null!;
        private readonly CancellationToken _ct = CancellationToken.None;


        [SetUp]
        public void SetUp()
        {
            _repo = new Mock<IDocumentRepository>();
            _sut = new DocumentsController(_repo.Object);
        }

        [Test]
        public async Task CreateReturns201()
        {
            _repo.Setup(r => r.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Document d, CancellationToken _) => d);

            var input = new Document { FileName = "demo.pdf", Size = 123 };

            var result = await _sut.Create(input, _ct);

            var created = result as CreatedAtActionResult;
            Assert.IsNotNull(created, "Expected CreatedAtActionResult");
            Assert.AreEqual(nameof(DocumentsController.GetById), created!.ActionName);

            var body = created.Value as Document;
            Assert.IsNotNull(body, "Expected Document body");
            Assert.AreEqual("demo.pdf", body!.FileName);
            Assert.AreNotEqual(Guid.Empty, body.Id);

            _repo.Verify(r => r.AddAsync(It.IsAny<Document>(), _ct), Times.Once);
            _repo.VerifyNoOtherCalls();
        }

        [Test]
        public async Task CreateReturns400EmptyFilename()
        {
            var input = new Document { FileName = "   ", Size = 1 };

            var result = await _sut.Create(input, _ct);

            Assert.IsInstanceOf<BadRequestResult>(result);
            _repo.VerifyNoOtherCalls();
        }

        [Test]
        public async Task CreateReturns400NoFilename()
        {
            var result = await _sut.Create(null!, _ct);

            Assert.IsInstanceOf<BadRequestResult>(result);
            _repo.VerifyNoOtherCalls();
        }

        [Test]
        public async Task GetByIdReturns200()
        {
            var id = Guid.NewGuid();
            var doc = new Document { Id = id, FileName = "x.pdf", Size = 9, UploadDate = DateTimeOffset.UtcNow };

            _repo.Setup(r => r.GetAsync(id, _ct)).ReturnsAsync(doc);

            var result = await _sut.GetById(id, _ct);

            var ok = result as OkObjectResult;
            Assert.IsNotNull(ok);
            var body = ok!.Value as Document;
            Assert.IsNotNull(body);
            Assert.AreEqual(id, body!.Id);

            _repo.Verify(r => r.GetAsync(id, _ct), Times.Once);
            _repo.VerifyNoOtherCalls();
        }

        [Test]
        public async Task GetByIdReturns404NoFile()
        {
            var id = Guid.NewGuid();
            _repo.Setup(r => r.GetAsync(id, _ct)).ReturnsAsync((Document?)null);

            var result = await _sut.GetById(id, _ct);

            Assert.IsInstanceOf<NotFoundResult>(result);
            _repo.Verify(r => r.GetAsync(id, _ct), Times.Once);
            _repo.VerifyNoOtherCalls();
        }

        [Test]
        public async Task DeleteReturns204()
        {
            var id = Guid.NewGuid();
            _repo.Setup(r => r.DeleteAsync(id, _ct)).Returns(Task.CompletedTask);

            var result = await _sut.Delete(id, _ct);

            Assert.IsInstanceOf<NoContentResult>(result);
            _repo.Verify(r => r.DeleteAsync(id, _ct), Times.Once);
            _repo.VerifyNoOtherCalls();
        }
    }
}