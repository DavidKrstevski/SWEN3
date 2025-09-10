using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Paperless_API.Data;
using Paperless_API.Data.Repositories;
using Paperless_API.Entities;
using System.Reflection.Metadata;
using Document = Paperless_API.Entities.Document;

namespace Paperless_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentsController : ControllerBase
    {
        private readonly IDocumentRepository _repo;
        public DocumentsController(IDocumentRepository repo) => _repo = repo;

        // POST api/documents
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Document doc, CancellationToken ct)
        {
            if (doc is null || string.IsNullOrWhiteSpace(doc.FileName))
                return BadRequest();

            doc.Id = Guid.NewGuid();
            doc.UploadDate = DateTimeOffset.UtcNow;

            await _repo.AddAsync(doc, ct);
            return CreatedAtAction(nameof(GetById), new { id = doc.Id }, doc);
        }

        // GET api/documents/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            var doc = await _repo.GetAsync(id, ct);
            return doc is null ? NotFound() : Ok(doc);
        }

        // DELETE api/documents/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _repo.DeleteAsync(id, ct);
            return NoContent();
        }
    }
}
