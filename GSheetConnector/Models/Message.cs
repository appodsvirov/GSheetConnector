using Telegram.Bot.Types;

namespace GSheetConnector.Models
{
    public class Message
    {
        public long MessageId { get; set; }
        public Chat Chat { get; set; }
        public string Text { get; set; }
    }
}
