﻿using System.Transactions;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Frontend.Managers;

public static class CallbackManager
{
    public static async Task HandleCallbackAsync(CallbackQuery callback, CancellationToken cancellationToken)
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
                        await LearningManager.ShowCategories(BotManager.Bot!, chatId.Value, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        await BotManager.Bot!.SendTextMessageAsync(
                            chatId: chatId.Value,
                            text: "Произошла ошибка при загрузке категорий. Пожалуйста, попробуйте позже или используйте команду /learn.",
                            cancellationToken: cancellationToken);
                    }
                    break;
                case var s when s.StartsWith("learn_"):
                    var category = s[6..];
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
                case "practise_menu":
                    UserStageManager.ResetUserState(chatId.Value);
                    await PractisingManager.HandlePracticeCommand(chatId.Value, cancellationToken);
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
                case "local_trans_ru_en":
                    UserStageManager.SetTempWord(chatId.Value, "ru-en");
                    UserStageManager.SetUserStage(chatId.Value, UserStage.WaitingForLocalTranslateWord);
                    await BotManager.Bot!.SendTextMessageAsync(
                        chatId: chatId.Value,
                        text: "✍️ Введите слово на русском языке:",
                        cancellationToken: cancellationToken
                    );
                    break;
                case "local_trans_en_ru":
                    UserStageManager.SetTempWord(chatId.Value, "en-ru");
                    UserStageManager.SetUserStage(chatId.Value, UserStage.WaitingForLocalTranslateWord);
                    await BotManager.Bot!.SendTextMessageAsync(
                        chatId: chatId.Value,
                        text: "✍️ Введите слово на английском языке:",
                        cancellationToken: cancellationToken
                    );
                    break;
                case "translation_menu":
                    await TranslationManager.ShowLocalTranslateDirectionMenu(chatId.Value, cancellationToken);
                    break;
                case "show_my_words":
                    await VocabularyManager.HandleShowMyWords(chatId.Value, cancellationToken);
                    break;
                case var s when s.StartsWith("delete_myword_"):
                    var wordIdString = s["delete_myword_".Length..];
                    if (long.TryParse(wordIdString, out var wordCustomId))
                    {
                        await LearningManager.HandleDeleteMyWord(chatId.Value, wordCustomId, cancellationToken);
                    }
                    break;
                case var s when s.StartsWith("practise_"):
                    var catIdStr = s.Substring("practise_".Length);
                    if (long.TryParse(catIdStr, out var catId))
                    {
                        UserStageManager.SetUserCurrentCategory(chatId.Value, catId);
                        await PractisingManager.HandlePracticeIteration(chatId.Value, catId, cancellationToken);
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