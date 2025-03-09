using Frontend.Services;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Text;

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
        private static readonly Dictionary<string, string> Categories = new()
        {
            { "Food", "🍔 Еда" },
            { "Technology", "💻 Технологии" },
            { "Business", "💼 Бизнес" },
            { "Travel", "✈️ Путешествия" },
            { "Health", "🏥 Здоровье" },
            { "Education", "📚 Образование" },
            { "Entertainment", "🎮 Развлечения" },
            { "Sports", "⚽ Спорт" },
            { "All", "📚 Все категории" }
        };

        private static CancellationTokenSource? _cts;

        /// <summary>
        /// Запускает бота и начинает обработку входящих сообщений.
        /// </summary>
        public static async Task StartAsync()
        {
            var token = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN") 
                       ?? throw new InvalidOperationException("TELEGRAM_BOT_TOKEN не задан");
            var baseUrl = Environment.GetEnvironmentVariable("BACKEND_API_URL") 
                       ?? throw new InvalidOperationException("BACKEND_API_URL не задан");
            
            Console.WriteLine($"Инициализация бота с URL бэкенда: {baseUrl}");
            
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
                // Получаем информацию о боте для проверки токена
                var me = await _bot.GetMeAsync(_cts.Token);
                Console.WriteLine($"Бот @{me.Username} запущен успешно!");

                // Запускаем получение обновлений
                _bot.StartReceiving(
                    updateHandler: HandleUpdateAsync,
                    pollingErrorHandler: HandleErrorAsync,
                    receiverOptions: receiverOptions,
                    cancellationToken: _cts.Token
                );

                // Держим приложение запущенным
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
                Console.WriteLine($"Получено сообщение '{messageText}' в чате {chatId}");

                if (messageText == "/start")
                {
                    await HandleStartCommand(chatId, cancellationToken);
                    return;
                }

                var userExists = await _apiClient!.UserExistsAsync(chatId);
                if (!userExists)
                {
                    await _bot!.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Пожалуйста, начните с команды /start",
                        cancellationToken: cancellationToken);
                    return;
                }

                switch (messageText.ToLower())
                {
                    case "/learn":
                        await ShowCategories(chatId, cancellationToken);
                        break;
                    case "/help":
                        await ShowHelp(chatId, cancellationToken);
                        break;
                    case "/categories":
                        await HandleCategoriesCommand(chatId);
                        break;
                    case "/generate":
                        await HandleGenerateCommand(chatId, cancellationToken);
                        break;
                    case "/vocabulary":
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
                Console.WriteLine($"Получен callback с данными: {data}");

                if (data.StartsWith("learn_"))
                {
                    var category = data[6..];
                    Console.WriteLine($"Обработка команды изучения для категории: {category}");
                    await HandleCategoryLearning(chatId.Value, category, cancellationToken);
                }
                else if (data.StartsWith("known_"))
                {
                    var idString = data[6..];
                    Console.WriteLine($"Обработка известного слова с ID строкой: {idString}");
                    if (int.TryParse(idString, out var wordId))
                    {
                        Console.WriteLine($"ID слова успешно преобразован: {wordId}");
                        await HandleKnownWord(chatId.Value, wordId, cancellationToken);
                    }
                    else
                    {
                        Console.WriteLine($"Не удалось преобразовать ID слова из строки: {idString}");
                        await _bot!.SendTextMessageAsync(
                            chatId: chatId.Value,
                            text: "Произошла ошибка при обработке слова. Пожалуйста, попробуйте еще раз.",
                            cancellationToken: cancellationToken);
                    }
                }
                else if (data.StartsWith("show_translation_"))
                {
                    var idString = data["show_translation_".Length..];
                    Console.WriteLine($"Обработка показа перевода с ID строкой: {idString}");
                    Console.WriteLine($"Длина строки ID: {idString.Length}");
                    Console.WriteLine($"Содержимое строки ID: '{idString}'");
                    
                    if (int.TryParse(idString, out var wordId))
                    {
                        Console.WriteLine($"ID слова успешно преобразован: {wordId}");
                        try 
                        {
                            await HandleShowTranslation(chatId.Value, wordId, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Исключение в HandleShowTranslation: {ex.Message}");
                            Console.WriteLine($"Stack trace: {ex.StackTrace}");
                            throw; // Пробрасываем исключение для общей обработки
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Не удалось преобразовать ID слова из строки: '{idString}'");
                        await _bot!.SendTextMessageAsync(
                            chatId: chatId.Value,
                            text: "Произошла ошибка при показе перевода. Пожалуйста, попробуйте еще раз.",
                            cancellationToken: cancellationToken);
                    }
                }
                else if (data.StartsWith("next_"))
                {
                    var category = data[5..];
                    Console.WriteLine($"Обработка следующего слова для категории: {category}");
                    await HandleCategoryLearning(chatId.Value, category, cancellationToken);
                }
                else if (data == "return_menu")
                {
                    Console.WriteLine("Возврат в главное меню");
                    await HandleStartCommand(chatId.Value, cancellationToken);
                }
                else
                {
                    Console.WriteLine($"Получен неизвестный тип callback данных: {data}");
                }

                await _bot!.AnswerCallbackQueryAsync(
                    callbackQueryId: callback.Id,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обработке callback: {ex}");
                Console.WriteLine($"Данные callback: {data}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

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
            Console.WriteLine($"Ошибка в боте: {exception}");
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
                                   "/categories - показать доступные категории слов\n" +
                                   "/generate - сгенерировать текст на основе изученных слов\n" +
                                   "/vocabulary - Просмотреть изученные слова";

                await _bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: welcomeMessage,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обработке команды /start: {ex}");
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
                var buttons = Categories.Select(category =>
                    new[] { InlineKeyboardButton.WithCallbackData(
                        text: category.Value,
                        callbackData: $"learn_{category.Key}") }
                ).ToList();

                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(
                    text: "🔙 Вернуться в меню",
                    callbackData: "return_menu") });

                var keyboard = new InlineKeyboardMarkup(buttons);

                await _bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: "📚 Выберите категорию для изучения или начните изучать слова из всех категорий:",
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при показе категорий: {ex}");
                await _bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Произошла ошибка при загрузке категорий. Пожалуйста, попробуйте позже.",
                    cancellationToken: cancellationToken);
            }
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
                Console.WriteLine($"Ошибка при показе справки: {ex}");
                await _bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Произошла ошибка при загрузке справки. Пожалуйста, попробуйте позже.",
                    cancellationToken: cancellationToken);
            }
        }

        private static async Task HandleCategoryLearning(long chatId, string category, CancellationToken cancellationToken)
        {
            try
            {
                var word = await _apiClient!.GetRandomWordAsync(chatId, category == "All" ? null : category);
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
                             $"📚 Категория: {Categories[word.Category]}";

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
                            callbackData: $"next_{category}"),
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
                Console.WriteLine($"Ошибка при обработке изучения категории: {ex}");
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
                Console.WriteLine($"[HandleShowTranslation] Начало обработки перевода для слова с ID: {wordId}");
                
                if (_apiClient == null)
                {
                    Console.WriteLine("[HandleShowTranslation] _apiClient is null!");
                    throw new InvalidOperationException("ApiClient is not initialized");
                }
                
                Console.WriteLine("[HandleShowTranslation] Запрос слова из API...");
                var word = await _apiClient.GetWordByIdAsync(wordId);
                Console.WriteLine($"[HandleShowTranslation] Результат запроса слова: {(word != null ? "получено" : "null")}");
                
                if (word == null)
                {
                    Console.WriteLine($"[HandleShowTranslation] Слово с ID {wordId} не найдено");
                    await _bot!.SendTextMessageAsync(
                        chatId: chatId,
                        text: "❌ Не удалось получить информацию о слове.",
                        cancellationToken: cancellationToken);
                    return;
                }

                Console.WriteLine($"[HandleShowTranslation] Получено слово: {word.Text} ({word.Translation}), категория: {word.Category}");

                // Проверяем, существует ли категория в нашем словаре
                if (!Categories.ContainsKey(word.Category))
                {
                    Console.WriteLine($"[HandleShowTranslation] Категория {word.Category} не найдена в словаре категорий");
                    word.Category = "All"; // Используем категорию "All" как запасной вариант
                }

                var message = $"📝 Слово:\n\n" +
                             $"🇬🇧 {word.Text}\n" +
                             $"🇷🇺 {word.Translation}\n" +
                             $"📚 Категория: {Categories[word.Category]}";

                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(
                            text: "✅ Запомнил(а)",
                            callbackData: $"known_{word.Id}"),
                        InlineKeyboardButton.WithCallbackData(
                            text: "➡️ Следующее слово",
                            callbackData: $"next_{word.Category}")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(
                            text: "🔙 Вернуться в меню",
                            callbackData: "return_menu")
                    }
                });

                Console.WriteLine("[HandleShowTranslation] Отправка сообщения пользователю...");
                await _bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: message,
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken);
                Console.WriteLine("[HandleShowTranslation] Сообщение успешно отправлено");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HandleShowTranslation] Ошибка при показе перевода для слова {wordId}:");
                Console.WriteLine($"[HandleShowTranslation] Сообщение об ошибке: {ex.Message}");
                Console.WriteLine($"[HandleShowTranslation] Тип исключения: {ex.GetType().Name}");
                Console.WriteLine($"[HandleShowTranslation] Stack trace: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[HandleShowTranslation] Inner exception: {ex.InnerException.Message}");
                    Console.WriteLine($"[HandleShowTranslation] Inner exception stack trace: {ex.InnerException.StackTrace}");
                }
                
                throw; // Пробрасываем исключение для общей обработки
            }
        }

        private static async Task HandleKnownWord(long chatId, int wordId, CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine($"[HandleKnownWord] Начало добавления слова {wordId} для пользователя {chatId}");
                
                if (_apiClient == null)
                {
                    Console.WriteLine("[HandleKnownWord] _apiClient is null!");
                    throw new InvalidOperationException("ApiClient is not initialized");
                }

                Console.WriteLine("[HandleKnownWord] Вызов AddWordToVocabularyAsync...");
                var success = await _apiClient.AddWordToVocabularyAsync(chatId, wordId);
                Console.WriteLine($"[HandleKnownWord] Результат добавления слова: {success}");

                if (success)
                {
                    Console.WriteLine("[HandleKnownWord] Слово успешно добавлено, отправка подтверждения пользователю");
                    await _bot!.SendTextMessageAsync(
                        chatId: chatId,
                        text: "✅ Отлично! Слово добавлено в ваш словарь.",
                        cancellationToken: cancellationToken);

                    // После успешного добавления показываем следующее слово
                    var word = await _apiClient.GetWordByIdAsync(wordId);
                    if (word != null)
                    {
                        Console.WriteLine($"[HandleKnownWord] Показ следующего слова из категории {word.Category}");
                        await HandleCategoryLearning(chatId, word.Category, cancellationToken);
                    }
                }
                else
                {
                    Console.WriteLine("[HandleKnownWord] Не удалось добавить слово");
                    await _bot!.SendTextMessageAsync(
                        chatId: chatId,
                        text: "❌ Не удалось добавить слово в словарь. Попробуйте позже.",
                        cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HandleKnownWord] Ошибка при обработке известного слова:");
                Console.WriteLine($"[HandleKnownWord] Сообщение об ошибке: {ex.Message}");
                Console.WriteLine($"[HandleKnownWord] Тип исключения: {ex.GetType().Name}");
                Console.WriteLine($"[HandleKnownWord] Stack trace: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[HandleKnownWord] Inner exception: {ex.InnerException.Message}");
                    Console.WriteLine($"[HandleKnownWord] Inner exception stack trace: {ex.InnerException.StackTrace}");
                }

                await _bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Произошла ошибка при добавлении слова. Пожалуйста, попробуйте позже.",
                    cancellationToken: cancellationToken);
            }
        }

        private static async Task HandleCategoriesCommand(long chatId)
        {
            try
            {
                var buttons = Categories.Select(category =>
                    new[] { InlineKeyboardButton.WithCallbackData(
                        text: category.Value,
                        callbackData: $"learn_{category.Key}") }
                );

                var keyboard = new InlineKeyboardMarkup(buttons);

                await _bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: "📚 Выберите категорию для изучения:",
                    replyMarkup: keyboard);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при показе категорий: {ex}");
                await _bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Произошла ошибка при загрузке категорий. Пожалуйста, попробуйте позже.");
            }
        }

        private static async Task HandleGenerateCommand(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                var text = await _apiClient!.GenerateTextFromVocabularyAsync(chatId);
                if (string.IsNullOrEmpty(text))
                {
                    await _bot!.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Извините, не удалось сгенерировать текст. Возможно, в вашем словаре пока недостаточно слов.",
                        cancellationToken: cancellationToken);
                    return;
                }

                await _bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"Сгенерированный текст:\n\n{text}",
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при генерации текста: {ex}");
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
                var vocabulary = await _apiClient!.GetLearnedWordsAsync(chatId);
                if (vocabulary == null || !vocabulary.Any())
                {
                    await _bot!.SendTextMessageAsync(
                        chatId: chatId,
                        text: "У вас пока нет изученных слов. Используйте /start, чтобы начать изучение!",
                        cancellationToken: cancellationToken);
                    return;
                }

                var message = new StringBuilder("📚 Ваш словарный запас:\n\n");
                
                foreach (var category in vocabulary)
                {
                    message.AppendLine($"📑 *{category.Category}*:");
                    foreach (var word in category.Words)
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
                Console.WriteLine($"Ошибка при получении словаря: {ex}");
                await _bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Извините, произошла ошибка при получении словаря. Пожалуйста, попробуйте позже.",
                    cancellationToken: cancellationToken);
            }
        }
    }
}