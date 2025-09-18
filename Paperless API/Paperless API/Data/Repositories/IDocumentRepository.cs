using Paperless_API.Entities;

namespace Paperless_API.Data.Repositories
{
    public interface IDocumentRepository
    {
        Task<Document> AddAsync(Document doc, CancellationToken ct);
        Task<Document> GetAsync(Guid id, CancellationToken ct);
        Task<List<Document>> GetAllAsync(CancellationToken ct);
        Task DeleteAsync(Guid id, CancellationToken ct);

    }
}
