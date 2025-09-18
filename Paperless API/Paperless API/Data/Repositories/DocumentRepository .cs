using Microsoft.EntityFrameworkCore;
using Paperless_API.Entities;

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

        public Task<Document> GetAsync(Guid id, CancellationToken ct) =>
            _db.Documents.FirstOrDefaultAsync(d => d.Id == id, ct);

        public Task<List<Document>> GetAllAsync(CancellationToken ct) =>
            _db.Documents.ToListAsync(ct);

        public async Task DeleteAsync(Guid id, CancellationToken ct)
        {
            var doc = new Document { Id = id };
            _db.Attach(doc);
            _db.Remove(doc);
            await _db.SaveChangesAsync(ct);
        }
    }
}
