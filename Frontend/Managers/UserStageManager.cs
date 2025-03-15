
namespace Frontend.Managers
{
    public enum UserStage
    {
        Initial,
        ChoosingCategory,
        Learning,
        GeneratingText,
        ViewingVocabulary,
        AddingWord,
        AddingTranslation,
        Practising,
        ChoosingLocalTranslateDirection,
        WaitingForLocalTranslateWord
    }

    public static class UserStageManager
    {
        private static readonly Dictionary<long, UserStage> _userStages = new();
        private static readonly Dictionary<long, long?> _userCurrentCategories = new();
        private static readonly Dictionary<long, string> _tempWordStorage = new();
        public static UserStage GetUserStage(long userId)
        {
            return _userStages.TryGetValue(userId, out var stage) ? stage : UserStage.Initial;
        }
        public static void SetUserStage(long userId, UserStage stage)
        {
            _userStages[userId] = stage;
        }
        public static void ResetUserState(long userId)
        {
            _userStages[userId] = UserStage.Initial;
            _userCurrentCategories.Remove(userId);
            _tempWordStorage.Remove(userId);
        }
        public static long? GetUserCurrentCategory(long userId)
        {
            return _userCurrentCategories.TryGetValue(userId, out var categoryId) ? categoryId : null;
        }
        public static void SetUserCurrentCategory(long userId, long? categoryId)
        {
            if (categoryId == null)
            {
                _userCurrentCategories.Remove(userId);
            }
            else
            {
                _userCurrentCategories[userId] = categoryId;
            }
        }
        public static void SetTempWord(long userId, string word)
        {
            _tempWordStorage[userId] = word;
        }
        public static string GetTempWord(long userId)
        {
            return _tempWordStorage.TryGetValue(userId, out var word) ? word : string.Empty;
        }
    }
}
