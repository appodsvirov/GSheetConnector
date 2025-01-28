using Telegram.Bot.Types;
using GSheetConnector.Models;
using Update = GSheetConnector.Models.Update;

namespace GSheetConnector.Interfaces
{
    public interface ITelegramService
    {
        Task ProcessCommandAsync(long chatId, string command);
        Task SendMessageAsync(long chatId, string text);
        Task<List<Update>> GetUpdatesAsync(int offset);
    }
}
