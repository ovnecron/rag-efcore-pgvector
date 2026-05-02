using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using RagEfcorePgvector.Models;

namespace RagEfcorePgvector.Services;

public sealed partial class DeterministicEmbeddingService : IEmbeddingService
{
    public Task<float[]> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var values = new float[Document.EmbeddingDimensions];

        foreach (Match match in TokenRegex().Matches(text))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var token = match.Value.ToLowerInvariant();
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));

            var index = BitConverter.ToUInt16(hash.AsSpan(0, 2)) % values.Length;
            var sign = (hash[2] & 1) == 0 ? 1f : -1f;

            values[index] += sign;
        }

        Normalize(values);
        return Task.FromResult(values);
    }

    private static void Normalize(float[] values)
    {
        var sumOfSquares = 0f;

        foreach (var value in values)
        {
            sumOfSquares += value * value;
        }

        if (sumOfSquares == 0)
        {
            values[0] = 1;
            return;
        }

        var magnitude = MathF.Sqrt(sumOfSquares);

        for (var i = 0; i < values.Length; i++)
        {
            values[i] /= magnitude;
        }
    }

    [GeneratedRegex("[a-z0-9]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TokenRegex();
}
