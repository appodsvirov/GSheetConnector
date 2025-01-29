namespace GSheetConnector.Models.TelegramBot
{
    public class TelegramMessage
    {
        public long MessageId { get; set; }
        public TelegramUser? From { get; set; }
        public TelegramChat? Chat { get; set; }
        public string? Text { get; set; }
        public TelegramDocument? Document { get; set; }
        public TelegramPhoto[]? Photo { get; set; }
    }
}
