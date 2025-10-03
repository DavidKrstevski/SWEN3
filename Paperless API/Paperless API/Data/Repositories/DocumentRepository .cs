using Microsoft.EntityFrameworkCore;
using Paperless_API.Entities;
using Paperless_API.Exceptions;

namespace Paperless_API.Data.Repositories
{
    public class DocumentRepository : IDocumentRepository
    {
        private readonly PaperlessDbContext _db;
        public DocumentRepository(PaperlessDbContext db) => _db = db;

        public async Task<Document> AddAsync(Document doc, CancellationToken ct)
        {
            _db.Documents.Add(doc);
            await _db.SaveChangesAsync(ct);
            return doc;
        }

        public async Task<Document> GetAsync(Guid id, CancellationToken ct)
        {
            var doc = await _db.Documents.FirstOrDefaultAsync(d => d.Id == id, ct);
            if (doc == null)
                throw new DocumentNotFoundException(id);

            return doc;
        }

        public Task<List<Document>> GetAllAsync(CancellationToken ct) =>
            _db.Documents.ToListAsync(ct);

        public async Task DeleteAsync(Guid id, CancellationToken ct)
        {
            var doc = await GetAsync(id, ct);
            _db.Documents.Remove(doc);
            await _db.SaveChangesAsync(ct);
        }
    }
}
