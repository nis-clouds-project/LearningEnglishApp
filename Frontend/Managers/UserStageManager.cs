using System.Collections.Concurrent;

namespace Frontend.Managers
{
    public enum UserStage
    {
        Initial,
        ChoosingCategory,
        Learning,
        ViewingVocabulary,
        GeneratingText
    }

    public class UserStageManager
    {
        private static readonly ConcurrentDictionary<long, UserStage> _userStages = new();
        private static readonly ConcurrentDictionary<long, long?> _userCurrentCategory = new();

        public static UserStage GetUserStage(long userId)
        {
            return _userStages.GetOrAdd(userId, UserStage.Initial);
        }

        public static void SetUserStage(long userId, UserStage stage)
        {
            _userStages.AddOrUpdate(userId, stage, (_, _) => stage);
        }

        public static void SetUserCurrentCategory(long userId, long? categoryId)
        {
            _userCurrentCategory.AddOrUpdate(userId, categoryId, (_, _) => categoryId);
        }
        
        public static long? GetUserCurrentCategory(long userId)
        {
            return _userCurrentCategory.TryGetValue(userId, out var categoryId) ? categoryId : null;
        }

        public static void ResetUserState(long userId)
        {
            _userStages.TryRemove(userId, out _);
            _userCurrentCategory.TryRemove(userId, out _);
        }
    }
} 