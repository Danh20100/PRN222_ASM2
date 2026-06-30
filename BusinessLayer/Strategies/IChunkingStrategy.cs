namespace BusinessLayer.Strategies;

/// <summary>
/// Strategy Pattern contract for text chunking.
/// </summary>
public interface IChunkingStrategy
{
    /// <summary>Strategy identifier matching ChunkingStrategy.StrategyType in DB</summary>
    string StrategyType { get; }

    /// <summary>Split raw text into chunks. chunkSize = max words per chunk.</summary>
    List<string> Chunk(string text, int chunkSize, int chunkOverlap);
}
