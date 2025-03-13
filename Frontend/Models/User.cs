namespace Frontend.Models
{
    /// <summary>
    /// Модель пользователя для внутреннего использования
    /// </summary>
    public class User
    {
        public long Id { get; set; }
        public List<long>? learned_words { get; set; }
        public List<long>? my_words { get; set; }
        public long? current_learning_category { get; set; }
    }
}