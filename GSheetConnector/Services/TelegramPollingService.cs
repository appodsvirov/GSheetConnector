using GSheetConnector.Controllers;
using GSheetConnector.Interfaces;

namespace GSheetConnector.Services
{
    public class TelegramPollingService : BackgroundService
    {
        private readonly ITelegramService _telegramService;
        private readonly TelegramController _controller;

        public TelegramPollingService(ITelegramService telegramService, TelegramController controller)
        {
            _telegramService = telegramService;
            _controller = controller;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            int offset = 0;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Получение обновлений
                    var updates = await _telegramService.GetUpdatesAsync(offset);

                    foreach (var update in updates)
                    {
                        // Обрабатываем каждое обновление
                        await _controller.ProcessUpdateAsync(update);
                        offset = update.UpdateId + 1; // Обновляем offset
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка: {ex.Message}");
                }

                // Задержка между запросами
                await Task.Delay(1000, stoppingToken);
            }
        }
    }

}
