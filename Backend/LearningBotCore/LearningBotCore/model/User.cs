namespace LearningBotCore.model;

/// <summary>
/// Класс, представляющий пользователя.
/// Содержит информацию о пользователе, включая идентификатор, списки изученных и просмотренных слов.
/// </summary>
public class User(long id)
{
    /// <summary>
    /// Уникальный идентификатор пользователя.
    /// </summary>
    public long Id { get; } = id;

    /// <summary>
    /// Список идентификаторов слов, которые пользователь выучил.
    /// </summary>
    public List<int> LearnedWordIds { get; } = [];

    /// <summary>
    /// Список идентификаторов слов, которые пользователь просмотрел.
    /// </summary>
    public List<int> ViewedWordsWordIds { get; } = [];
    
    public UserAiUsage UserAiUsage { get; set; } = new UserAiUsage();
}