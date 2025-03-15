using Telegram.Bot;
using Telegram.Bot.Types;

namespace Frontend.Managers;

public static class UpdateRoute
{
    public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try
        {
            if (update.CallbackQuery is { } callback)
            {
                await CallbackManager.HandleCallbackAsync(callback, cancellationToken);
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
                    await VocabularyManager.HandleAddWordStep1(chatId, messageText, cancellationToken);
                    return;
                case UserStage.AddingTranslation:
                    await VocabularyManager.HandleAddWordStep2(chatId, messageText, cancellationToken);
                    return;
                case UserStage.Practising:
                    await PractisingManager.HandlePracticeAnswer(chatId, messageText, cancellationToken);
                    return;
                case UserStage.WaitingForLocalTranslateWord:
                    var direction = UserStageManager.GetTempWord(chatId);
                    await TranslationManager.HandleLocalTranslation(chatId, messageText, direction, cancellationToken);
                    return;
            }

            switch (messageText.ToLower())
            {
                case "/start":
                    UserStageManager.ResetUserState(chatId);
                    await MessageCommandManager.HandleStartCommand(botClient, message, cancellationToken);
                    break;
                case "/learn":
                    UserStageManager.SetUserStage(chatId, UserStage.ChoosingCategory);
                    await LearningManager.ShowCategories(botClient, chatId, cancellationToken);
                    break;
                case "/help":
                    await MessageCommandManager.ShowHelp(chatId, cancellationToken);
                    break;
                case "/categories":
                    UserStageManager.SetUserStage(chatId, UserStage.ChoosingCategory);
                    await LearningManager.ShowCategories(botClient, chatId, cancellationToken);
                    break;
                case "/generate":
                    UserStageManager.SetUserStage(chatId, UserStage.GeneratingText);
                    await LearningManager.HandleGenerateCommand(chatId, cancellationToken);
                    break;
                case "/vocabulary":
                    UserStageManager.SetUserStage(chatId, UserStage.ViewingVocabulary);
                    await VocabularyManager.HandleVocabularyCommand(chatId, cancellationToken);
                    break;
                case "/addword":
                    await VocabularyManager.StartAddWord(chatId, cancellationToken);
                    break;
                case "/mywords":
                    await VocabularyManager.HandleShowMyWords(chatId, cancellationToken);
                    break;
                case "/translate":
                    UserStageManager.ResetUserState(chatId);
                    await TranslationManager.ShowLocalTranslateDirectionMenu(chatId, cancellationToken);
                    break;
                case "/practise":
                    UserStageManager.ResetUserState(chatId);
                    await PractisingManager.HandlePracticeCommand(chatId, cancellationToken);
                    break;
                default:
                    await BotManager.Bot!.SendTextMessageAsync(
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

    public static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}