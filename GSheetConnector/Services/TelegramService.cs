using GSheetConnector.Interfaces;
using GSheetConnector.Models;
using Telegram.Bot;

namespace GSheetConnector.Services
{
    public class TelegramService: ITelegramService
    {
        private readonly string _botToken = "";
        private readonly HttpClient _httpClient;

        public TelegramService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task ProcessCommandAsync(long chatId, string command)
        {
            switch (command)
            {
                case "/start":
                    await SendMessageAsync(chatId, "Добро пожаловать! Я помогу вам с расходами.");
                    break;
                case "/help":
                    await SendMessageAsync(chatId, "Список доступных команд: /start, /help");
                    break;
                default:
                    await SendMessageAsync(chatId, "Неизвестная команда.");
                    break;
            }
        }

        public async Task SendMessageAsync(long chatId, string text)
        {
            var url = $"https://api.telegram.org/bot{_botToken}/sendMessage";
            var payload = new
            {
                chat_id = chatId,
                text = text
            };

            await _httpClient.PostAsJsonAsync(url, payload);
        }

        public async Task<List<Update>> GetUpdatesAsync(int offset)
        {
            var url = $"https://api.telegram.org/bot{_botToken}/getUpdates?offset={offset}";
            var response = await _httpClient.GetFromJsonAsync<TelegramResponse>(url);
            return response?.Result ?? new List<Update>();
        }
    }
}
