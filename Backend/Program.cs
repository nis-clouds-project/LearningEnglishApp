using Backend.Data;
using Backend.Integrations;
using Backend.Integrations.Interfaces;
using Backend.Services;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));


// Register services
builder.Services.AddScoped<IUserManager, DbUserManager>();
builder.Services.AddScoped<IWordManager, DbWordManager>();
builder.Services.AddScoped<ITextGenerator, GigaChatTextGenerator>();
builder.Services.AddScoped<Backend.Integrations.Interfaces.ITokenService, Backend.Integrations.TokenService>();

// Register translator service
builder.Services.AddSingleton<IYandexTokenService, YandexTokenService>();
builder.Services.AddSingleton<HttpClient>();
builder.Services.AddSingleton<ITranslatorService, TranslatorService>();
builder.Services.AddHostedService<YandexTokenBackgroundService>();

var app = builder.Build();

// Add database initialization
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Starting database initialization...");
        await DbInitializer.Initialize(context);
        logger.LogInformation("Database initialization completed successfully.");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the database.");
        throw;
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.Run();