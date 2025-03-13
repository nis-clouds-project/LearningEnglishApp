using Frontend.Services;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Text;
using Frontend.Models;

namespace Frontend.Managers
{
    /// <summary>
    /// Менеджер для управления ботом.
    /// Отвечает за инициализацию бота, обработку входящих сообщений и управление состоянием пользователя.
    /// </summary>
    public static class BotManager
    {
        private static TelegramBotClient? _bot;
        private static ApiClient? _apiClient;
        private static CancellationTokenSource? _cts;
        private static readonly Dictionary<long, GeneratedTextResponse> _generatedTexts = new();

        /// <summary>
        /// Запускает бота и начинает обработку входящих сообщений.
        /// </summary>
        public static async Task StartAsync()
        {
            var token = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN") 
                       ?? throw new InvalidOperationException("TELEGRAM_BOT_TOKEN не задан");
            var baseUrl = Environment.GetEnvironmentVariable("BACKEND_API_URL") 
                       ?? throw new InvalidOperationException("BACKEND_API_URL не задан");
            
            
            _bot = new TelegramBotClient(token);
            _apiClient = new ApiClient(baseUrl);
            _cts = new CancellationTokenSource();

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>(),
                ThrowPendingUpdates = true
            };

            try
            {
                var me = await _bot.GetMeAsync(_cts.Token);

                _bot.StartReceiving(
                    updateHandler: HandleUpdateAsync,
                    pollingErrorHandler: HandleErrorAsync,
                    receiverOptions: receiverOptions,
                    cancellationToken: _cts.Token
                );

                while (!_cts.Token.IsCancellationRequested)
                {
                    await Task.Delay(1000, _cts.Token);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при запуске бота: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Останавливает бота.
        /// </summary>
        public static void Stop()
        {
            _cts?.Cancel();
            _apiClient?.Dispose();
        }

        private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                if (update.CallbackQuery is { } callback)
                {
                    await HandleCallbackAsync(callback, cancellationToken);
                    return;
                }

                if (update.Message is not { } message)
                    return;
                if (message.Text is not { } messageText)
                    return;

                var chatId = message.Chat.Id;

                var currentStage = UserStageManager.GetUserStage(chatId);

                switch (messageText.ToLower())
                {
                    case "/start":
                        UserStageManager.ResetUserState(chatId);
                        await HandleStartCommand(chatId, cancellationToken);
                        break;
                    case "/learn":
                        UserStageManager.SetUserStage(chatId, UserStage.ChoosingCategory);
                        await ShowCategories(chatId, cancellationToken);
                        break;
                    case "/help":
                        await ShowHelp(chatId, cancellationToken);
                        break;
                    case "/categories":
                        UserStageManager.SetUserStage(chatId, UserStage.ChoosingCategory);
                        await ShowCategories(chatId, cancellationToken);
                        break;
                    case "/generate":
                        UserStageManager.SetUserStage(chatId, UserStage.GeneratingText);
                        await HandleGenerateCommand(chatId, cancellationToken);
                        break;
                    case "/vocabulary":
                        UserStageManager.SetUserStage(chatId, UserStage.ViewingVocabulary);
                        await HandleVocabularyCommand(chatId, cancellationToken);
                        break;
                    default:
                        await _bot!.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Извините, я не понимаю эту команду. Используйте /help для списка доступных команд.",
                            cancellationToken: cancellationToken);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обработке сообщения: {ex}");
            }
        }

        private static async Task HandleCallbackAsync(CallbackQuery callback, CancellationToken cancellationToken)
        {
            var chatId = callback.Message?.Chat.Id;
            if (chatId == null) return;

            var data = callback.Data;
            if (string.IsNullOrEmpty(data)) return;

            try
            {
                var currentStage = UserStageManager.GetUserStage(chatId.Value);

                switch (data)
                {
                    case var s when s.StartsWith("learn_"):
                        var category = s[6..];
                        UserStageManager.SetUserStage(chatId.Value, UserStage.Learning);
                        UserStageManager.SetUserCurrentCategory(chatId.Value, category == "all" ? null : long.Parse(category));
                        await HandleCategoryLearning(chatId.Value, category, cancellationToken);
                        break;
                    case var s when s.StartsWith("known_"):
                        if (currentStage != UserStage.Learning)
                        {
                            UserStageManager.SetUserStage(chatId.Value, UserStage.Learning);
                        }
                        var idString = s[6..];
                        if (int.TryParse(idString, out var wordId))
                        {
                            await HandleKnownWord(chatId.Value, wordId, cancellationToken);
                        }
                        break;
                    case var s when s.StartsWith("show_translation_"):
                        if (currentStage != UserStage.Learning)
                        {
                            UserStageManager.SetUserStage(chatId.Value, UserStage.Learning);
                        }
                        var translationIdString = s["show_translation_".Length..];
                        if (int.TryParse(translationIdString, out var translationWordId))
                        {
                            await HandleShowTranslation(chatId.Value, translationWordId, cancellationToken);
                        }
                        break;
                    case var s when s.StartsWith("next_"):
                        if (currentStage != UserStage.Learning)
                        {
                            UserStageManager.SetUserStage(chatId.Value, UserStage.Learning);
                        }
                        var nextCategory = s[5..];
                        await HandleCategoryLearning(chatId.Value, nextCategory, cancellationToken);
                        break;
                    case "return_menu":
                        UserStageManager.ResetUserState(chatId.Value);
                        await HandleStartCommand(chatId.Value, cancellationToken);
                        break;
                    default:
                        Console.WriteLine($"Получен неизвестный тип callback данных: {data}");
                        break;
                }

                await _bot!.AnswerCallbackQueryAsync(
                    callbackQueryId: callback.Id,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await _bot!.AnswerCallbackQueryAsync(
                    callbackQueryId: callback.Id,
                    text: "Произошла ошибка. Пожалуйста, попробуйте еще раз.",
                    cancellationToken: cancellationToken);
                
                await _bot!.SendTextMessageAsync(
                    chatId: chatId.Value,
                    text: "Произошла ошибка при обработке действия. Пожалуйста, попробуйте еще раз или вернитесь в главное меню.",
                    cancellationToken: cancellationToken);
            }
        }

        private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private static async Task HandleStartCommand(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                var userExists = await _apiClient!.UserExistsAsync(chatId);
                if (!userExists)
                {
                    var success = await _apiClient.AddUserAsync(chatId);
                    if (!success)
                    {
                        await _bot!.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Произошла ошибка при регистрации. Пожалуйста, попробуйте позже.",
                            cancellationToken: cancellationToken);
                        return;
                    }
                }

                var welcomeMessage = "👋 Добро пожаловать в бот для изучения английского языка!\n\n" +
                                   "Доступные команды:\n" +
                                   "📚 /learn - Начать изучение слов\n" +
                                   "❓ /help - Показать справку\n" +
                                   "📚 /categories - показать доступные категории слов\n" +
                                   "📚 /generate - сгенерировать текст на основе изученных слов\n" +
                                   "📚 /vocabulary - Просмотреть изученные слова";

                await _bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: welcomeMessage,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await _bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Произошла ошибка. Пожалуйста, попробуйте позже.",
                    cancellationToken: cancellationToken);
            }
        }

        private static async Task ShowCategories(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                if (_apiClient == null)
                {
                    Console.WriteLine("[ShowCategories] _apiClient is null!");
                    await _bot!.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Произошла внутренняя ошибка. Пожалуйста, попробуйте позже.",
                        cancellationToken: cancellationToken);
                    return;
                }

                var categories = await _apiClient.GetCategoriesAsync();
                if (categories == null || !categories.Any())
                {
                    await _bot!.SendTextMessageAsync(
                        chatId: chatId,
                        text: "К сожалению, не удалось загрузить категории. Попробуйте позже.",
                        cancellationToken: cancellationToken);
                    return;
                }

                var buttons = categories.Select(category =>
                    new[] { InlineKeyboardButton.WithCallbackData(
                        text: GetCategoryEmoji(category.Name) + " " + category.Name,
                        callbackData: $"learn_{category.Id}") }
                ).ToList();

                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(
                    text: "📚 Все категории",
                    callbackData: "learn_all") });

                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(
                    text: "🔙 Вернуться в меню",
                    callbackData: "return_menu") });

                var keyboard = new InlineKeyboardMarkup(buttons);

                await _bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: "📚 Выберите категорию для изучения:",
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await _bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Произошла ошибка при загрузке категорий. Пожалуйста, попробуйте позже.",
                    cancellationToken: cancellationToken);
            }
        }

        private static string GetCategoryEmoji(string categoryName)
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

        private static async Task ShowHelp(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                var helpMessage = "📖 Справка по использованию бота:\n\n" +
                                 "Основные команды:\n" +
                                 "👋 /start - Начать работу с ботом\n" +
                                 "📚 /learn - Начать изучение слов\n" +
                                 "❓ /help - Показать эту справку\n\n" +
                                 "Как учить слова:\n" +
                                 "1. Выберите категорию через команду /learn\n" +
                                 "2. Бот будет показывать вам слова\n" +
                                 "3. Отмечайте известные вам слова\n\n" +
                                 "Советы:\n" +
                                 "- Регулярно повторяйте изученные слова\n" +
                                 "- Используйте слова в контексте\n" +
                                 "- Учите понемногу, но каждый день";

                await _bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: helpMessage,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await _bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Произошла ошибка при загрузке справки. Пожалуйста, попробуйте позже.",
                    cancellationToken: cancellationToken);
            }
        }

        private static async Task HandleCategoryLearning(long chatId, string categoryId, CancellationToken cancellationToken)
        {
            try
            {
                long? parsedCategoryId = categoryId == "all" ? null : long.Parse(categoryId);
                UserStageManager.SetUserCurrentCategory(chatId, parsedCategoryId);
                
                var word = await _apiClient!.GetRandomWordAsync(chatId, parsedCategoryId);
                if (word == null)
                {
                    await _bot!.SendTextMessageAsync(
                        chatId: chatId,
                        text: "😔 К сожалению, не удалось получить слово для изучения. Попробуйте другую категорию или повторите попытку позже.",
                        cancellationToken: cancellationToken);
                    return;
                }

                var message = $"📝 Новое слово для изучения:\n\n" +
                             $"🇬🇧 {word.Text}\n" +
                             $"📚 Категория: {GetCategoryEmoji(word.Category)} {word.Category}";

                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(
                            text: "✅ Знаю это слово",
                            callbackData: $"known_{word.Id}"),
                        InlineKeyboardButton.WithCallbackData(
                            text: "❓ Показать перевод",
                            callbackData: $"show_translation_{word.Id}")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(
                            text: "➡️ Следующее слово",
                            callbackData: $"next_{categoryId}"),
                        InlineKeyboardButton.WithCallbackData(
                            text: "🔙 Вернуться в меню",
                            callbackData: "return_menu")
                    }
                });

                await _bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: message,
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await _bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Произошла ошибка при получении слова. Пожалуйста, попробуйте позже.",
                    cancellationToken: cancellationToken);
            }
        }

        private static async Task HandleShowTranslation(long chatId, int wordId, CancellationToken cancellationToken)
        {
            try
            {
                
                if (_apiClient == null)
                {
                    throw new InvalidOperationException("ApiClient is not initialized");
                }
                
                var word = await _apiClient.GetWordByIdAsync(wordId);
                if (word == null)
                {
                    await _bot!.SendTextMessageAsync(
                        chatId: chatId,
                        text: "❌ Извините, не удалось найти это слово. Попробуйте другое.",
                        cancellationToken: cancellationToken);
                    return;
                }

                var currentCategory = UserStageManager.GetUserCurrentCategory(chatId);
                var categoryForButton = currentCategory?.ToString() ?? "all";

                var message = $"📝 Слово:\n\n" +
                             $"🇬🇧 {word.Text}\n" +
                             $"🇷🇺 {word.Translation}\n" +
                             $"📚 Категория: {GetCategoryEmoji(word.Category)} {word.Category}";

                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(
                            text: "✅ Знаю это слово",
                            callbackData: $"known_{word.Id}"),
                        InlineKeyboardButton.WithCallbackData(
                            text: "➡️ Следующее слово",
                            callbackData: $"next_{categoryForButton}")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(
                            text: "🔙 К категориям",
                            callbackData: "return_menu")
                    }
                });

                await _bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: message,
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                
                await _bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Произошла ошибка при получении перевода. Пожалуйста, попробуйте позже.",
                    cancellationToken: cancellationToken);
            }
        }

        private static async Task HandleKnownWord(long chatId, int wordId, CancellationToken cancellationToken)
        {
            try
            {
                if (_apiClient == null)
                {
                    Console.WriteLine("[HandleKnownWord] _apiClient is null!");
                    throw new InvalidOperationException("ApiClient is not initialized");
                }

                var currentCategory = UserStageManager.GetUserCurrentCategory(chatId);
                
                var success = await _apiClient.AddWordToVocabularyAsync(chatId, wordId);
                
                if (!success)
                {
                    await _bot!.SendTextMessageAsync(
                        chatId: chatId,
                        text: "❌ Не удалось добавить слово в словарь. Попробуйте позже.",
                        cancellationToken: cancellationToken);
                    return;
                }

                await _bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: "✅ Отлично! Слово добавлено в ваш словарь.",
                    cancellationToken: cancellationToken);

                var nextWord = await _apiClient.GetRandomWordAsync(chatId, currentCategory);
                
                if (nextWord == null)
                {
                    await _bot!.SendTextMessageAsync(
                        chatId: chatId,
                        text: "🎉 Поздравляем! Вы изучили все слова в этой категории. Используйте /learn для выбора другой категории.",
                        cancellationToken: cancellationToken);
                    return;
                }

                var message = $"📝 Новое слово для изучения:\n\n" +
                             $"🇬🇧 {nextWord.Text}\n" +
                             $"📚 Категория: {GetCategoryEmoji(nextWord.Category)} {nextWord.Category}";

                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(
                            text: "✅ Знаю это слово",
                            callbackData: $"known_{nextWord.Id}"),
                        InlineKeyboardButton.WithCallbackData(
                            text: "❓ Показать перевод",
                            callbackData: $"show_translation_{nextWord.Id}")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(
                            text: "➡️ Следующее слово",
                            callbackData: $"next_{currentCategory}"),
                        InlineKeyboardButton.WithCallbackData(
                            text: "🔙 Вернуться в меню",
                            callbackData: "return_menu")
                    }
                });

                await _bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: message,
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await _bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Произошла ошибка при обработке слова. Пожалуйста, попробуйте позже.",
                    cancellationToken: cancellationToken);
            }
        }

        private static async Task HandleGenerateCommand(long chatId, CancellationToken cancellationToken)
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

                var generatedText = await _apiClient.GenerateTextFromVocabularyAsync(chatId);
                if (generatedText == null)
                {
                    await _bot!.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Извините, не удалось сгенерировать текст. Возможно, в вашем словаре пока недостаточно слов.",
                        cancellationToken: cancellationToken);
                    return;
                }
                
                _generatedTexts[chatId] = generatedText; 

                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(
                            text: "🔙 Вернуться в меню",
                            callbackData: "return_menu")
                    }
                });

                var message = new StringBuilder();
                message.AppendLine("📝 Сгенерированный текст на основе ваших слов:\n");
                message.AppendLine(generatedText.EnglishText);
                message.AppendLine(generatedText.RussianText);
                message.AppendLine("\n📚 Использованные слова:");
                foreach (var word in generatedText.Words)
                {
                    message.AppendLine($"• {word.Key}");
                }

                await _bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: message.ToString(),
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await _bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Извините, произошла ошибка при генерации текста. Пожалуйста, попробуйте позже.",
                    cancellationToken: cancellationToken);
            }
        }

        private static async Task HandleVocabularyCommand(long chatId, CancellationToken cancellationToken)
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

                var vocabulary = await _apiClient.GetLearnedWordsAsync(chatId);
                if (vocabulary == null || !vocabulary.Any())
                {
                    await _bot!.SendTextMessageAsync(
                        chatId: chatId,
                        text: "У вас пока нет изученных слов. Используйте /start, чтобы начать изучение!",
                        cancellationToken: cancellationToken);
                    return;
                }

                var message = new StringBuilder("📚 Ваш словарный запас:\n\n");
                
                var groupedWords = vocabulary.GroupBy(w => w.Category);
                
                foreach (var group in groupedWords)
                {
                    message.AppendLine($"📑 *{group.Key}*:");
                    foreach (var word in group)
                    {
                        message.AppendLine($"• {word.Text} - {word.Translation}");
                    }
                    message.AppendLine();
                }

                await _bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: message.ToString(),
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await _bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Извините, произошла ошибка при получении словаря. Пожалуйста, попробуйте позже.",
                    cancellationToken: cancellationToken);
            }
        }
    }
}