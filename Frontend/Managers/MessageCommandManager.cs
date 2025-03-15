using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Frontend.Managers;

public static class MessageCommandManager
{
    public static async Task HandleStartCommand(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;
        try
        {
                
            var userExists = await BotManager.ApiClient!.UserExistsAsync(chatId);
            if (!userExists)
            {
                var user = await BotManager.ApiClient.AddUserAsync(chatId);
                if (user == null)
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "❌ Произошла ошибка при регистрации. Пожалуйста, попробуйте позже.",
                        cancellationToken: cancellationToken);
                    return;
                }
                    
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "👋 Добро пожаловать! Вы успешно зарегистрированы.",
                    cancellationToken: cancellationToken);
            }

            await ShowMainMenu(botClient, chatId, cancellationToken);
        }
        catch (Exception ex)
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Произошла ошибка. Пожалуйста, попробуйте позже.",
                cancellationToken: cancellationToken);
        }
    }
    
    public static async Task ShowHelp(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                var helpMessage = "📖 Справка по использованию бота:\n\n" +
                                  "Основные команды:\n" +
                                  "👋 /start - Начать работу с ботом\n" +
                                  "📚 /learn - Начать изучение слов\n" +
                                  "📝 /addword - Добавить своё слово\n" +
                                  "📖 /vocabulary - Посмотреть изученные слова\n" +
                                  "📝 /mywords - Посмотреть все слова из категории \"My Words\"\n" +
                                  "📚 /practise - Практика перевода слов\n" +
                                  "📝 /mywords - Посмотреть все слова из категории \"My Words\"\n" + 
                                  "✍️ /translate - Перевод слова \n" +
                                  "✍️ /generate - Сгенерировать текст из изученных слов\n" +
                                  "❓ /help - Показать эту справку\n\n" +
                                  "Как учить слова:\n" +
                                  "1. Выберите категорию через команду /learn\n" +
                                  "2. Бот будет показывать вам слова\n" +
                                  "3. Отмечайте известные вам слова\n\n" +
                                  "Как добавить своё слово:\n" +
                                  "1. Используйте команду /addword\n" +
                                  "2. Введите английское слово\n" +
                                  "3. Введите русский перевод\n\n" +
                                  "Советы:\n" +
                                  "- Регулярно повторяйте изученные слова\n" +
                                  "- Используйте слова в контексте\n" +
                                  "- Учите понемногу, но каждый день";

                await BotManager.Bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: helpMessage,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await BotManager.Bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Произошла ошибка при загрузке справки. Пожалуйста, попробуйте позже.",
                    cancellationToken: cancellationToken);
            }
        }
    
    public static async Task ShowMainMenu(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
    {
        var mainMenuMessage = "👋 Добро пожаловать в бот для изучения английских слов!\n\n" +
                              "Доступные команды:\n" +
                              "📚 /learn - Начать изучение слов\n" +
                              "📝 /addword - Добавить своё слово\n" +
                              "📖 /vocabulary - Посмотреть изученные слова\n" +
                              "📝 /mywords - Посмотреть все слова из категории \"My Words\"\n" +
                              "📚 /practise - Практика перевода слов\n" +
                              "📝 /mywords - Посмотреть все слова из категории \"My Words\"\n" + 
                              "✍️ /translate - Перевод слова \n" +
                              "✍️ /generate - Сгенерировать текст из изученных слов\n" +
                              "❓ /help - Показать эту справку\n\n" +
                              "Выберите действие:";

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(text: "📚 Учить слова", callbackData: "learn_menu"),
                InlineKeyboardButton.WithCallbackData(text: "📝 Добавить слово", callbackData: "add_word")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(text: "📖 Изученные слова", callbackData: "show_vocabulary"),
                InlineKeyboardButton.WithCallbackData(text: "📝 Мои слова", callbackData: "show_my_words")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(text: "✍️ Генерировать текст", callbackData: "generate_text"),
                InlineKeyboardButton.WithCallbackData(text: "📚 Практика", callbackData: "practise_menu")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(text: "📖 Переводчик", callbackData: "translation_menu"),
            }
        });

        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: mainMenuMessage,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);
    }
}