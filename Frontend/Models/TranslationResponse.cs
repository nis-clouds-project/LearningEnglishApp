namespace Frontend.Models
{
    public class TranslationResponse
    {
        public string OriginalText { get; set; } = string.Empty;
        public string TranslatedText { get; set; } = string.Empty;
        public string TargetLanguage { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
    }
}