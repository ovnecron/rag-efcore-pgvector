using Pgvector;
using System.ComponentModel.DataAnnotations.Schema;

namespace RagEfcorePgvector.Models;

public sealed class Document
{
    public const int EmbeddingDimensions = 1536;

    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    // 1536 matches OpenAI text-embedding-3-small's default output.
    // Keep this aligned with the embedding model you actually use.
    [Column(TypeName = "vector(1536)")]
    public Vector Embedding { get; set; } = new(new float[EmbeddingDimensions]);

    public int TenantId { get; set; }

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
