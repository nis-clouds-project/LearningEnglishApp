using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

/// <summary>
/// Модель категории слов
/// </summary>
public class Category
{
    /// <summary>
    /// Уникальный идентификатор категории
    /// </summary>
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// Название категории
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    public Category()
    {
    }

    public Category(string name)
    {
        Name = name;
    }
} 