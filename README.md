# RAG with EF Core and pgvector

This repository accompanies the article:

[RAG with EF Core and pgvector](https://www.lukaswalter.dev/posts/rag-efcore-pgvector/)

This project implements the article sample as a runnable .NET console app:

- PostgreSQL stores documents, tenant metadata, and vector embeddings together.
- EF Core enables the `vector` extension with `modelBuilder.HasPostgresExtension("vector")`.
- The `Document.Embedding` column is mapped as `vector(1536)`.
- The embedding column has an HNSW index with `vector_cosine_ops`.
- Retrieval combines `TenantId` filtering and cosine-distance ordering in one LINQ query.

## Run PostgreSQL

From the repository root:

```bash
docker compose up -d
```

The compose file uses the `pgvector/pgvector:pg16` image. The extension is installed in the image. The app enables it in the target database through the EF Core model configuration.

This works with the default local `postgres` user from the compose file. In managed or locked-down PostgreSQL environments, the database user may need permission to create extensions.

## Run the Sample

From the repository root:

```bash
dotnet run -- "How do I filter vector search by tenant?"
```

On first run, the app creates the schema, enables the `vector` extension, creates the HNSW index, seeds sample documents, and runs a tenant-scoped vector search.

Expected behavior: the document from tenant `7` should not appear when searching as tenant `42`.

If you want to inspect the generated SQL, add `--log-sql`:

```bash
dotnet run -- --log-sql "How do I filter vector search by tenant?"
```

The default connection string is:

```text
Host=localhost;Port=5432;Database=rag_efcore_pgvector;Username=postgres;Password=postgres
```

Override it with an environment variable from the repository root:

```bash
POSTGRES_CONNECTION_STRING="Host=localhost;Port=5432;Database=rag_efcore_pgvector;Username=postgres;Password=postgres" \
dotnet run
```

Or pass it as an argument from the repository root:

```bash
dotnet run -- \
  --connection="Host=localhost;Port=5432;Database=rag_efcore_pgvector;Username=postgres;Password=postgres" \
  "How should I tune HNSW?"
```

Stop PostgreSQL when you are done:

```bash
docker compose down
```

To remove the database volume and start from a clean database:

```bash
docker compose down -v
```

This is especially useful after schema or seed-data changes because the compose file uses a named volume and `EnsureCreatedAsync()` will not update an existing schema after model changes.

## Demo Schema Setup

The sample uses `EnsureCreatedAsync()` to keep local setup small:

```csharp
await dbContext.Database.EnsureCreatedAsync();
```

This is demo-only schema setup. For production applications, prefer EF Core migrations. `EnsureCreatedAsync()` is best suited to clean local databases, tests, prototypes, or transient data stores, and it does not work well together with migrations.

## Replace the Embedding Stub

This sample uses `DeterministicEmbeddingService` to keep the demo local and reproducible.

It is not meant to produce production-quality semantic embeddings.

In a real RAG application, replace `DeterministicEmbeddingService` with an embedding provider such as OpenAI, Azure OpenAI, or another model exposed through your preferred .NET AI library.

Keep `Document.EmbeddingDimensions`, the deterministic embedding stub output, and `vector(1536)` aligned with the embedding model output. If you use larger embeddings and need indexed search, reduce the embedding dimensions or evaluate `halfvec`/`HalfVector`.

## HNSW Query Tuning

The sample uses transaction-scoped query tuning:

```csharp
await using var transaction = await dbContext.Database.BeginTransactionAsync();

await dbContext.Database.ExecuteSqlRawAsync("SET LOCAL hnsw.ef_search = 80");

var results = await searchService.SearchKnowledgeBaseAsync(
    currentTenantId,
    question,
    matchCount: 5);

await transaction.CommitAsync();
```

pgvector documents `SET LOCAL` inside a transaction for query-scoped `hnsw.ef_search`. The setting stays scoped to this transaction, which is a safer pattern with EF Core and Npgsql connection pooling than setting a session value globally. Higher values usually improve recall, but increase query cost.

## Relevant Files

- `Models/Document.cs`: entity with relational metadata and `Vector` embedding.
- `Data/AppDbContext.cs`: pgvector extension, tenant index, and HNSW index configuration.
- `Services/KnowledgeBaseSearchService.cs`: tenant-filtered cosine vector search in LINQ.
- `Program.cs`: schema creation, seed data, `hnsw.ef_search` tuning, and sample query.
