using IntegrationWithTelegram.models;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace IntegrationWithTelegram.managers;

/// <summary>
/// Менеджер для создания и отправки клавиатур с кнопками пользователю.
/// </summary>
public static class ButtonPanelManager
{
    /// <summary>
    /// Отправляет клавиатуру с основными действиями пользователю.
    /// </summary>
    /// <param name="chatId">ID чата пользователя.</param>
    /// <param name="botClient">Клиент бота.</param>
    public static async Task SendButtonsToGetAction(long chatId, ITelegramBotClient botClient)
    {
        // Создаем кнопки для основных действий.
        var buttons = new List<KeyboardButton>
        {
            new KeyboardButton("📚 Учить лексику"),
            new KeyboardButton("➕ Добавить новое слово в словарь"),
            new KeyboardButton("🔤 Практика перевода"),
            new KeyboardButton("🔄 Повторить выученные слова")
        };

        // Создаем клавиатуру с кнопками.
        var keyboard = new ReplyKeyboardMarkup(buttons)
        {
            ResizeKeyboard = true, // Кнопки автоматически подстраиваются под размер экрана.
            OneTimeKeyboard = true // Клавиатура скрывается после выбора.
        };

        // Устанавливаем состояние пользователя в "Ожидание запроса".
        UserStateManager.SetState(chatId, State.WaitingRequest);

        // Отправляем сообщение с клавиатурой.
        await botClient.SendMessage(chatId, "Выберите действие:", replyMarkup: keyboard);
    }

    /// <summary>
    /// Отправляет клавиатуру с категориями слов пользователю.
    /// Кнопки распределяются по 4 в строке.
    /// </summary>
    /// <param name="chatId">ID чата пользователя.</param>
    /// <param name="botClient">Клиент бота.</param>
    public static async Task SendButtonsToGetCategory(long chatId, ITelegramBotClient botClient)
    {
        // Получаем все значения перечисления CategoryType.
        var categories = Enum.GetValues(typeof(CategoryType))
            .Cast<CategoryType>()
            .Select(category => new KeyboardButton(category.ToString()))
            .ToList();

        // Добавляем кнопку "Закончить".
        categories.Add(new KeyboardButton("Закончить"));

        // Разбиваем кнопки на строки по 4 кнопки в каждой.
        var buttonRows = categories
            .Select((button, index) => new { button, index })
            .GroupBy(x => x.index / 4) // Группируем по 4 кнопки в строке.
            .Select(group => group.Select(x => x.button).ToList())
            .ToList();

        // Создаем клавиатуру с кнопками.
        var keyboard = new ReplyKeyboardMarkup(buttonRows)
        {
            ResizeKeyboard = true, // Кнопки автоматически подстраиваются под размер экрана.
            OneTimeKeyboard = true // Клавиатура скрывается после выбора.
        };

        // Отправляем сообщение с клавиатурой.
        await botClient.SendMessage(chatId, "Выберите категорию:", replyMarkup: keyboard);
    }

    /// <summary>
    /// Отправляет клавиатуру с действиями для добавления слова в словарь.
    /// </summary>
    /// <param name="chatId">ID чата пользователя.</param>
    /// <param name="botClient">Клиент бота.</param>
    public static async Task SendButtonsAboutAddingWordInVocabulary(long chatId, ITelegramBotClient botClient)
    {
        // Устанавливаем состояние пользователя в "Запрос следующего слова".
        UserStateManager.SetState(chatId, State.LearningWordsAskNext);

        // Создаем кнопки для действий.
        var buttons = new List<KeyboardButton>
        {
            new KeyboardButton("Не знаю"),
            new KeyboardButton("Знаю"),
            new KeyboardButton("Закончить")
        };

        // Создаем клавиатуру с кнопками.
        var keyboard = new ReplyKeyboardMarkup(buttons)
        {
            ResizeKeyboard = true, // Кнопки автоматически подстраиваются под размер экрана.
            OneTimeKeyboard = true // Клавиатура скрывается после выбора.
        };

        // Отправляем сообщение с клавиатурой.
        await botClient.SendMessage(chatId, "Выберите действие:", replyMarkup: keyboard);
    }
}