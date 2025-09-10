using Microsoft.EntityFrameworkCore;
using Paperless_API.Entities;

namespace Paperless_API.Data.Repositories
{
    public class DocumentRepository : IDocumentRepository
    {
        private readonly PaperlessDbContext _db;
        public DocumentRepository(PaperlessDbContext db) => _db = db;

        public async Task<Document> AddAsync(Document doc, CancellationToken ct = default)
        {
            _db.Documents.Add(doc);
            await _db.SaveChangesAsync(ct);
            return doc;
        }

        public Task<Document?> GetAsync(Guid id, CancellationToken ct = default) =>
            _db.Documents.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, ct);

        public async Task UpdateAsync(Document doc, CancellationToken ct = default)
        {
            _db.Documents.Update(doc);
            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var stub = new Document { Id = id };
            _db.Attach(stub);
            _db.Remove(stub);
            await _db.SaveChangesAsync(ct);
        }
    }
}
