using System.Net;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace GSheetConnector.Services
{
    public class TelegramBotService
    {
        private readonly TelegramBotClient _botClient;
        private readonly string _downloadPath = "Downloads"; // Папка для файлов
        private readonly string _token;
        public TelegramBotService(IConfiguration config)
        {
            _token = config["BotConfiguration:BotToken"]
                         ?? throw new ArgumentNullException("Bot token is missing");

            _botClient = new TelegramBotClient(_token);
        }

        public void Start()
        {
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[] { UpdateType.Message } // Получаем только сообщения
            };

            _botClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                CancellationToken.None
            );

            Console.WriteLine("🤖 Бот запущен...");
        }

        private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
        {
            if (update.Message is not { } message) return;
            long chatId = message.Chat.Id;

            // 📌 Обработка текстовых сообщений
            if (!string.IsNullOrEmpty(message.Text))
            {
                string text = message.Text.ToLower();
                string response = text switch
                {
                    "/start" => "Привет! Отправь мне файл!",
                    _ => "Я жду файл или документ."
                };

                await bot.SendTextMessageAsync(chatId, response);
            }

            // Обработка документов 
            if (message.Document is { } document)
            {
                await HandleFileAsync(bot, chatId, document.FileId, document.FileName);
            }

            // Обработка изображений
            if (message.Photo?.Length > 0)
            {
                var photo = message.Photo.Last(); // Берём изображение лучшего качества
                await HandleFileAsync(bot, chatId, photo.FileId, "photo.jpg");
            }
        }

        private async Task HandleFileAsync(ITelegramBotClient bot, long chatId, string fileId, string fileName)
        {
            var file = await bot.GetFileAsync(fileId);
            string fileUrl = $"https://api.telegram.org/file/bot{_token}/{file.FilePath}";

            string savePath = Path.Combine(_downloadPath, fileName);
            Directory.CreateDirectory(_downloadPath); // Создаём папку, если её нет

            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(fileUrl);
                if (response.IsSuccessStatusCode)
                {
                    var fileBytes = await response.Content.ReadAsByteArrayAsync();
                    await File.WriteAllBytesAsync(savePath, fileBytes);
                    await bot.SendTextMessageAsync(chatId, $"✅ Файл сохранён: {fileName}");
                    Console.WriteLine($"Файл {fileName} сохранён в {savePath}");
                }
                else
                {
                    await bot.SendTextMessageAsync(chatId, "❌ Ошибка загрузки файла.");
                    Console.WriteLine($"Ошибка загрузки файла: {response.StatusCode}");
                }
            }
        }

        private Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Ошибка бота: {exception.Message}");
            return Task.CompletedTask;
        }
    }
}
