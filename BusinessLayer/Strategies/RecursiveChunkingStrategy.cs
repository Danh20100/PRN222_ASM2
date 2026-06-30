using System.Text;
using BusinessLayer.Helpers;

namespace BusinessLayer.Strategies;

/// <summary>
/// Recursive chunker — splits hierarchically by paragraph, line, sentence, then word count.
/// </summary>
public class RecursiveChunkingStrategy : IChunkingStrategy
{
    public string StrategyType => "Recursive";

    private static readonly string[] Separators = ["\n\n", "\n", ". ", " "];

    public List<string> Chunk(string text, int chunkSize, int chunkOverlap)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var chunks = RecursiveSplit(text.Trim(), chunkSize, chunkOverlap, 0);
        return ChunkTextHelper.EnforceMaxWords(chunks, chunkSize, chunkOverlap);
    }

    private static List<string> RecursiveSplit(string text, int chunkSize, int chunkOverlap, int separatorIndex)
    {
        var wordCount = ChunkTextHelper.CountWords(text);

        if (wordCount <= chunkSize)
            return [text];

        if (separatorIndex >= Separators.Length)
            return ChunkTextHelper.SplitByWordCount(text, chunkSize, chunkOverlap);

        var separator = Separators[separatorIndex];
        var parts = text.Split(separator, StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();

        if (parts.Count <= 1)
            return RecursiveSplit(text, chunkSize, chunkOverlap, separatorIndex + 1);

        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        var currentWords = 0;

        foreach (var part in parts)
        {
            var partWords = ChunkTextHelper.CountWords(part);

            if (partWords > chunkSize)
            {
                FlushCurrent();
                result.AddRange(RecursiveSplit(part, chunkSize, chunkOverlap, separatorIndex + 1));
                continue;
            }

            if (currentWords + partWords > chunkSize && current.Length > 0)
            {
                FlushCurrent();

                if (chunkOverlap > 0 && result.Count > 0)
                {
                    var overlapText = TakeLastWords(result[^1], chunkOverlap);
                    if (!string.IsNullOrWhiteSpace(overlapText))
                    {
                        current.Append(overlapText);
                        currentWords = ChunkTextHelper.CountWords(overlapText);
                    }
                }
            }

            if (current.Length > 0)
                current.Append(separator);
            current.Append(part);
            currentWords += partWords;
        }

        FlushCurrent();
        return result;

        void FlushCurrent()
        {
            if (current.Length == 0)
                return;

            result.Add(current.ToString().Trim());
            current.Clear();
            currentWords = 0;
        }
    }

    private static string TakeLastWords(string text, int wordCount)
    {
        var words = ChunkTextHelper.SplitWords(text);
        if (words.Length == 0)
            return string.Empty;

        var take = Math.Min(wordCount, words.Length);
        return ChunkTextHelper.JoinWords(words[^take..]);
    }
}
