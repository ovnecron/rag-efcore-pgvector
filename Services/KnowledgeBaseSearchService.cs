using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using RagEfcorePgvector.Data;

namespace RagEfcorePgvector.Services;

public sealed class KnowledgeBaseSearchService(
    AppDbContext dbContext,
    IEmbeddingService embeddingService)
{
    public async Task<List<DocumentSearchResult>> SearchKnowledgeBaseAsync(
        int currentTenantId,
        string userQuestion,
        int matchCount = 5,
        CancellationToken cancellationToken = default)
    {
        var embeddingArray = await embeddingService.GenerateEmbeddingAsync(
            userQuestion,
            cancellationToken);

        var queryVector = new Vector(embeddingArray);

        return await dbContext.Documents
            .AsNoTracking()
            .Where(document => document.TenantId == currentTenantId)
            .OrderBy(document => document.Embedding.CosineDistance(queryVector))
            .Select(document => new DocumentSearchResult(
                document.Id,
                document.TenantId,
                document.Title,
                document.Content,
                document.Embedding.CosineDistance(queryVector)))
            .Take(matchCount)
            .ToListAsync(cancellationToken);
    }
}
