namespace Backend.Controllers.Responses
{
    public class CustomWordRequest
    {
        public long UserId { get; set; }
        public string Text { get; set; } = string.Empty;
        public string Translation { get; set; } = string.Empty;
    }
}