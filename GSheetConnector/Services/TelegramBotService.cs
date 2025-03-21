using GSheetConnector.Interfaces;
using System.Net;
using System.Text;
using GSheetConnector.Models.GoogleTables;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using GSheetConnector.Models.TelegramBot;

namespace GSheetConnector.Services;
public class TelegramBotService: ITelegramService
{
    private readonly ITelegramBotClient _botClient;
    private readonly GoogleSheetsService _googleSheetsService;
    private readonly Dictionary<long, string> _userSheets = new();

    public TelegramBotService(string botToken, GoogleSheetsService googleSheetsService)
    {
        _botClient = new TelegramBotClient(botToken);
        _googleSheetsService = googleSheetsService;
    }

    public void Start()
    {
        var cts = new CancellationTokenSource();
        _botClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync, cancellationToken: cts.Token);
        Console.WriteLine("Бот запущен.");
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type != UpdateType.Message || update.Message?.Text == null) return;

        var chatId = update.Message.Chat.Id;
        var messageText = update.Message.Text.Trim();

        if (messageText.StartsWith("/settable"))
        {
            var parts = messageText.Split(' ', 2);
            if (parts.Length < 2)
            {
                await botClient.SendTextMessageAsync(chatId, "Используйте команду: /settable <URL Google таблицы>");
                return;
            }

            var spreadsheetUrl = parts[1].Trim();
            try
            {
                _googleSheetsService.SetSpreadsheetByUrl(spreadsheetUrl);
                _userSheets[chatId] = spreadsheetUrl;
                await botClient.SendTextMessageAsync(chatId, "Google таблица установлена успешно!");
            }
            catch (Exception ex)
            {
                await botClient.SendTextMessageAsync(chatId, $"Ошибка: {ex.Message}");
            }
        }
        else if (messageText == "/gettable")
        {
            if (_userSheets.TryGetValue(chatId, out var sheetUrl))
            {
                await botClient.SendTextMessageAsync(chatId, $"Текущая таблица: {sheetUrl}");
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId, "Вы не установили Google таблицу. Используйте команду /settable <URL>");
            }
        }
        else
        {
            await botClient.SendTextMessageAsync(chatId, "Неизвестная команда. Доступные команды: \n/settable <URL> - Установить Google таблицу\n/gettable - Получить текущий URL таблицы");
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Ошибка в боте: {exception.Message}");
        return Task.CompletedTask;
    }
}

