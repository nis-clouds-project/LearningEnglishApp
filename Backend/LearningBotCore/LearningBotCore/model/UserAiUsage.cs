namespace LearningBotCore.model;

using System;

public class UserAiUsage
{
    /// <summary>
    /// Количество обращений пользователя к искусственному интеллекту.
    /// </summary>
    public int AiRequestCount { get; private set; }

    /// <summary>
    /// Дата последнего обращения к ИИ.
    /// </summary>
    public DateTime LastRequestDate { get; private set; }

    /// <summary>
    /// Максимальное количество запросов в день.
    /// </summary>
    private const int DailyRequestLimit = 1;

    /// <summary>
    /// Проверяет, можно ли выполнить запрос к ИИ.
    /// </summary>
    /// <returns>True, если запрос разрешен, иначе — False.</returns>
    public bool CanMakeRequest()
    {
        var currentDate = DateTime.UtcNow.Date;

        // Если дата изменилась, сбрасываем счетчик.
        if (LastRequestDate != currentDate)
        {
            AiRequestCount = 0;
            LastRequestDate = currentDate;
        }

        // Проверяем, не превышен ли лимит запросов.
        return AiRequestCount < DailyRequestLimit;
    }

    /// <summary>
    /// Увеличивает счетчик запросов к ИИ.
    /// </summary>
    public void IncrementRequestCount()
    {
        if (CanMakeRequest())
        {
            AiRequestCount++;
        }
        else
        {
            throw new InvalidOperationException("Достигнут дневной лимит запросов к ИИ.");
        }
    }
}