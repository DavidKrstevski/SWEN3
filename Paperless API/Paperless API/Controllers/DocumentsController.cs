using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Paperless_API.Data;
using Paperless_API.Data.Repositories;
using Paperless_API.Entities;
using Paperless_API.Exceptions;
using Paperless_API.Messaging;
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
        public async Task<IActionResult> Create([FromBody] Document doc, CancellationToken ct)
        {
            if (doc is null || string.IsNullOrWhiteSpace(doc.FileName))
            {
                _logger.LogWarning("Invalid document received");
                return BadRequest();
            }

            doc.Id = Guid.NewGuid();
            doc.UploadDate = DateTimeOffset.UtcNow;

            await _repo.AddAsync(doc, ct);
            _logger.LogInformation("Document {DocId} saved to DB", doc.Id);

            await _producer.PublishAsync(doc, _producer.Host, _producer.Queue);
            _logger.LogInformation("Document {DocId} published to RabbitMQ", doc.Id);

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
