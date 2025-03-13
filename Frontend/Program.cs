using Frontend.Managers;
using DotNetEnv;

try
{
    Env.Load();
    
    Console.WriteLine("Запуск бота...");
    Console.WriteLine($"Используется URL бэкенда: {Environment.GetEnvironmentVariable("BACKEND_API_URL")}");

    var cts = new CancellationTokenSource();

    Console.CancelKeyPress += (s, e) =>
    {
        e.Cancel = true;
        cts.Cancel();
        Console.WriteLine("\nЗавершение работы бота...");
    };

    await BotManager.StartAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"Критическая ошибка: {ex.Message}");
    Console.WriteLine($"Стек вызовов: {ex.StackTrace}");
}
finally
{
    BotManager.Stop();
    Console.WriteLine("Бот остановлен.");
}