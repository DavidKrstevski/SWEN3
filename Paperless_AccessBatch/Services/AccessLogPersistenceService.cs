using Microsoft.EntityFrameworkCore;
using Paperless_AccessBatch.DB;
using Paperless_AccessBatch.Entities;

public class AccessLogPersistenceService
{
    private readonly AccessBatchDbContext _db;

    public AccessLogPersistenceService(AccessBatchDbContext db)
    {
        _db = db;
    }

    public async Task PersistLogsAsync(
        List<DocumentAccessLog> logs,
        CancellationToken ct)
    {
        if (logs.Count == 0)
            return;

        var documentIds = logs
            .Select(l => l.DocumentId)
            .Distinct()
            .ToList();

        var existingDocumentIds = await _db.Database
            .SqlQuery<Guid>($"""
            SELECT "Id"
            FROM "Documents"
            WHERE "Id" = ANY ({documentIds})
        """)
            .ToListAsync(ct);

        var validLogs = logs
            .Where(l => existingDocumentIds.Contains(l.DocumentId))
            .ToList();

        if (validLogs.Count == 0)
            return;

        foreach (var log in validLogs)
        {
            var existing = await _db.DocumentAccessLogs
                .SingleOrDefaultAsync(l =>
                    l.DocumentId == log.DocumentId &&
                    l.AccessDate == log.AccessDate,
                    ct);

            if (existing != null)
            {
                existing.AccessCount += log.AccessCount;
            }
            else
            {
                _db.DocumentAccessLogs.Add(log);
            }
        }

        await _db.SaveChangesAsync(ct);
    }

}
