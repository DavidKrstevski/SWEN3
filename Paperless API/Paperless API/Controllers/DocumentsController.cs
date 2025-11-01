using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Paperless_API.Data;
using Paperless_API.Data.Repositories;
using Paperless_API.Entities;
using Paperless_API.Exceptions;
using Paperless_API.Messaging;
using Paperless_API.Services;
using System.Reflection.Metadata;
using System.Xml.Linq;
using Document = Paperless_API.Entities.Document;

namespace Paperless_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentsController : ControllerBase
    {
        private readonly IDocumentRepository _repo;
        private readonly IRabbitMqProducer _producer;
        private readonly ILogger<DocumentsController> _logger;

        public DocumentsController(IDocumentRepository repo, IRabbitMqProducer producer, ILogger<DocumentsController> logger)
        {
            _repo = repo;
            _producer = producer;
            _logger = logger;
        }

        // POST api/documents
        [HttpPost]
        [RequestSizeLimit(50_000_000)] 
        public async Task<IActionResult> Create(IFormFile file, [FromServices] MinioService minio, CancellationToken ct)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var doc = new Document
            {
                Id = Guid.NewGuid(),
                FileName = file.FileName,
                Size = file.Length,
                UploadDate = DateTimeOffset.UtcNow
            };

            await using var stream = file.OpenReadStream();
            await minio.UploadAsync(doc.Id + ".pdf", stream);

            await _repo.AddAsync(doc, ct);
            await _producer.PublishAsync(doc, _producer.Host, _producer.Queue);

            return CreatedAtAction(nameof(GetById), new { id = doc.Id }, doc);
        }

        // GET api/documents/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            try
            {
                var doc = await _repo.GetAsync(id, ct);
                return Ok(doc);
            }
            catch (DocumentNotFoundException)
            {
                _logger.LogWarning("Unable to get Document {DocId} not found in database", id);
                return NotFound();
            }
        }


        // GET api/documents/
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var doc = await _repo.GetAllAsync(ct);
            if (doc is null)
            {
                _logger.LogInformation("No Documents exist in the Database");
                return NotFound();
            }
            else
                return Ok(doc);
        }

        // DELETE api/documents/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            try
            {
                await _repo.DeleteAsync(id, ct);
                return NoContent();
            }
            catch (DocumentNotFoundException)
            {
                _logger.LogWarning("Unable to delete Document {DocId} not found in database", id);
                return NotFound();
            }
        }

    }
}
