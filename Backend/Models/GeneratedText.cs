namespace Backend.Models
{
    public class GeneratedText
    {
        public string EnglishText { get; set; }
        public string RussianText { get; set; }
        public Dictionary<string, string> Words { get; set; }

        public GeneratedText(string englishText, string russianText)
        {
            EnglishText = englishText;
            RussianText = russianText;
            Words = new Dictionary<string, string>();
        }
    }
} 