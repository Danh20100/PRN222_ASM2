using BusinessLayer.Helpers;

namespace BusinessLayer.Strategies;

/// <summary>
/// FixedSize chunker — splits text into overlapping windows of fixed word count.
/// </summary>
public class FixedSizeChunkingStrategy : IChunkingStrategy
{
    public string StrategyType => "FixedSize";

    public List<string> Chunk(string text, int chunkSize, int chunkOverlap)
        => ChunkTextHelper.SplitByWordCount(text, chunkSize, chunkOverlap);
}
