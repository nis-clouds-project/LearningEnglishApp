namespace Backend.Models
{
    public class TranslateRequest
    {
        public string Text { get; set; } = string.Empty;
        public string TargetLanguageCode { get; set; } = string.Empty;
    }
} 