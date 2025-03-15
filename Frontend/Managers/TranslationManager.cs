using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace Frontend.Managers;

public static class TranslationManager
{
    public static async Task ShowLocalTranslateDirectionMenu(long chatId, CancellationToken cancellationToken)
    {
        UserStageManager.SetUserStage(chatId, UserStage.ChoosingLocalTranslateDirection);

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("\uD83C\uDDF7\uD83C\uDDFARU → \uD83C\uDDEC\uD83C\uDDE7EN", "local_trans_ru_en"),
                InlineKeyboardButton.WithCallbackData("\uD83C\uDDEC\uD83C\uDDE7EN → \uD83C\uDDF7\uD83C\uDDFARU", "local_trans_en_ru")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🔙 В меню", "return_menu")
            }
        });

        await BotManager.Bot!.SendTextMessageAsync(
            chatId: chatId,
            text: "Выберите направление:",
            replyMarkup: keyboard,
            cancellationToken: cancellationToken
        );
    }
    
    public static async Task HandleLocalTranslation(long chatId, string sourceWord, string direction, CancellationToken cancellationToken)
    {
        try
        {
            var translation = await BotManager.ApiClient!.GetLocalTranslationAsync(sourceWord, direction);

            if (translation == null)
            {
                await BotManager.Bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: "❌ Слово не найдено в базе данных.",
                    cancellationToken: cancellationToken
                );
            }
            else
            {
                await BotManager.Bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"✅ Перевод: {translation}",
                    cancellationToken: cancellationToken
                );
            }
        }
        catch (Exception ex)
        {
            await BotManager.Bot!.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Ошибка при поиске перевода.",
                cancellationToken: cancellationToken
            );
        }
        finally
        {
            UserStageManager.ResetUserState(chatId);
        }
    }
}