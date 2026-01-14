using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Paperless_API.Entities;
using System.Text.Json;


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

                e.Property(x => x.FileName).IsRequired().HasMaxLength(255);
                e.Property(x => x.Size).IsRequired();
                e.Property(x => x.UploadDate).IsRequired();

                e.Property(x => x.Summary).HasColumnType("text");
                e.Property(x => x.ChatHistory).HasColumnType("text");

                e.Property(x => x.RiskColor)
                    .HasConversion<string>()
                    .HasMaxLength(10);
            });
        }
    }
}
