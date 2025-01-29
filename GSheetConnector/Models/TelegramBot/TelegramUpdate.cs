namespace GSheetConnector.Models.TelegramBot
{
    public class TelegramUpdate
    {
        public long UpdateId { get; set; }
        public TelegramMessage? Message { get; set; }
    }
}
