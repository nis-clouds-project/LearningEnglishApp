using LearningBotCore.integrations;
using LearningBotCore.integrations.interfaces;
using LearningBotCore.service;
using LearningBotCore.service.interfaces;

namespace LearningBotCore
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddSingleton<IUserManager, InMemoryUserManager>();
            builder.Services.AddSingleton<IWordManager, InMemoryWordManager>();
            builder.Services.AddSingleton<ITextGenerator, GigaChatTextGenerator>();
            builder.Services.AddControllers();

            var app = builder.Build();

            app.UseHttpsRedirection();
            app.MapControllers();

            app.Run();
        }
    }
}