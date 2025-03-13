namespace Frontend.Models
{
    public class Word
    {
        public long Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public string Translation { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public long CategoryId { get; set; }
    }
} 