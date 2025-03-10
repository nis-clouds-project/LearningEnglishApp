namespace Backend.Models;

/// <summary>
/// Перечисление, представляющее различные категории слов для изучения.
/// </summary>
public class CategoryType
{
    public const string Food = "Food";
    public const string Technology = "Technology";
    public const string Business = "Business";
    public const string Travel = "Travel";
    public const string Health = "Health";
    public const string Education = "Education";
    public const string Entertainment = "Entertainment";
    public const string Sports = "Sports";

    public static readonly string[] AllCategories = {
        Food,
        Technology,
        Business,
        Travel,
        Health,
        Education,
        Entertainment,
        Sports
    };
}