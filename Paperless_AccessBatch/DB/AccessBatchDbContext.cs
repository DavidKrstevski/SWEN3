using Microsoft.EntityFrameworkCore;
using Paperless_AccessBatch.Entities;

namespace Paperless_AccessBatch.DB
{
    public class AccessBatchDbContext : DbContext
    {
        public AccessBatchDbContext(DbContextOptions<AccessBatchDbContext> options)
            : base(options)
        {
        }

        public DbSet<DocumentAccessLog> DocumentAccessLogs => Set<DocumentAccessLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DocumentAccessLog>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => new { e.DocumentId, e.AccessDate })
                      .IsUnique();
            });
        }
    }
}
