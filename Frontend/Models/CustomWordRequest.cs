using System.Text.Json.Serialization;

namespace Frontend.Models
{
    public class CustomWordRequest
    {
        [JsonPropertyName("userId")]
        public long UserId { get; set; }
        
        [JsonPropertyName("text")]
        public string? Text { get; set; }
        
        [JsonPropertyName("translation")]
        public string? Translation { get; set; }
    }
} 