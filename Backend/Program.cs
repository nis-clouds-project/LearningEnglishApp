using Backend.Integrations;
using Backend.Integrations.Interfaces;
using Backend.Services;
using Backend.Services.Interfaces;

namespace Backend
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
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            app.UseHttpsRedirection();
            app.MapControllers();
            
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.Run();
        }
    }
}