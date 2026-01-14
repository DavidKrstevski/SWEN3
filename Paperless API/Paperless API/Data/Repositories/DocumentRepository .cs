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

        public async Task<Document> UpdateAsync(Document updated, CancellationToken ct)
        {
            var existing = await _db.Documents.FirstOrDefaultAsync(d => d.Id == updated.Id, ct);
            if (existing == null)
                throw new DocumentNotFoundException(updated.Id);

            // Felder updaten (wichtig: Id nicht ändern)
            existing.FileName = updated.FileName;
            existing.Size = updated.Size;
            existing.UploadDate = updated.UploadDate;

            existing.Summary = updated.Summary;
            existing.ChatHistory = updated.ChatHistory;
            existing.RiskColor = updated.RiskColor;

            await _db.SaveChangesAsync(ct);
            return existing;
        }
    }
}
