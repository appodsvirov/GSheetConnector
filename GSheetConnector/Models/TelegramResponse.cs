namespace GSheetConnector.Models
{
    public class TelegramResponse
    {
        public bool Ok { get; set; }
        public List<Update> Result { get; set; }
    }
}
