namespace BusinessLayer.Helpers;

/// <summary>
/// Vector math utilities for RAG retrieval.
/// </summary>
public static class VectorHelper
{
    /// <summary>
    /// Compute cosine similarity between two float vectors.
    /// Returns a value in [0, 1] where 1 = identical direction.
    /// </summary>
    public static double CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException($"Vector dimension mismatch: {a.Length} vs {b.Length}");

        double dot = 0, normA = 0, normB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        if (normA == 0 || normB == 0) return 0;
        return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }

    /// <summary>
    /// Deserialize JSON embedding string to float[].
    /// Format: "[0.1, 0.2, -0.3, ...]"
    /// </summary>
    public static float[] DeserializeEmbedding(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "[]") return [];
        return System.Text.Json.JsonSerializer.Deserialize<float[]>(json) ?? [];
    }

    /// <summary>
    /// Serialize float[] to compact JSON string for DB storage.
    /// </summary>
    public static string SerializeEmbedding(float[] vector)
        => System.Text.Json.JsonSerializer.Serialize(vector);

    /// <summary>
    /// Get top-K most similar chunks by cosine similarity.
    /// Returns tuples of (index, score) sorted descending.
    /// </summary>
    public static List<(int Index, double Score)> TopKSimilar(
        float[] query,
        IList<float[]> candidates,
        int k)
    {
        return candidates
            .Select((vec, idx) => (Index: idx, Score: CosineSimilarity(query, vec)))
            .OrderByDescending(x => x.Score)
            .Take(k)
            .ToList();
    }
}
