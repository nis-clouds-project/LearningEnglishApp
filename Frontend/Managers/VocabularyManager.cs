using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Frontend.Managers;

public static class VocabularyManager
{
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
                var categoryEmoji = MenuFactory.GetCategoryEmoji(group.Key);
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
            
            try
            {
                var word = await BotManager.ApiClient!.AddCustomWordAsync(chatId, englishWord, translation);
                
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
                
            }
            catch (Exception ex)
            {
                await BotManager.Bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: "❌ Произошла ошибка при добавлении слова. Пожалуйста, попробуйте позже.",
                    cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
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
    
    public static async Task HandleShowMyWords(long chatId, CancellationToken cancellationToken)
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
            var myWordsCategory = categories?.FirstOrDefault(c => c.Name?.Equals("My Words", StringComparison.OrdinalIgnoreCase) == true);
            Console.WriteLine(myWordsCategory);
            
            if (myWordsCategory == null)
            {
                await BotManager.Bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Категория \"My Words\" не найдена.",
                    cancellationToken: cancellationToken);
                return;
            }

            var vocabulary = await BotManager.ApiClient.GetAllCustomWordsAsync(chatId);
            Console.WriteLine(vocabulary.Count);
            
            if (vocabulary == null || !vocabulary.Any())
            {
                Console.WriteLine("Тут");
                await BotManager.Bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: "У вас пока нет слов в категории \"My Words\".",
                    cancellationToken: cancellationToken);
                return;
            }

            var myWords = vocabulary
                .Where(word => word.CategoryId == myWordsCategory.Id)
                .ToList();
            
            Console.WriteLine(myWords.Count);
            if (!myWords.Any())
            {
                await BotManager.Bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: "У вас пока нет слов в категории \"My Words\".",
                    cancellationToken: cancellationToken);
                return;
            }

            var message = new StringBuilder("📝 *Ваши слова в категории \"My Words\":*\n\n");

            foreach (var word in myWords)
            {
                message.AppendLine($"• {word.Text} - {word.Translation}");
            }

            var buttons = new List<InlineKeyboardButton[]>();
            foreach (var word in myWords)
            {
                buttons.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        text: $"❌ Удалить \"{word.Text}\"",
                        callbackData: $"delete_myword_{word.Id}")
                });
            }

            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(text: "📚 Учить слова", callbackData: "learn_menu"),
                InlineKeyboardButton.WithCallbackData(text: "🔙 В меню", callbackData: "return_menu")
            });

            var keyboard = new InlineKeyboardMarkup(buttons);

            await BotManager.Bot!.SendTextMessageAsync(
                chatId: chatId,
                text: message.ToString(),
                parseMode: ParseMode.Markdown,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            await BotManager.Bot!.SendTextMessageAsync(
                chatId: chatId,
                text: "Произошла ошибка при получении слов. Пожалуйста, попробуйте позже.",
                cancellationToken: cancellationToken);
        }
    }
}