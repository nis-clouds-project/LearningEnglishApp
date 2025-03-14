using Frontend.Models;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace Frontend.Managers;

public static class PractisingManager
{
    public static async Task HandlePracticeCommand(long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var categories = await BotManager.ApiClient!.GetCategoriesAsync();
            if (categories == null || !categories.Any())
            {
                await BotManager.Bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Ошибка при загрузке категорий.",
                    cancellationToken: cancellationToken);
                return;
            }
        
            var buttons = new List<InlineKeyboardButton[]>();
            foreach (var category in categories)
            {
                var buttonText = $"{MenuFactory.GetCategoryEmoji(category.Name)} {category.Name}";
                var callbackData = $"practise_{category.Id}";
                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(buttonText, callbackData) });
            }
            
            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 В меню", "return_menu") });
            var keyboard = new InlineKeyboardMarkup(buttons);
        
            await BotManager.Bot!.SendTextMessageAsync(
                chatId: chatId,
                text: "Выберите категорию для практики:",
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            await BotManager.Bot!.SendTextMessageAsync(
                chatId: chatId,
                text: "Произошла ошибка при загрузке категорий для практики.",
                cancellationToken: cancellationToken);
        }
    }
    
    public static async Task HandlePracticeIteration(long chatId, long? categoryId, CancellationToken cancellationToken)
    {
        try
        {
            var word = await BotManager.ApiClient!.GetRandomWordAsync(chatId, categoryId);
            if (word == null)
            {
                await BotManager.Bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Нет доступных слов для практики в выбранной категории.",
                    cancellationToken: cancellationToken);
                return;
            }
            
            UserStageManager.SetUserStage(chatId, UserStage.Practising);
            UserStageManager.SetTempWord(chatId, word.Text);
            
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("🔙 В меню", "return_menu") }
            });
            
            await BotManager.Bot!.SendTextMessageAsync(
                chatId: chatId,
                text: $"Перевод: {word.Translation}\nВведите слово на английском:",
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
        }
        catch (Exception)
        {
            await BotManager.Bot!.SendTextMessageAsync(
                chatId: chatId,
                text: "Произошла ошибка при получении слова для практики.",
                cancellationToken: cancellationToken);
        }
    }
    
    public static async Task HandlePracticeAnswer(long chatId, string userAnswer, CancellationToken cancellationToken)
    {
        try
        {
            var expected = UserStageManager.GetTempWord(chatId);
        
            if (!string.IsNullOrWhiteSpace(userAnswer) &&
                string.Equals(userAnswer.Trim(), expected, StringComparison.InvariantCultureIgnoreCase))
            {
                await BotManager.Bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Правильно!",
                    cancellationToken: cancellationToken);
            }
            else
            {
                await BotManager.Bot!.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"Неправильно. Правильный ответ: {expected}",
                    cancellationToken: cancellationToken);
            }
            
            var categoryId = UserStageManager.GetUserCurrentCategory(chatId);
            await HandlePracticeIteration(chatId, categoryId, cancellationToken);
        }
        catch (Exception ex)
        {
            await BotManager.Bot!.SendTextMessageAsync(
                chatId: chatId,
                text: "Произошла ошибка при проверке ответа.",
                cancellationToken: cancellationToken);
        }
    }
}