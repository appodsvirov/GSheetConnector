namespace GSheetConnector.Models.TelegramBot;
public class UserSheetStore
{
    private readonly Dictionary<long, string> _userSheets = new();

    public void SetSheet(long chatId, string sheetUrl)
    {
        _userSheets[chatId] = sheetUrl;
    }

    public string? GetSheet(long chatId)
    {
        return _userSheets.TryGetValue(chatId, out var sheet) ? sheet : null;
    }
}
