using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using RagEfcorePgvector.Data;
using RagEfcorePgvector.Models;
using RagEfcorePgvector.Services;

var connectionString = DatabaseSettings.GetConnectionString(args);
var question = DatabaseSettings.GetQuestion(args)
    ?? "How can I combine vector search with relational filters?";
const int currentTenantId = 42;

var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>()
    .UseNpgsql(connectionString, npgsql => npgsql.UseVector());

if (DatabaseSettings.ShouldLogSql(args))
{
    optionsBuilder.LogTo(Console.WriteLine);
}

var options = optionsBuilder.Options;

var embeddingService = new DeterministicEmbeddingService();

await using var dbContext = new AppDbContext(options);

Console.WriteLine("Ensuring PostgreSQL schema exists...");
// Demo-only schema setup. For production applications, prefer EF Core migrations.
await dbContext.Database.EnsureCreatedAsync();

await SeedDocumentsAsync(dbContext, embeddingService);

var searchService = new KnowledgeBaseSearchService(dbContext, embeddingService);

await using var transaction = await dbContext.Database.BeginTransactionAsync();

// Query-time HNSW tuning for this transaction.
// Higher values improve recall, but increase query cost.
await dbContext.Database.ExecuteSqlRawAsync("SET LOCAL hnsw.ef_search = 80");

var results = await searchService.SearchKnowledgeBaseAsync(
    currentTenantId,
    question,
    matchCount: 5);

await transaction.CommitAsync();

Console.WriteLine();
Console.WriteLine($"Question: {question}");
Console.WriteLine($"Tenant: {currentTenantId}");
Console.WriteLine();
Console.WriteLine("Top matches:");

foreach (var result in results)
{
    var cosineDistance = result.CosineDistance.ToString("F4", CultureInfo.InvariantCulture);
    Console.WriteLine($"- {result.Title} (distance: {cosineDistance})");
    Console.WriteLine($"  {result.Content}");
}

static async Task SeedDocumentsAsync(
    AppDbContext dbContext,
    IEmbeddingService embeddingService,
    CancellationToken cancellationToken = default)
{
    if (await dbContext.Documents.AnyAsync(cancellationToken))
    {
        return;
    }

    var seedDocuments = new[]
    {
        new
        {
            TenantId = 42,
            Title = "RAG Storage Architecture",
            Content = "Store embeddings beside business records in PostgreSQL to avoid synchronizing a second vector datastore."
        },
        new
        {
            TenantId = 42,
            Title = "Tenant Scoped Retrieval",
            Content = "Always combine semantic search with relational filters such as TenantId, permissions, status, and document version."
        },
        new
        {
            TenantId = 42,
            Title = "HNSW Tuning",
            Content = "HNSW indexes trade recall for speed. Tune m and ef_construction at index build time, then hnsw.ef_search at query time."
        },
        new
        {
            TenantId = 7,
            Title = "Different Tenant Support Notes",
            Content = "This document belongs to another tenant and should not appear in tenant 42 search results."
        },
        new
        {
            TenantId = 42,
            Title = "Embedding Dimensions",
            Content = "Match the vector column dimensions to the embedding model output. Use reduced dimensions or halfvec for larger indexed vectors."
        }
    };

    foreach (var document in seedDocuments)
    {
        var embedding = await embeddingService.GenerateEmbeddingAsync(
            $"{document.Title}\n{document.Content}",
            cancellationToken);

        dbContext.Documents.Add(new Document
        {
            TenantId = document.TenantId,
            Title = document.Title,
            Content = document.Content,
            Embedding = new Vector(embedding)
        });
    }

    await dbContext.SaveChangesAsync(cancellationToken);
}
