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
        foreach (var log in logs)
        {
            log.AccessDate = DateTime.SpecifyKind(
                log.AccessDate, DateTimeKind.Utc);

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
