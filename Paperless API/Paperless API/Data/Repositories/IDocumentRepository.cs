using Paperless_API.Entities;

namespace Paperless_API.Data.Repositories
{
    public interface IDocumentRepository
    {
        Task<Document> AddAsync(Document doc, CancellationToken ct = default);
        Task<Document?> GetAsync(Guid id, CancellationToken ct = default);
        Task UpdateAsync(Document doc, CancellationToken ct = default);
        Task DeleteAsync(Guid id, CancellationToken ct = default);
    }
}
