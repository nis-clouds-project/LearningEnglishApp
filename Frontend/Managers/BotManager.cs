using Frontend.Services;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

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
            { "Sports", "⚽ Спорт" }
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
                if (data.StartsWith("learn_"))
                {
                    var category = data[6..];
                    await HandleCategoryLearning(chatId.Value, category, cancellationToken);
                }
                else if (data.StartsWith("known_"))
                {
                    var wordId = int.Parse(data[6..]);
                    await HandleKnownWord(chatId.Value, wordId, cancellationToken);
                }
                else if (data.StartsWith("unknown_"))
                {
                    var wordId = int.Parse(data[8..]);
                    await HandleUnknownWord(chatId.Value, wordId, cancellationToken);
                }
                else if (data.StartsWith("next_"))
                {
                    var category = data[5..];
                    await HandleCategoryLearning(chatId.Value, category, cancellationToken);
                }

                await _bot!.AnswerCallbackQueryAsync(
                    callbackQueryId: callback.Id,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обработке callback: {ex}");
                await _bot!.AnswerCallbackQueryAsync(
                    callbackQueryId: callback.Id,
                    text: "Произошла ошибка. Пожалуйста, попробуйте еще раз.",
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
                                   "❓ /help - Показать справку";

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
                );

                var keyboard = new InlineKeyboardMarkup(buttons);

                await _bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: "📚 Выберите категорию для изучения:",
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
                var word = await _apiClient!.GetRandomWordAsync(chatId, category);
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
                             $"🇷🇺 {word.Translation}\n" +
                             $"📚 Категория: {Categories[word.Category]}";

                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(
                            text: "✅ Знаю",
                            callbackData: $"known_{word.Id}"),
                        InlineKeyboardButton.WithCallbackData(
                            text: "❌ Не знаю",
                            callbackData: $"unknown_{word.Id}")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(
                            text: "➡️ Следующее слово",
                            callbackData: $"next_{category}")
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

        private static async Task HandleKnownWord(long chatId, int wordId, CancellationToken cancellationToken)
        {
            try
            {
                var success = await _apiClient!.AddWordToVocabularyAsync(chatId, wordId);
                if (success)
                {
                    await _bot!.SendTextMessageAsync(
                        chatId: chatId,
                        text: "✅ Отлично! Слово добавлено в ваш словарь.",
                        cancellationToken: cancellationToken);
                }
                else
                {
                    await _bot!.SendTextMessageAsync(
                        chatId: chatId,
                        text: "❌ Не удалось добавить слово в словарь. Попробуйте позже.",
                        cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обработке известного слова: {ex}");
                await _bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Произошла ошибка. Пожалуйста, попробуйте позже.",
                    cancellationToken: cancellationToken);
            }
        }

        private static async Task HandleUnknownWord(long chatId, int wordId, CancellationToken cancellationToken)
        {
            try
            {
                await _bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: "🔄 Ничего страшного! Это слово появится снова позже для повторения.",
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обработке неизвестного слова: {ex}");
                await _bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Произошла ошибка. Пожалуйста, попробуйте позже.",
                    cancellationToken: cancellationToken);
            }
        }
    }
}