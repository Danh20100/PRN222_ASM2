namespace BusinessLayer.Strategies;

/// <summary>
/// Paragraph chunker — splits at double-newline boundaries.
/// Respects maxChunkSize by merging small paragraphs.
/// </summary>
public class ParagraphChunkingStrategy : IChunkingStrategy
{
    public string StrategyType => "Paragraph";

    public List<string> Chunk(string text, int chunkSize, int chunkOverlap)
    {
        if (string.IsNullOrWhiteSpace(text)) return [];

        // Split on double newlines (paragraph breaks)
        var paragraphs = text
            .Split(["\r\n\r\n", "\n\n"], StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();

        var chunks = new List<string>();
        var current = new System.Text.StringBuilder();

        foreach (var para in paragraphs)
        {
            var paraWords = para.Split(' ').Length;
            var currentWords = current.Length > 0
                ? current.ToString().Split(' ').Length
                : 0;

            if (currentWords + paraWords > chunkSize && current.Length > 0)
            {
                chunks.Add(current.ToString().Trim());
                current.Clear();
            }

            if (current.Length > 0) current.Append("\n\n");
            current.Append(para);
        }

        if (current.Length > 0)
            chunks.Add(current.ToString().Trim());

        return chunks;
    }
}
