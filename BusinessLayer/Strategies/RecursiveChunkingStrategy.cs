namespace BusinessLayer.Strategies;

/// <summary>
/// Recursive chunker — tries to split hierarchically: 
/// paragraph → sentence → word, until chunks are ≤ chunkSize.
/// Mimics LangChain's RecursiveCharacterTextSplitter.
/// </summary>
public class RecursiveChunkingStrategy : IChunkingStrategy
{
    public string StrategyType => "Recursive";

    private static readonly string[] Separators = ["\n\n", "\n", ". ", " "];

    public List<string> Chunk(string text, int chunkSize, int chunkOverlap)
    {
        if (string.IsNullOrWhiteSpace(text)) return [];
        return RecursiveSplit(text.Trim(), chunkSize, chunkOverlap, 0);
    }

    private static List<string> RecursiveSplit(string text, int chunkSize, int chunkOverlap, int separatorIndex)
    {
        var wordCount = text.Split(' ').Length;

        // Base case: fits in one chunk
        if (wordCount <= chunkSize)
            return [text];

        // Try current separator
        if (separatorIndex >= Separators.Length)
        {
            // Last resort: hard split by word count
            return HardSplit(text, chunkSize, chunkOverlap);
        }

        var separator = Separators[separatorIndex];
        var parts = text.Split(separator, StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => p.Trim())
                        .Where(p => !string.IsNullOrWhiteSpace(p))
                        .ToList();

        if (parts.Count <= 1)
        {
            // Separator didn't help, try next level
            return RecursiveSplit(text, chunkSize, chunkOverlap, separatorIndex + 1);
        }

        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        int currentWords = 0;

        foreach (var part in parts)
        {
            var partWords = part.Split(' ').Length;

            if (partWords > chunkSize)
            {
                // Flush current buffer
                if (current.Length > 0)
                {
                    result.Add(current.ToString().Trim());
                    current.Clear();
                    currentWords = 0;
                }
                // Recursively split this large part
                result.AddRange(RecursiveSplit(part, chunkSize, chunkOverlap, separatorIndex + 1));
                continue;
            }

            if (currentWords + partWords > chunkSize && current.Length > 0)
            {
                result.Add(current.ToString().Trim());

                // Apply overlap
                if (chunkOverlap > 0 && current.Length > 0)
                {
                    var overlapWords = current.ToString()
                        .Split(' ')
                        .TakeLast(chunkOverlap)
                        .ToArray();
                    current.Clear();
                    current.Append(string.Join(' ', overlapWords));
                    currentWords = overlapWords.Length;
                }
                else
                {
                    current.Clear();
                    currentWords = 0;
                }
            }

            if (current.Length > 0) current.Append(separator);
            current.Append(part);
            currentWords += partWords;
        }

        if (current.Length > 0)
            result.Add(current.ToString().Trim());

        return result;
    }

    private static List<string> HardSplit(string text, int chunkSize, int chunkOverlap)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var chunks = new List<string>();
        int start = 0;
        while (start < words.Length)
        {
            int end = Math.Min(start + chunkSize, words.Length);
            chunks.Add(string.Join(' ', words[start..end]));
            start += chunkSize - chunkOverlap;
        }
        return chunks;
    }
}
