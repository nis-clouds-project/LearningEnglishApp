using Frontend.Managers;
using DotNetEnv;

try
{
    // Загружаем переменные окружения из .env файла
    Env.Load();
    
    Console.WriteLine("Запуск бота...");
    Console.WriteLine($"Используется URL бэкенда: {Environment.GetEnvironmentVariable("BACKEND_API_URL")}");

    var cts = new CancellationTokenSource();

    // Обработка сигнала завершения для корректного выхода
    Console.CancelKeyPress += (s, e) =>
    {
        e.Cancel = true;
        cts.Cancel();
        Console.WriteLine("\nЗавершение работы бота...");
    };

    // Запускаем бота
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