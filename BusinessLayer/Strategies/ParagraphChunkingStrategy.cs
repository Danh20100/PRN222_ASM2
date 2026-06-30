using System.Text;
using BusinessLayer.Helpers;

namespace BusinessLayer.Strategies;

/// <summary>
/// Paragraph chunker — splits at double-newline boundaries, then enforces max word count.
/// </summary>
public class ParagraphChunkingStrategy : IChunkingStrategy
{
    public string StrategyType => "Paragraph";

    public List<string> Chunk(string text, int chunkSize, int chunkOverlap)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var paragraphs = text
            .Split(["\r\n\r\n", "\n\n"], StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();

        var chunks = new List<string>();
        var current = new StringBuilder();
        var currentWords = 0;

        foreach (var para in paragraphs)
        {
            var paraWords = ChunkTextHelper.CountWords(para);

            // Single paragraph exceeds limit → hard-split it
            if (paraWords > chunkSize)
            {
                FlushCurrent();
                chunks.AddRange(ChunkTextHelper.SplitByWordCount(para, chunkSize, chunkOverlap));
                continue;
            }

            if (currentWords + paraWords > chunkSize && current.Length > 0)
                FlushCurrent();

            if (current.Length > 0)
                current.Append("\n\n");
            current.Append(para);
            currentWords += paraWords;
        }

        FlushCurrent();
        return ChunkTextHelper.EnforceMaxWords(chunks, chunkSize, chunkOverlap);

        void FlushCurrent()
        {
            if (current.Length == 0)
                return;

            chunks.Add(current.ToString().Trim());
            current.Clear();
            currentWords = 0;
        }
    }
}
