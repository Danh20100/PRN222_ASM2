namespace BusinessLayer.Strategies;

/// <summary>
/// FixedSize chunker — splits text into overlapping windows of fixed character/token size.
/// </summary>
public class FixedSizeChunkingStrategy : IChunkingStrategy
{
    public string StrategyType => "FixedSize";

    public List<string> Chunk(string text, int chunkSize, int chunkOverlap)
    {
        if (string.IsNullOrWhiteSpace(text)) return [];

        var chunks = new List<string>();
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        int start = 0;
        while (start < words.Length)
        {
            int end = Math.Min(start + chunkSize, words.Length);
            var chunk = string.Join(' ', words[start..end]).Trim();
            if (!string.IsNullOrWhiteSpace(chunk))
                chunks.Add(chunk);

            start += chunkSize - chunkOverlap;
            if (start >= words.Length) break;
        }

        return chunks;
    }
}
