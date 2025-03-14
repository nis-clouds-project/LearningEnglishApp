namespace Frontend.Managers;

public class MenuFactory
{
    public static string GetCategoryEmoji(string? categoryName)
    {
        if (string.IsNullOrEmpty(categoryName))
            return "📚";

        return categoryName.ToLower() switch
        {
            "my words" => "📝",
            "common words" => "💬",
            "business" => "💼",
            "technology" => "💻",
            "travel" => "✈️",
            "education" => "📚",
            _ => "📚"
        };
    }
}