namespace Backend.Models
{
    public class SaveTranslationRequest
    {
        public string Text { get; set; } = string.Empty;
        public string Translation { get; set; } = string.Empty;
        public long UserId { get; set; }
        public long? CategoryId { get; set; }
    }
} 