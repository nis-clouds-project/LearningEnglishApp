namespace Frontend.Models
{
    public class GeneratedTextResponse
    {
        public string EnglishText { get; set; } = string.Empty;
        public string RussianText { get; set; } = string.Empty;
        public Dictionary<string, string> Words { get; set; } = new();
    }
}