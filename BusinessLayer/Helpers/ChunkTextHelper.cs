using System.Text;
using System.Text.RegularExpressions;

namespace BusinessLayer.Helpers;

/// <summary>
/// Shared word counting and hard-split utilities for all chunking strategies.
/// ChunkSize in DB = maximum words per chunk (not characters).
/// </summary>
public static class ChunkTextHelper
{
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

    public static int CountWords(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        return WhitespaceRegex.Split(text.Trim()).Length;
    }

    public static string[] SplitWords(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        return WhitespaceRegex.Split(text.Trim());
    }

    public static string JoinWords(IEnumerable<string> words)
        => string.Join(' ', words);

    /// <summary>
    /// Split text into fixed-size word windows with overlap. Every chunk has at most maxWords words.
    /// </summary>
    public static List<string> SplitByWordCount(string text, int maxWords, int overlapWords)
    {
        if (string.IsNullOrWhiteSpace(text) || maxWords <= 0)
            return [];

        overlapWords = Math.Clamp(overlapWords, 0, maxWords - 1);
        var words = SplitWords(text);
        if (words.Length == 0)
            return [];

        var chunks = new List<string>();
        var start = 0;

        while (start < words.Length)
        {
            var end = Math.Min(start + maxWords, words.Length);
            var chunk = JoinWords(words[start..end]);
            if (!string.IsNullOrWhiteSpace(chunk))
                chunks.Add(chunk);

            if (end >= words.Length)
                break;

            start += maxWords - overlapWords;
        }

        return chunks;
    }

    /// <summary>
    /// Safety net: re-split any chunk that still exceeds maxWords.
    /// </summary>
    public static List<string> EnforceMaxWords(IEnumerable<string> chunks, int maxWords, int overlapWords)
    {
        var result = new List<string>();

        foreach (var chunk in chunks)
        {
            if (CountWords(chunk) <= maxWords)
            {
                if (!string.IsNullOrWhiteSpace(chunk))
                    result.Add(chunk.Trim());
                continue;
            }

            result.AddRange(SplitByWordCount(chunk, maxWords, overlapWords));
        }

        return result;
    }
}
