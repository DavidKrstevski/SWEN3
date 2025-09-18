using Microsoft.EntityFrameworkCore;
using Paperless_API.Entities;


namespace Paperless_API.Data
{
    public class PaperlessDbContext : DbContext
    {
        public PaperlessDbContext(DbContextOptions<PaperlessDbContext> options) : base(options) { }

        public DbSet<Document> Documents => Set<Document>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<Document>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.FileName).IsRequired();
            });
        }
    }
}
