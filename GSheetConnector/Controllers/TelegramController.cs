using GSheetConnector.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;
using Update = GSheetConnector.Models.Update;

namespace GSheetConnector.Controllers
{
    public class TelegramController : ControllerBase
    {
        private readonly ITelegramService _telegramService;

        public TelegramController(ITelegramService telegramService)
        {
            _telegramService = telegramService;
        }

        public async Task ProcessUpdateAsync(Update update)
        {
            if (update.Message != null)
            {
                var messageText = update.Message.Text;

                await _telegramService.ProcessCommandAsync(update.Message.Chat.Id, messageText);
            }
        }
    }
}
