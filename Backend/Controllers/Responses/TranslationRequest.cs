namespace Backend.Controllers.Responses
{
    public class TranslationRequest
    {
        public string Text { get; set; } = string.Empty;
        public string TargetLanguage { get; set; } = string.Empty;
    }
}