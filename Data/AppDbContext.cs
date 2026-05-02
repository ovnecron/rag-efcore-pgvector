using Microsoft.EntityFrameworkCore;
using RagEfcorePgvector.Models;

namespace RagEfcorePgvector.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Document> Documents => Set<Document>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<Document>(entity =>
        {
            entity.ToTable("documents");

            entity.HasKey(document => document.Id);

            entity.Property(document => document.Title)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(document => document.Content)
                .IsRequired();

            entity.Property(document => document.Embedding)
                .HasColumnType("vector(1536)")
                .IsRequired();

            entity.Property(document => document.UpdatedAt)
                .HasDefaultValueSql("now()");

            entity.HasIndex(document => document.TenantId);

            entity.HasIndex(document => document.Embedding)
                .HasMethod("hnsw")
                .HasOperators("vector_cosine_ops")
                .HasStorageParameter("m", 16)
                .HasStorageParameter("ef_construction", 64);
        });
    }
}
