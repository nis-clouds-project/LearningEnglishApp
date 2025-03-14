using System.Text;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Frontend.Managers;

public static class VocabularyManager
{
    private static readonly ILogger Logger = LoggerFactory.Create(builder => builder.AddConsole())
        .CreateLogger("VocabularyManager");
    
    public static async Task HandleVocabularyCommand(long chatId, CancellationToken cancellationToken)
    {
        try
        {
            if (BotManager.ApiClient == null)
            {
                await BotManager.Bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Произошла внутренняя ошибка. Пожалуйста, попробуйте позже.",
                    cancellationToken: cancellationToken);
                return;
            }

            var categories = await BotManager.ApiClient.GetCategoriesAsync();
            var vocabulary = await BotManager.ApiClient.GetLearnedWordsAsync(chatId);
            
            if (vocabulary == null || !vocabulary.Any())
            {
                await BotManager.Bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: "У вас пока нет изученных слов. Используйте /start, чтобы начать изучение!",
                    cancellationToken: cancellationToken);
                return;
            }

            foreach (var word in vocabulary)
            {
                var category = categories?.FirstOrDefault(c => c.Id == word.CategoryId);
                if (category != null)
                {
                    word.CategoryName = category.Name;
                }
            }

            var message = new StringBuilder("📚 Ваш словарный запас:\n\n");
            
            var groupedWords = vocabulary
                .GroupBy(w => w.CategoryName ?? "Без категории")
                .OrderBy(g => g.Key);
            
            foreach (var group in groupedWords)
            {
                var categoryEmoji = UIHelper.GetCategoryEmoji(group.Key);
                message.AppendLine($"{categoryEmoji} *{group.Key}*:");
                foreach (var word in group.OrderBy(w => w.Text))
                {
                    message.AppendLine($"• {word.Text} - {word.Translation}");
                }
                message.AppendLine();
            }

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        text: "📚 Учить слова",
                        callbackData: "learn_menu"),
                    InlineKeyboardButton.WithCallbackData(
                        text: "🔙 В меню",
                        callbackData: "return_menu")
                }
            });

            await BotManager.Bot!.SendTextMessageAsync(
                chatId: chatId,
                text: message.ToString(),
                parseMode: ParseMode.Markdown,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in HandleVocabularyCommand for user {ChatId}", chatId);
            await BotManager.Bot!.SendTextMessageAsync(
                chatId: chatId,
                text: "Извините, произошла ошибка при получении словаря. Пожалуйста, попробуйте позже.",
                cancellationToken: cancellationToken);
        }
    }

    public static async Task StartAddWord(long chatId, CancellationToken cancellationToken)
    {
        UserStageManager.SetUserStage(chatId, UserStage.AddingWord);
        await BotManager.Bot!.SendTextMessageAsync(
            chatId: chatId,
            text: "Введите английское слово, которое хотите добавить:",
            cancellationToken: cancellationToken);
    }

    public static async Task HandleAddWordStep1(long chatId, string englishWord, CancellationToken cancellationToken)
    {
        UserStageManager.SetTempWord(chatId, englishWord);
        UserStageManager.SetUserStage(chatId, UserStage.AddingTranslation);
        var message = string.Format("Теперь введите перевод слова \"{0}\" на русский язык:", englishWord);
        await BotManager.Bot!.SendTextMessageAsync(
            chatId: chatId,
            text: message,
            cancellationToken: cancellationToken);
    }

    public static async Task HandleAddWordStep2(long chatId, string translation, CancellationToken cancellationToken)
    {
        try
        {
            var englishWord = UserStageManager.GetTempWord(chatId);
            if (string.IsNullOrEmpty(englishWord))
            {
                await BotManager.Bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Произошла ошибка. Пожалуйста, начните процесс добавления слова заново с команды /addword",
                    cancellationToken: cancellationToken);
                UserStageManager.ResetUserState(chatId);
                return;
            }

            Logger.LogInformation("Adding custom word '{Word}' with translation '{Translation}' for user {ChatId}", 
                englishWord, translation, chatId);

            try
            {
                var word = await BotManager.ApiClient!.AddCustomWordAsync(chatId, englishWord, translation);
                
                // Получаем категории для определения правильного ID категории "My Words"
                var categories = await BotManager.ApiClient.GetCategoriesAsync();
                var myWordsCategory = categories?.FirstOrDefault(c => c.Name?.Equals("My Words", StringComparison.OrdinalIgnoreCase) == true);
                
                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(
                            text: "📚 Учить слова в My Words",
                            callbackData: $"learn_{myWordsCategory?.Id ?? 0}"),
                        InlineKeyboardButton.WithCallbackData(
                            text: "📝 Добавить ещё",
                            callbackData: "add_word")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(
                            text: "🔙 В меню",
                            callbackData: "return_menu")
                    }
                });

                var successMessage = $"✅ Слово \"{englishWord}\" успешно добавлено в категорию \"My Words\"!\n\nЧто делаем дальше?";
                await BotManager.Bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: successMessage,
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken);
                
                Logger.LogInformation("Successfully added word {Word} for user {ChatId}", englishWord, chatId);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error while adding word {Word} for user {ChatId}", englishWord, chatId);
                await BotManager.Bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: "❌ Произошла ошибка при добавлении слова. Пожалуйста, попробуйте позже.",
                    cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in HandleAddWordStep2 for user {ChatId}", chatId);
            await BotManager.Bot!.SendTextMessageAsync(
                chatId: chatId,
                text: "Произошла ошибка при добавлении слова. Пожалуйста, попробуйте позже.",
                cancellationToken: cancellationToken);
        }
        finally
        {
            UserStageManager.ResetUserState(chatId);
        }
    }
    
    private static async Task HandleDeleteMyWord(long chatId, long wordId, CancellationToken cancellationToken)
    {
        try
        {
            if (_apiClient == null)
            {
                await _bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Произошла внутренняя ошибка. Пожалуйста, попробуйте позже.",
                    cancellationToken: cancellationToken);
                return;
            }
 
            var success = await _apiClient.DeleteCustomWord(chatId, wordId);
 
            if (success)
            {
                await _bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: "✅ Слово успешно удалено.",
                    cancellationToken: cancellationToken);
            }
            else
            {
                await _bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: "❌ Не удалось удалить слово. Пожалуйста, попробуйте позже.",
                    cancellationToken: cancellationToken);
            }
 
            await HandleShowMyWords(chatId, cancellationToken);
        }
        catch (Exception ex)
        {
            await _bot!.SendTextMessageAsync(
                chatId: chatId,
                text: "Произошла ошибка при удалении слова. Пожалуйста, попробуйте позже.",
                cancellationToken: cancellationToken);
        }
    }

}