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

        static BotManager()
        {
        }

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

                switch (currentStage)
                {
                    case UserStage.AddingWord:
                        await HandleAddWordStep1(chatId, messageText, cancellationToken);
                        return;
                    case UserStage.AddingTranslation:
                        await HandleAddWordStep2(chatId, messageText, cancellationToken);
                        return;
                }

                switch (messageText.ToLower())
                {
                    case "/start":
                        UserStageManager.ResetUserState(chatId);
                        await HandleStartCommand(botClient, message, cancellationToken);
                        break;
                    case "/learn":
                        UserStageManager.SetUserStage(chatId, UserStage.ChoosingCategory);
                        await ShowCategories(botClient, chatId, cancellationToken);
                        break;
                    case "/help":
                        await ShowHelp(chatId, cancellationToken);
                        break;
                    case "/categories":
                        UserStageManager.SetUserStage(chatId, UserStage.ChoosingCategory);
                        await ShowCategories(botClient, chatId, cancellationToken);
                        break;
                    case "/generate":
                        UserStageManager.SetUserStage(chatId, UserStage.GeneratingText);
                        await HandleGenerateCommand(chatId, cancellationToken);
                        break;
                    case "/vocabulary":
                        UserStageManager.SetUserStage(chatId, UserStage.ViewingVocabulary);
                        await HandleVocabularyCommand(chatId, cancellationToken);
                        break;
                    case "/addword":
                        await StartAddWord(chatId, cancellationToken);
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
                Console.WriteLine($"Error in HandleUpdateAsync: {ex.Message}");
            }
        }

        private static async Task HandleCallbackAsync(CallbackQuery callback, CancellationToken cancellationToken)
        {
            try
            {
                var chatId = callback.Message?.Chat.Id;
                if (chatId == null) return;

                var data = callback.Data;
                if (string.IsNullOrEmpty(data)) return;


                var currentStage = UserStageManager.GetUserStage(chatId.Value);

                switch (data)
                {
                    case "learn_menu":
                        try
                        {
                            UserStageManager.SetUserStage(chatId.Value, UserStage.ChoosingCategory);
                            await ShowCategories(_bot!, chatId.Value, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            await _bot!.SendTextMessageAsync(
                                chatId: chatId.Value,
                                text: "Произошла ошибка при загрузке категорий. Пожалуйста, попробуйте позже или используйте команду /learn.",
                                cancellationToken: cancellationToken);
                        }
                        break;
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
                        await ShowMainMenu(_bot!, chatId.Value, cancellationToken);
                        break;
                    case "add_word":
                        UserStageManager.SetUserStage(chatId.Value, UserStage.AddingWord);
                        await StartAddWord(chatId.Value, cancellationToken);
                        break;
                    case "show_vocabulary":
                        UserStageManager.SetUserStage(chatId.Value, UserStage.ViewingVocabulary);
                        await HandleVocabularyCommand(chatId.Value, cancellationToken);
                        break;
                    case "generate_text":
                        UserStageManager.SetUserStage(chatId.Value, UserStage.GeneratingText);
                        await HandleGenerateCommand(chatId.Value, cancellationToken);
                        break;
                    case "show_my_words":
                        await HandleShowMyWords(chatId.Value, cancellationToken);
                        break;
                    case var s when s.StartsWith("delete_myword_"):
                        var wordIdString = s["delete_myword_".Length..];
                        if (long.TryParse(wordIdString, out var wordCustomId))
                        {
                            await HandleDeleteMyWord(chatId.Value, wordCustomId, cancellationToken);
                        }
                        break;
                    default:
                        break;
                }

                await _bot!.AnswerCallbackQueryAsync(
                    callbackQueryId: callback.Id,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                if (callback.Message?.Chat.Id != null)
                {
                    await _bot!.SendTextMessageAsync(
                        chatId: callback.Message.Chat.Id,
                        text: "Произошла ошибка при обработке действия. Пожалуйста, попробуйте еще раз или вернитесь в главное меню.",
                        cancellationToken: cancellationToken);
                }
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

        private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private static async Task HandleStartCommand(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            var chatId = message.Chat.Id;
            try
            {
                
                var userExists = await _apiClient.UserExistsAsync(chatId);
                if (!userExists)
                {
                    var user = await _apiClient.AddUserAsync(chatId);
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

        private static async Task ShowCategories(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            try
            {
                if (_apiClient == null)
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Произошла внутренняя ошибка. Пожалуйста, попробуйте позже.",
                        cancellationToken: cancellationToken);
                    return;
                }

                var categories = await _apiClient.GetCategoriesAsync();
                
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
                    var firstEmoji = GetCategoryEmoji(firstCategoryName);
                    var firstButtonText = string.Format("{0} {1}", firstEmoji, firstCategoryName);
                    var firstCallbackData = string.Format("learn_{0}", categories[i].Id);
                    
                    rowButtons.Add(InlineKeyboardButton.WithCallbackData(
                        text: firstButtonText,
                        callbackData: firstCallbackData));
                    
                    if (i + 1 < categories.Count)
                    {
                        var secondCategoryName = categories[i + 1].Name ?? "Unknown";
                        var secondEmoji = GetCategoryEmoji(secondCategoryName);
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

        private static string GetCategoryEmoji(string? categoryName)
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
                                  "📝 /addword - Добавить своё слово\n" +
                                  "📖 /vocabulary - Посмотреть изученные слова\n" +
                                  "📝 /mywords - Посмотреть все слова из категории \"My Words\"\n" + // Новая команда
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
                
                var categories = await _apiClient!.GetCategoriesAsync();
                var category = categories?.FirstOrDefault(c => c.Id == parsedCategoryId);
                
                UserStageManager.SetUserCurrentCategory(chatId, parsedCategoryId);
                
                var selectedCategoryName = category?.Name ?? "all";
                var isMyWordsCategory = category?.Name?.Equals("My Words", StringComparison.OrdinalIgnoreCase) == true;

                try
                {
                    Word? word = null;
                    try {
                        if (isMyWordsCategory) {
                            try {
                                word = await _apiClient!.GetRandomCustomWordAsync(chatId);
                                if (word != null)
                                {
                                    var wordCategory = categories?.FirstOrDefault(c => c.Id == word.CategoryId);
                                    if (wordCategory != null)
                                    {
                                        word.CategoryName = wordCategory.Name;
                                    }
                                }
                            } catch (System.Text.Json.JsonException jsonEx) {
                                throw;
                            }
                        } else {
                            word = await _apiClient!.GetRandomWordAsync(chatId, parsedCategoryId);
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
                        
                        await _bot!.SendTextMessageAsync(
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

                        await _bot!.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Выберите действие:",
                            replyMarkup: noWordsKeyboard,
                            cancellationToken: cancellationToken);
                        return;
                    }
                    
                    string wordCategoryName = word.CategoryName;
                    string emojiForCategory = GetCategoryEmoji(wordCategoryName);
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

                    await _bot!.SendTextMessageAsync(
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
                
                var categories = await _apiClient.GetCategoriesAsync();
                var word = await _apiClient.GetWordByIdAsync(wordId);
                if (word == null)
                {
                    await _bot!.SendTextMessageAsync(
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
                    GetCategoryEmoji(word.CategoryName),
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
                    throw new InvalidOperationException("ApiClient is not initialized");
                }

                var currentCategory = UserStageManager.GetUserCurrentCategory(chatId);
                
                var categories = await _apiClient.GetCategoriesAsync();
                var category = categories?.FirstOrDefault(c => c.Id == currentCategory);
                var isMyWordsCategory = category?.Name?.Equals("My Words", StringComparison.OrdinalIgnoreCase) == true;
                
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

                var nextWord = isMyWordsCategory
                    ? await _apiClient.GetRandomCustomWordAsync(chatId)
                    : await _apiClient.GetRandomWordAsync(chatId, currentCategory);

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
                    
                    await _bot!.SendTextMessageAsync(
                        chatId: chatId,
                        text: message,
                        cancellationToken: cancellationToken);
                    return;
                }

                var messageText = string.Format("📝 Новое слово для изучения:\n\n🇬🇧 {0}\n📚 Категория: {1} {2}",
                    nextWord.Text,
                    GetCategoryEmoji(nextWord.CategoryName),
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

                await _bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: messageText,
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

                await _bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: "🎨 Генерирую текст на основе ваших изученных слов...",
                    cancellationToken: cancellationToken);

                var generatedText = await _apiClient.GenerateTextFromVocabularyAsync(chatId);
                if (generatedText == null)
                {
                    await _bot!.SendTextMessageAsync(
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

                await _bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: message.ToString(),
                    parseMode: ParseMode.Markdown,
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await _bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: "❌ Извините, произошла ошибка при генерации текста. Пожалуйста, попробуйте позже.",
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

                var categories = await _apiClient.GetCategoriesAsync();
                var vocabulary = await _apiClient.GetLearnedWordsAsync(chatId);
                
                if (vocabulary == null || !vocabulary.Any())
                {
                    await _bot!.SendTextMessageAsync(
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
                    var categoryEmoji = GetCategoryEmoji(group.Key);
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

                await _bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: message.ToString(),
                    parseMode: ParseMode.Markdown,
                    replyMarkup: keyboard,
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

        private static async Task StartAddWord(long chatId, CancellationToken cancellationToken)
        {
            UserStageManager.SetUserStage(chatId, UserStage.AddingWord);
            await _bot!.SendTextMessageAsync(
                chatId: chatId,
                text: "Введите английское слово, которое хотите добавить:",
                cancellationToken: cancellationToken);
        }

        private static async Task HandleAddWordStep1(long chatId, string englishWord, CancellationToken cancellationToken)
        {
            UserStageManager.SetTempWord(chatId, englishWord);
            UserStageManager.SetUserStage(chatId, UserStage.AddingTranslation);
            var message = string.Format("Теперь введите перевод слова \"{0}\" на русский язык:", englishWord);
            await _bot!.SendTextMessageAsync(
                chatId: chatId,
                text: message,
                cancellationToken: cancellationToken);
        }

        private static async Task HandleAddWordStep2(long chatId, string translation, CancellationToken cancellationToken)
        {
            try
            {
                var englishWord = UserStageManager.GetTempWord(chatId);
                if (string.IsNullOrEmpty(englishWord))
                {
                    await _bot!.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Произошла ошибка. Пожалуйста, начните процесс добавления слова заново с команды /addword",
                        cancellationToken: cancellationToken);
                    UserStageManager.ResetUserState(chatId);
                    return;
                }
                
                try
                {
                    var word = await _apiClient!.AddCustomWordAsync(chatId, englishWord, translation);
                    
                    var categories = await _apiClient.GetCategoriesAsync();
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
                    await _bot!.SendTextMessageAsync(
                        chatId: chatId,
                        text: successMessage,
                        replyMarkup: keyboard,
                        cancellationToken: cancellationToken);
                    
                }
                catch (Exception ex)
                {
                    await _bot!.SendTextMessageAsync(
                        chatId: chatId,
                        text: "❌ Произошла ошибка при добавлении слова. Пожалуйста, попробуйте позже.",
                        cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await _bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Произошла ошибка при добавлении слова. Пожалуйста, попробуйте позже.",
                    cancellationToken: cancellationToken);
            }
            finally
            {
                UserStageManager.ResetUserState(chatId);
            }
        }
        
        private static async Task HandleShowMyWords(long chatId, CancellationToken cancellationToken)
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

                var categories = await _apiClient.GetCategoriesAsync();
                var myWordsCategory = categories?.FirstOrDefault(c => c.Name?.Equals("My Words", StringComparison.OrdinalIgnoreCase) == true);
                Console.WriteLine(myWordsCategory);
                
                if (myWordsCategory == null)
                {
                    await _bot!.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Категория \"My Words\" не найдена.",
                        cancellationToken: cancellationToken);
                    return;
                }

                var vocabulary = await _apiClient.GetAllCustomWordsAsync(chatId);
                Console.WriteLine(vocabulary.Count);
                
                if (vocabulary == null || !vocabulary.Any())
                {
                    Console.WriteLine("Тут");
                    await _bot!.SendTextMessageAsync(
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
                    await _bot!.SendTextMessageAsync(
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

                await _bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: message.ToString(),
                    parseMode: ParseMode.Markdown,
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await _bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Произошла ошибка при получении слов. Пожалуйста, попробуйте позже.",
                    cancellationToken: cancellationToken);
            }
        }

        private static async Task ShowMainMenu(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            var mainMenuMessage = "👋 Добро пожаловать в бот для изучения английских слов!\n\n" +
                                  "Доступные команды:\n" +
                                  "📚 /learn - Начать изучение слов\n" +
                                  "📝 /addword - Добавить своё слово\n" +
                                  "📖 /vocabulary - Посмотреть изученные слова\n" +
                                  "📝 /mywords - Посмотреть все слова из категории \"My Words\"\n" +
                                  "✍️ /generate - Сгенерировать текст из изученных слов\n" +
                                  "❓ /help - Подробная справка\n\n" +
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
                    InlineKeyboardButton.WithCallbackData(text: "✍️ Генерировать текст", callbackData: "generate_text")
                }
            });

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: mainMenuMessage,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
        }
    }
}