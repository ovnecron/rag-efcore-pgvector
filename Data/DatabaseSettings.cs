namespace RagEfcorePgvector.Data;

public static class DatabaseSettings
{
    public const string DefaultConnectionString =
        "Host=localhost;Port=5432;Database=rag_efcore_pgvector;Username=postgres;Password=postgres";

    public static string GetConnectionString(IEnumerable<string>? args = null)
    {
        var connectionArg = args?
            .FirstOrDefault(arg => arg.StartsWith("--connection=", StringComparison.OrdinalIgnoreCase));

        if (connectionArg is not null)
        {
            return connectionArg["--connection=".Length..];
        }

        return Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? DefaultConnectionString;
    }

    public static string? GetQuestion(IEnumerable<string> args)
    {
        var questionParts = args
            .Where(arg => !arg.StartsWith("--", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return questionParts.Length == 0
            ? null
            : string.Join(' ', questionParts);
    }

    public static bool ShouldLogSql(IEnumerable<string> args)
    {
        return args.Any(arg => string.Equals(arg, "--log-sql", StringComparison.OrdinalIgnoreCase));
    }
}
