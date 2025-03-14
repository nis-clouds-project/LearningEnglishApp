using System.Text;
using Frontend.Models;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Frontend.Managers;

public static class LearningManager
{
    private static readonly Dictionary<long, GeneratedTextResponse> _generatedTexts = new();
    
    public static async Task ShowCategories(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            if (BotManager.ApiClient == null)
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Произошла внутренняя ошибка. Пожалуйста, попробуйте позже.",
                    cancellationToken: cancellationToken);
                return;
            }

            var categories = await BotManager.ApiClient.GetCategoriesAsync();
            
            if (categories == null || !categories.Any())
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "К сожалению, не удалось загрузить категории. Попробуйте позже.",
                    cancellationToken: cancellationToken);
                return;
            }

            var buttons = new List<InlineKeyboardButton[]>();

            for (int i = 0; i < categories.Count; i += 2)
            {
                var rowButtons = new List<InlineKeyboardButton>();
                
                var firstCategoryName = categories[i].Name ?? "Unknown";
                var firstEmoji = MenuFactory.GetCategoryEmoji(firstCategoryName);
                var firstButtonText = string.Format("{0} {1}", firstEmoji, firstCategoryName);
                var firstCallbackData = string.Format("learn_{0}", categories[i].Id);
                
                rowButtons.Add(InlineKeyboardButton.WithCallbackData(
                    text: firstButtonText,
                    callbackData: firstCallbackData));
                
                if (i + 1 < categories.Count)
                {
                    var secondCategoryName = categories[i + 1].Name ?? "Unknown";
                    var secondEmoji = MenuFactory.GetCategoryEmoji(secondCategoryName);
                    var secondButtonText = string.Format("{0} {1}", secondEmoji, secondCategoryName);
                    var secondCallbackData = string.Format("learn_{0}", categories[i + 1].Id);
                    
                    rowButtons.Add(InlineKeyboardButton.WithCallbackData(
                        text: secondButtonText,
                        callbackData: secondCallbackData));
                }
                
                buttons.Add(rowButtons.ToArray());
            }

            buttons.Add(new[] 
            { 
                InlineKeyboardButton.WithCallbackData(
                    text: "📚 Все категории",
                    callbackData: "learn_all") 
            });

            buttons.Add(new[] 
            { 
                InlineKeyboardButton.WithCallbackData(
                    text: "🔙 Вернуться в меню",
                    callbackData: "return_menu") 
            });

            var keyboard = new InlineKeyboardMarkup(buttons);

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "📚 Выберите категорию для изучения:",
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Произошла ошибка при загрузке категорий. Пожалуйста, попробуйте позже.",
                cancellationToken: cancellationToken);
        }
    }
    
    public static async Task HandleCategoryLearning(long chatId, string categoryId, CancellationToken cancellationToken)
    {
        try
        {
            long? parsedCategoryId = categoryId == "all" ? null : long.Parse(categoryId);
            
            var categories = await BotManager.ApiClient!.GetCategoriesAsync();
            var category = categories?.FirstOrDefault(c => c.Id == parsedCategoryId);
            
            UserStageManager.SetUserCurrentCategory(chatId, parsedCategoryId);
            
            var isMyWordsCategory = category?.Name?.Equals("My Words", StringComparison.OrdinalIgnoreCase) == true;

            try
            {
                Word? word;
                try {
                    if (isMyWordsCategory) {
                        try {
                            word = await BotManager.ApiClient!.GetRandomCustomWordAsync(chatId);
                            if (word != null)
                            {
                                var wordCategory = categories?.FirstOrDefault(c => c.Id == word.CategoryId);
                                if (wordCategory != null)
                                {
                                    word.CategoryName = wordCategory.Name;
                                }
                            }
                        } 
                        catch (System.Text.Json.JsonException jsonEx) {
                            throw;
                        }
                    } else {
                        word = await BotManager.ApiClient!.GetRandomWordAsync(chatId, parsedCategoryId);
                        if (word != null)
                        {
                            var wordCategory = categories?.FirstOrDefault(c => c.Id == word.CategoryId);
                            if (wordCategory != null)
                            {
                                word.CategoryName = wordCategory.Name;
                            }
                        }
                    }
                } 
                catch (Exception ex) 
                {
                    throw;
                }
                
                if (word == null)
                {
                    var message = isMyWordsCategory
                        ? "В категории \"My Words\" пока нет слов для изучения. Возможно, вы уже выучили все добавленные слова или еще не добавили ни одного слова.\n\n" +
                          "Используйте команду /addword или кнопку \"📝 Добавить слово\", чтобы добавить новые слова."
                        : "😔 К сожалению, не удалось получить слово для изучения. Попробуйте другую категорию или повторите попытку позже.";
                    
                    await BotManager.Bot!.SendTextMessageAsync(
                        chatId: chatId,
                        text: message,
                        cancellationToken: cancellationToken);

                    var noWordsKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        isMyWordsCategory
                            ? new[]
                            {
                                InlineKeyboardButton.WithCallbackData(
                                    text: "📝 Добавить слово",
                                    callbackData: "add_word"),
                                InlineKeyboardButton.WithCallbackData(
                                    text: "🔙 К категориям",
                                    callbackData: "learn_menu")
                            }
                            : new[]
                            {
                                InlineKeyboardButton.WithCallbackData(
                                    text: "🔄 Другая категория",
                                    callbackData: "learn_menu"),
                                InlineKeyboardButton.WithCallbackData(
                                    text: "🔙 В меню",
                                    callbackData: "return_menu")
                            }
                    });

                    await BotManager.Bot!.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Выберите действие:",
                        replyMarkup: noWordsKeyboard,
                        cancellationToken: cancellationToken);
                    return;
                }
                
                string wordCategoryName = word.CategoryName;
                string emojiForCategory = MenuFactory.GetCategoryEmoji(wordCategoryName);
                string messageText = string.Format("📝 Новое слово для изучения:\n\n🇬🇧 {0}\n📚 Категория: {1} {2}",
                    word.Text, emojiForCategory, wordCategoryName);

                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(
                            text: "✅ Знаю это слово",
                            callbackData: string.Format("known_{0}", word.Id)),
                            InlineKeyboardButton.WithCallbackData(
                                text: "❓ Показать перевод",
                                callbackData: string.Format("show_translation_{0}", word.Id))
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(
                            text: "➡️ Следующее слово",
                            callbackData: string.Format("next_{0}", categoryId)),
                            InlineKeyboardButton.WithCallbackData(
                                text: "🔙 К категориям",
                                callbackData: "learn_menu")
                    }
                });

                await BotManager.Bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: messageText,
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        catch (Exception ex)
        {
            await BotManager.Bot!.SendTextMessageAsync(
                chatId: chatId,
                text: "Произошла ошибка при получении слова. Пожалуйста, попробуйте позже.",
                cancellationToken: cancellationToken);
        }
    }
    
    public static async Task HandleShowTranslation(long chatId, int wordId, CancellationToken cancellationToken)
    {
        try
        {
            if (BotManager.ApiClient == null)
            {
                throw new InvalidOperationException("BotManager.ApiClient is not initialized");
            }
            
            var categories = await BotManager.ApiClient.GetCategoriesAsync();
            var word = await BotManager.ApiClient.GetWordByIdAsync(wordId);
            if (word == null)
            {
                await BotManager.Bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: "❌ Извините, не удалось найти это слово. Попробуйте другое.",
                    cancellationToken: cancellationToken);
                return;
            }

            var wordCategory = categories?.FirstOrDefault(c => c.Id == word.CategoryId);
            if (wordCategory != null)
            {
                word.CategoryName = wordCategory.Name;
            }

            var currentCategory = UserStageManager.GetUserCurrentCategory(chatId);
            var categoryForButton = currentCategory?.ToString() ?? "all";

            var message = string.Format("📝 Слово:\n\n🇬🇧 {0}\n🇷🇺 {1}\n📚 Категория: {2} {3}",
                word.Text,
                word.Translation,
                MenuFactory.GetCategoryEmoji(word.CategoryName),
                word.CategoryName);

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        text: "✅ Знаю это слово",
                        callbackData: string.Format("known_{0}", word.Id)),
                        InlineKeyboardButton.WithCallbackData(
                            text: "➡️ Следующее слово",
                            callbackData: string.Format("next_{0}", categoryForButton))
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        text: "🔙 К категориям",
                        callbackData: "return_menu")
                }
            });

            await BotManager.Bot!.SendTextMessageAsync(
                chatId: chatId,
                text: message,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            await BotManager.Bot!.SendTextMessageAsync(
                chatId: chatId,
                text: "Произошла ошибка при получении перевода. Пожалуйста, попробуйте позже.",
                cancellationToken: cancellationToken);
        }
    }
    
    public static async Task HandleKnownWord(long chatId, int wordId, CancellationToken cancellationToken)
    {
        try
        {
            if (BotManager.ApiClient == null)
            {
                throw new InvalidOperationException("BotManager.ApiClient is not initialized");
            }

            var currentCategory = UserStageManager.GetUserCurrentCategory(chatId);
            
            var categories = await BotManager.ApiClient.GetCategoriesAsync();
            var category = categories?.FirstOrDefault(c => c.Id == currentCategory);
            var isMyWordsCategory = category?.Name?.Equals("My Words", StringComparison.OrdinalIgnoreCase) == true;
            
            var success = await BotManager.ApiClient.AddWordToVocabularyAsync(chatId, wordId);
            
            if (!success)
            {
                await BotManager.Bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: "❌ Не удалось добавить слово в словарь. Попробуйте позже.",
                    cancellationToken: cancellationToken);
                return;
            }

            await BotManager.Bot!.SendTextMessageAsync(
                chatId: chatId,
                text: "✅ Отлично! Слово добавлено в ваш словарь.",
                cancellationToken: cancellationToken);

            var nextWord = isMyWordsCategory
                ? await BotManager.ApiClient.GetRandomCustomWordAsync(chatId)
                : await BotManager.ApiClient.GetRandomWordAsync(chatId, currentCategory);

            if (nextWord != null)
            {
                var wordCategory = categories?.FirstOrDefault(c => c.Id == nextWord.CategoryId);
                if (wordCategory != null)
                {
                    nextWord.Category.Name = wordCategory.Name;
                }
            }
            
            if (nextWord == null)
            {
                var message = isMyWordsCategory
                    ? "🎉 Поздравляем! Вы изучили все добавленные вами слова. Используйте команду /addword, чтобы добавить новые слова."
                    : "🎉 Поздравляем! Вы изучили все слова в этой категории. Используйте /learn для выбора другой категории.";
                
                await BotManager.Bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: message,
                    cancellationToken: cancellationToken);
                return;
            }

            var messageText = string.Format("📝 Новое слово для изучения:\n\n🇬🇧 {0}\n📚 Категория: {1} {2}",
                nextWord.Text,
                MenuFactory.GetCategoryEmoji(nextWord.CategoryName),
                nextWord.CategoryName);

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        text: "✅ Знаю это слово",
                        callbackData: string.Format("known_{0}", nextWord.Id)),
                        InlineKeyboardButton.WithCallbackData(
                            text: "❓ Показать перевод",
                            callbackData: string.Format("show_translation_{0}", nextWord.Id))
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        text: "➡️ Следующее слово",
                        callbackData: string.Format("next_{0}", currentCategory)),
                        InlineKeyboardButton.WithCallbackData(
                            text: "🔙 К категориям",
                            callbackData: "learn_menu")
                }
            });

            await BotManager.Bot!.SendTextMessageAsync(
                chatId: chatId,
                text: messageText,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            await BotManager.Bot!.SendTextMessageAsync(
                chatId: chatId,
                text: "Произошла ошибка при обработке слова. Пожалуйста, попробуйте позже.",
                cancellationToken: cancellationToken);
        }
    }
    
    public static async Task HandleGenerateCommand(long chatId, CancellationToken cancellationToken)
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

            await BotManager.Bot!.SendTextMessageAsync(
                chatId: chatId,
                text: "🎨 Генерирую текст на основе ваших изученных слов...",
                cancellationToken: cancellationToken);

            var generatedText = await BotManager.ApiClient.GenerateTextFromVocabularyAsync(chatId);
            if (generatedText == null)
            {
                await BotManager.Bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: "📚 Извините, не удалось сгенерировать текст. Возможно, в вашем словаре пока недостаточно слов.\n\n" +
                          "Попробуйте изучить больше слов с помощью команды /learn или добавьте свои слова через /addword.",
                    cancellationToken: cancellationToken);
                return;
            }
            
            _generatedTexts[chatId] = generatedText; 

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        text: "🔄 Сгенерировать новый текст",
                        callbackData: "generate_text"),
                    InlineKeyboardButton.WithCallbackData(
                        text: "📚 Учить слова",
                        callbackData: "learn_menu")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        text: "🔙 В меню",
                        callbackData: "return_menu")
                }
            });

            var message = new StringBuilder();
            message.AppendLine("✨ *Сгенерированный текст на основе ваших слов*\n");
            message.AppendLine("🇬🇧 *Английский текст:*");
            message.AppendLine($"_{generatedText.EnglishText}_\n");
            message.AppendLine("🇷🇺 *Русский перевод:*");
            message.AppendLine($"_{generatedText.RussianText}_\n");
            message.AppendLine("📝 *Использованные слова:*");
            
            var wordsList = generatedText.Words
                .OrderBy(w => w.Key)
                .Select(w => $"• {w.Key} - {w.Value}")
                .ToList();

            foreach (var word in wordsList)
            {
                message.AppendLine(word);
            }

            message.AppendLine("\n💡 _Совет: Попробуйте использовать эти слова в своих предложениях для лучшего запоминания!_");

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
                text: "❌ Извините, произошла ошибка при генерации текста. Пожалуйста, попробуйте позже.",
                cancellationToken: cancellationToken);
        }
    }
    
    public static async Task HandleDeleteMyWord(long chatId, long wordId, CancellationToken cancellationToken)
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

            var success = await BotManager.ApiClient.DeleteCustomWord(chatId, wordId);

            if (success)
            {
                await BotManager.Bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: "✅ Слово успешно удалено.",
                    cancellationToken: cancellationToken);
            }
            else
            {
                await BotManager.Bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: "❌ Не удалось удалить слово. Пожалуйста, попробуйте позже.",
                    cancellationToken: cancellationToken);
            }

            await VocabularyManager.HandleShowMyWords(chatId, cancellationToken);
        }
        catch (Exception ex)
        {
            await BotManager.Bot!.SendTextMessageAsync(
                chatId: chatId,
                text: "Произошла ошибка при удалении слова. Пожалуйста, попробуйте позже.",
                cancellationToken: cancellationToken);
        }
    }
}