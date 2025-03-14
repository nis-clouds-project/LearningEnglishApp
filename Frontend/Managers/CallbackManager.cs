using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Frontend.Managers;

public static class CallbackManager
{
    private static readonly ILogger Logger = LoggerFactory.Create(builder => builder.AddConsole())
        .CreateLogger("CallbackManager");
    
    public static async Task HandleCallbackAsync(CallbackQuery callback, CancellationToken cancellationToken)
    {
        try
        {
            var chatId = callback.Message?.Chat.Id;
            if (chatId == null) return;

            var data = callback.Data;
            if (string.IsNullOrEmpty(data)) return;

            Logger.LogInformation("Processing callback {Data} for user {ChatId}", data, chatId.Value);

            var currentStage = UserStageManager.GetUserStage(chatId.Value);

            switch (data)
            {
                case "learn_menu":
                    try
                    {
                        Logger.LogInformation("Processing learn_menu callback for user {ChatId}", chatId.Value);
                        UserStageManager.SetUserStage(chatId.Value, UserStage.ChoosingCategory);
                        await LearningManager.ShowCategories(BotManager.Bot!, chatId.Value, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Error processing learn_menu callback for user {ChatId}", chatId.Value);
                        await BotManager.Bot!.SendTextMessageAsync(
                            chatId: chatId.Value,
                            text: "Произошла ошибка при загрузке категорий. Пожалуйста, попробуйте позже или используйте команду /learn.",
                            cancellationToken: cancellationToken);
                    }
                    break;
                case var s when s.StartsWith("learn_"):
                    var category = s[6..];
                    Logger.LogInformation("User {ChatId} selected category {Category}", chatId.Value, category);
                    UserStageManager.SetUserStage(chatId.Value, UserStage.Learning);
                    UserStageManager.SetUserCurrentCategory(chatId.Value, category == "all" ? null : long.Parse(category));
                    await LearningManager.HandleCategoryLearning(chatId.Value, category, cancellationToken);
                    break;
                case var s when s.StartsWith("known_"):
                    if (currentStage != UserStage.Learning)
                    {
                        UserStageManager.SetUserStage(chatId.Value, UserStage.Learning);
                    }
                    var idString = s[6..];
                    if (int.TryParse(idString, out var wordId))
                    {
                        await LearningManager.HandleKnownWord(chatId.Value, wordId, cancellationToken);
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
                        await LearningManager.HandleShowTranslation(chatId.Value, translationWordId, cancellationToken);
                    }
                    break;
                case var s when s.StartsWith("next_"):
                    if (currentStage != UserStage.Learning)
                    {
                        UserStageManager.SetUserStage(chatId.Value, UserStage.Learning);
                    }
                    var nextCategory = s[5..];
                    await LearningManager.HandleCategoryLearning(chatId.Value, nextCategory, cancellationToken);
                    break;
                case "return_menu":
                    UserStageManager.ResetUserState(chatId.Value);
                    await MessageCommandManager.ShowMainMenu(BotManager.Bot!, chatId.Value, cancellationToken);
                    break;
                case "add_word":    
                    UserStageManager.SetUserStage(chatId.Value, UserStage.AddingWord);
                    await VocabularyManager.StartAddWord(chatId.Value, cancellationToken);
                    break;
                case "show_vocabulary":
                    UserStageManager.SetUserStage(chatId.Value, UserStage.ViewingVocabulary);
                    await VocabularyManager.HandleVocabularyCommand(chatId.Value, cancellationToken);
                    break;
                case "generate_text":
                    UserStageManager.SetUserStage(chatId.Value, UserStage.GeneratingText);
                    await LearningManager.HandleGenerateCommand(chatId.Value, cancellationToken);
                    break;
                case "show_my_words":
                    await VocabularyManager.HandleShowMyWords(chatId.Value, cancellationToken);
                    break;
                case var s when s.StartsWith("delete_myword_"):
                    var wordIdString = s["delete_myword_".Length..];
                    if (long.TryParse(wordIdString, out var wordCustomId))
                    {
                        await VocabularyManager.HandleDeleteMyWord(chatId.Value, wordCustomId, cancellationToken);
                    }
                    break;
                default:
                    break;
            }

            await BotManager.Bot!.AnswerCallbackQueryAsync(
                callbackQueryId: callback.Id,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in HandleCallbackAsync");
            if (callback.Message?.Chat.Id != null)
            {
                await BotManager.Bot!.SendTextMessageAsync(
                    chatId: callback.Message.Chat.Id,
                    text: "Произошла ошибка при обработке действия. Пожалуйста, попробуйте еще раз или вернитесь в главное меню.",
                    cancellationToken: cancellationToken);
            }
        }
    }
}