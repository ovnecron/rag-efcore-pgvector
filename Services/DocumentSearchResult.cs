namespace RagEfcorePgvector.Services;

public sealed record DocumentSearchResult(
    int Id,
    int TenantId,
    string Title,
    string Content,
    double CosineDistance);
