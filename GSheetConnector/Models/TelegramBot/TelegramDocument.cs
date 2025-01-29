namespace GSheetConnector.Models.TelegramBot
{
    public class TelegramDocument
    {
        public string? FileId { get; set; }
        public string? FileName { get; set; }
        public string? MimeType { get; set; }
        public long FileSize { get; set; }
    }
}
