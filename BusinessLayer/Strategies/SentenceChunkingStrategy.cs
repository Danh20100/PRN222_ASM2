using System.Text.RegularExpressions;
using BusinessLayer.Helpers;

namespace BusinessLayer.Strategies;

/// <summary>
/// Sentence chunker — groups sentences up to max word count, never exceeding chunkSize.
/// </summary>
public class SentenceChunkingStrategy : IChunkingStrategy
{
    public string StrategyType => "Sentence";

    private static readonly Regex SentenceRegex = new(
        @"(?<=[.!?])\s+(?=[A-ZÀÁÂĂÃẠẢẤẦẨẪẬẮẰẲẴẶÉÈÊẼẺẸẾỀỂỄỆÍÌĨỈỊÓÒÔÕỌỐỒỔỖỘỚỜỞỠỢÚÙÛŨỤỨỪỬỮỰÝỲỶỸ])",
        RegexOptions.Compiled);

    public List<string> Chunk(string text, int chunkSize, int chunkOverlap)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var sentences = SentenceRegex
            .Split(text)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToArray();

        if (sentences.Length == 0)
            return ChunkTextHelper.SplitByWordCount(text, chunkSize, chunkOverlap);

        var chunks = new List<string>();
        var window = new System.Text.StringBuilder();
        var wordCount = 0;

        foreach (var sentence in sentences)
        {
            var sentenceWords = ChunkTextHelper.CountWords(sentence);

            if (sentenceWords > chunkSize)
            {
                FlushWindow();
                chunks.AddRange(ChunkTextHelper.SplitByWordCount(sentence, chunkSize, chunkOverlap));
                continue;
            }

            if (wordCount + sentenceWords > chunkSize && window.Length > 0)
                FlushWindow();

            if (window.Length > 0)
                window.Append(' ');
            window.Append(sentence);
            wordCount += sentenceWords;
        }

        FlushWindow();
        return ChunkTextHelper.EnforceMaxWords(chunks, chunkSize, chunkOverlap);

        void FlushWindow()
        {
            if (window.Length == 0)
                return;

            chunks.Add(window.ToString().Trim());
            window.Clear();
            wordCount = 0;
        }
    }
}
