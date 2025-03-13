using System.Text.Json.Serialization;

namespace Frontend.Models
{
    public class Word
    {
        public long Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public string Translation { get; set; } = string.Empty;
        
        [JsonPropertyName("category_id")]
        public long CategoryId { get; set; }
        
        [JsonPropertyName("category")]
        public string CategoryName { get; set; } = string.Empty;
        
        [JsonPropertyName("user_id")]
        public long UserId { get; set; }
        
        [JsonPropertyName("is_custom")]
        public bool IsCustom { get; set; }
        
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }
        
        [JsonIgnore]
        public Category? Category => new Category { Id = CategoryId, Name = CategoryName };
    }
} 