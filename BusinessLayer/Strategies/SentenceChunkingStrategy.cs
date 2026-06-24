using System.Text.RegularExpressions;

namespace BusinessLayer.Strategies;

/// <summary>
/// Sentence chunker — splits at sentence boundaries and groups sentences 
/// into chunks using a sliding window with overlap.
/// </summary>
public class SentenceChunkingStrategy : IChunkingStrategy
{
    public string StrategyType => "Sentence";

    // Matches sentence-ending punctuation followed by space/end
    private static readonly Regex SentenceRegex = new(
        @"(?<=[.!?])\s+(?=[A-ZÀÁÂĂÃẠẢẤẦẨẪẬẮẰẲẴẶÉÈÊẼẺẸẾỀỂỄỆ])",
        RegexOptions.Compiled);

    public List<string> Chunk(string text, int chunkSize, int chunkOverlap)
    {
        if (string.IsNullOrWhiteSpace(text)) return [];

        var sentences = SentenceRegex
            .Split(text)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToArray();

        var chunks = new List<string>();
        int start = 0;

        while (start < sentences.Length)
        {
            var window = new System.Text.StringBuilder();
            int wordCount = 0;
            int end = start;

            // Fill window up to chunkSize words
            while (end < sentences.Length)
            {
                var sentenceWords = sentences[end].Split(' ').Length;
                if (wordCount + sentenceWords > chunkSize && window.Length > 0)
                    break;

                if (window.Length > 0) window.Append(' ');
                window.Append(sentences[end]);
                wordCount += sentenceWords;
                end++;
            }

            chunks.Add(window.ToString().Trim());

            // Overlap: step back by chunkOverlap sentences
            int step = Math.Max(1, end - start - chunkOverlap);
            start += step;
        }

        return chunks;
    }
}
