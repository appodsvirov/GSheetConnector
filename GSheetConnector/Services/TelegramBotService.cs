using GSheetConnector.Interfaces;
using System.Net;
using System.Text;
using GSheetConnector.Models.GoogleTables;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Exceptions;

namespace GSheetConnector.Services;

public class TelegramBotService : ITelegramService
{
    private readonly TelegramBotClient _botClient;
    private readonly string _downloadPath = "Downloads";
    private readonly string _token;
    private readonly IFileReader _reader;
    private readonly IStatementParser _parser;
    private readonly Dictionary<long, string> _userSpreadsheetIds = new();
    private readonly string _credentialsPath;

    public TelegramBotService(
        IFileReader reader,
        IStatementParser parser,
        string credentialsPath, 
        string token)
    {
        _token = token;
        _reader = reader;
        _parser = parser;
        _credentialsPath = credentialsPath;
        _botClient = new TelegramBotClient(_token);
    }

    public void Start()
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        _botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions
        );

        Console.WriteLine("🤖 Бот запущен");
    }

    private async Task HandleUpdateAsync(
        ITelegramBotClient bot,
        Update update,
        CancellationToken cancellationToken)
    {
        try
        {
            if (update.Message is not { } message) return;
            var chatId = message.Chat.Id;

            switch (message.Text)
            {
                case "/start":
                    await HandleStartCommand(bot, chatId);
                    return;

                case "/seturl":
                    await SendUrlInstructionsAsync(bot, chatId);
                    return;
            }

            if (message.Text != null)
            {
                if (message.Text.StartsWith("/seturl "))
                {
                    await HandleSetUrlCommand(bot, chatId, message.Text);
                    return;
                }

                if (message.Text == "📤 Отправить файл")
                {
                    await HandleFileUploadCommand(bot, chatId);
                    return;
                }

                if (message.Text == "✏️ Изменить URL")
                {
                    await HandleChangeUrlCommand(bot, chatId);
                    return;
                }
            }

            if (message.Document != null || message.Photo != null)
            {
                await HandleFileMessage(bot, message, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка обработки сообщения: {ex}");
        }
    }

    private async Task HandleStartCommand(
        ITelegramBotClient bot,
        long chatId)
    {
        var welcomeMessage = "👋 Добро пожаловать!\n\n" +
                            "Для работы с ботом:\n" +
                            "1. Установите URL таблицы командой /seturl\n" +
                            "2. Отправляйте PDF-файлы через кнопку ниже\n\n" +
                            "Сейчас доступные команды:";

        await bot.SendTextMessageAsync(
            chatId,
            welcomeMessage,
            replyMarkup: CreateMainKeyboard());
    }

    private async Task HandleSetUrlCommand(
        ITelegramBotClient bot,
        long chatId,
        string messageText)
    {
        var url = messageText["/seturl ".Length..].Trim();

        if (!IsValidSpreadsheetUrl(url))
        {
            await bot.SendTextMessageAsync(
                chatId,
                "❌ Неверный формат URL таблицы. Пример:\n" +
                "https://docs.google.com/spreadsheets/d/ID_таблицы/edit");
            return;
        }

        _userSpreadsheetIds[chatId] = ExtractSpreadsheetId(url);
        await bot.SendTextMessageAsync(
            chatId,
            $"✅ URL таблицы успешно установлен!\n" +
            $"Теперь вы можете отправлять файлы",
            replyMarkup: CreateMainKeyboard());
    }

    private async Task HandleFileUploadCommand(
        ITelegramBotClient bot,
        long chatId)
    {
        if (!_userSpreadsheetIds.ContainsKey(chatId))
        {
            await SendUrlInstructionsAsync(bot, chatId);
            return;
        }

        await bot.SendTextMessageAsync(
            chatId,
            "⬆️ Отправьте PDF-файл для импорта");
    }

    private async Task HandleChangeUrlCommand(
        ITelegramBotClient bot,
        long chatId)
    {
        await SendUrlInstructionsAsync(bot, chatId);
        await bot.SendTextMessageAsync(
            chatId,
            "Текущий URL будет перезаписан");
    }

    private async Task HandleFileMessage(
        ITelegramBotClient bot,
        Message message,
        CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;

        if (!_userSpreadsheetIds.TryGetValue(chatId, out var spreadsheetId))
        {
            await SendUrlInstructionsAsync(bot, chatId);
            return;
        }

        var fileId = message.Document?.FileId
                   ?? message.Photo?.Last()?.FileId
                   ?? throw new InvalidOperationException("File ID not found");

        var fileName = message.Document?.FileName ?? $"file_{DateTime.Now:yyyyMMddHHmmss}.pdf";

        try
        {
            var file = await bot.GetFileAsync(fileId, cancellationToken);
            await ProcessFileAsync(bot, chatId, file, fileName, spreadsheetId, cancellationToken);
        }
        catch (ApiRequestException ex)
        {
            Console.WriteLine($"Telegram API Error: {ex.Message}");
            await bot.SendTextMessageAsync(chatId, "❌ Ошибка при получении файла от Telegram");
        }
    }

    private async Task ProcessFileAsync(
        ITelegramBotClient bot,
        long chatId,
        TGFile file,
        string fileName,
        string spreadsheetId,
        CancellationToken cancellationToken)
    {
        try
        {
            var filePath = Path.Combine(_downloadPath, fileName);
            Directory.CreateDirectory(_downloadPath);

            // Формируем URL для скачивания файла

            var fileUrl = $"https://api.telegram.org/file/bot{_token}/{file.FilePath}";

            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(fileUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                await bot.SendTextMessageAsync(chatId, "❌ Не удалось скачать файл");
                return;
            }

            await using (var stream = await response.Content.ReadAsStreamAsync())
            await using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await stream.CopyToAsync(fileStream, cancellationToken);
            }

            var fileContent = _reader.ReadPdf(filePath);
            var statements = _parser.ParseTransactions(fileContent)
                .Select(s => new ArticleModel(s))
                .ToList();

            var sheetsService = new GoogleSheetsService(
                _credentialsPath,
                spreadsheetId);

            await sheetsService.UpdateArticleAsync(statements);

            await bot.SendTextMessageAsync(
                chatId,
                $"✅ Файл {fileName} успешно импортирован!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка обработки файла: {ex}");
            await bot.SendTextMessageAsync(
                chatId,
                "❌ Ошибка при обработке файла");
        }
        finally
        {
            // Очистка временных файлов
            var tempFile = Path.Combine(_downloadPath, fileName);
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    private ReplyKeyboardMarkup CreateMainKeyboard()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("📤 Отправить файл") },
            new[] { new KeyboardButton("✏️ Изменить URL") }
        })
        {
            ResizeKeyboard = true,
            InputFieldPlaceholder = "Выберите действие"
        };
    }

    private async Task SendUrlInstructionsAsync(
        ITelegramBotClient bot,
        long chatId)
    {
        await bot.SendTextMessageAsync(
            chatId,
            "🔧 Чтобы установить URL таблицы, отправьте команду:\n" +
            "/seturl [ваш_url]\n\n" +
            "Пример:\n" +
            "/seturl https://docs.google.com/spreadsheets/d/abc123/edit");
    }

    private static bool IsValidSpreadsheetUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
            && uriResult.Host == "docs.google.com"
            && uriResult.AbsolutePath.Contains("/spreadsheets/d/");
    }

    private static string ExtractSpreadsheetId(string url)
    {
        var startIndex = url.IndexOf("/d/", StringComparison.Ordinal) + 3;
        var endIndex = url.IndexOf("/", startIndex, StringComparison.Ordinal);
        return endIndex == -1
            ? url[startIndex..]
            : url[startIndex..endIndex];
    }

    private Task HandleErrorAsync(
        ITelegramBotClient bot,
        Exception exception,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Ошибка бота: {exception.Message}");
        return Task.CompletedTask;
    }
}