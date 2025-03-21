using System.Reflection;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Sheets.v4;
using GSheetConnector.Interfaces;
using GSheetConnector.Models.GoogleTables;
using GSheetConnector.Attributs;
using System.Text.RegularExpressions;

namespace GSheetConnector.Services;

public class GoogleSheetsService
{
    private readonly SheetsService _sheetsService;
    private string _spreadsheetId;
    private readonly ISheetParser<ArticleModel> _articleParser = new ArticleSheetParser();

    public GoogleSheetsService(string credentialsPath)
    {
        using var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read);
        var credential = GoogleCredential.FromStream(stream)
            .CreateScoped(SheetsService.Scope.Spreadsheets);

        _sheetsService = new SheetsService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Google Sheets API Example"
        });
    }

    public void SetSpreadsheetByUrl(string url)
    {
        var match = Regex.Match(url, @"/spreadsheets/d/([a-zA-Z0-9-_]+)");
        if (!match.Success)
            throw new ArgumentException("Некорректный URL Google Таблицы.");

        _spreadsheetId = match.Groups[1].Value;
    }

    public async Task<IList<IList<object>>> ReadRangeAsync(string range)
    {
        var request = _sheetsService.Spreadsheets.Values.Get(_spreadsheetId, range);
        var response = await request.ExecuteAsync();
        return response.Values;
    }

    public async Task UpdateArticleAsync(List<ArticleModel> statements)
    {
        var sheetName = "Article";
        var listSheets = await GetSheetNamesAsync();

        if (!listSheets.Contains(sheetName))
        {
            Console.WriteLine($"Лист {sheetName} не найден.");
            return;
        }

        var headers = await ReadHeadersAsync(sheetName);
        if (headers.Count == 0)
        {
            Console.WriteLine("Не удалось прочитать заголовки колонок.");
            return;
        }

        await AppendArticlesToSheetAsync(statements, sheetName, headers);
    }

    public async Task<List<string>> ReadHeadersAsync(string sheetName)
    {
        try
        {
            var request = _sheetsService.Spreadsheets.Values.Get(_spreadsheetId, $"{sheetName}!1:1");
            var response = await request.ExecuteAsync();
            return response.Values.FirstOrDefault()?.Select(h => h.ToString()).ToList() ?? new List<string>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при чтении заголовков: {ex.Message}");
            return new List<string>();
        }
    }

    public async Task AppendArticlesToSheetAsync(List<ArticleModel> articles, string sheetName, List<string> headers)
    {
        try
        {
            var properties = typeof(ArticleModel).GetProperties()
                .Select(p => new
                {
                    Property = p,
                    Attribute = p.GetCustomAttribute<ColumnNameAttribute>()
                })
                .Where(p => p.Attribute != null && !p.Attribute.IsReadOnlyPropert)
                .ToList();

            var propertyMap = properties
                .SelectMany(p => new[] { p.Attribute.Name, p.Attribute.Alias })
                .Where(name => !string.IsNullOrEmpty(name) && headers.Contains(name))
                .ToDictionary(name => name, name => properties.First(p => p.Attribute.Name == name || p.Attribute.Alias == name).Property);

            var values = articles.Select(article => headers
                .Select(header => propertyMap.TryGetValue(header, out var property) ? property.GetValue(article) ?? "" : "")
                .ToList() as IList<object>)
                .ToList();

            var range = $"{sheetName}!A2";
            var valueRange = new ValueRange { Values = values };

            var appendRequest = _sheetsService.Spreadsheets.Values.Append(valueRange, _spreadsheetId, range);
            appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;

            await appendRequest.ExecuteAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при добавлении данных в Google Sheets: {ex.Message}");
        }
    }

    private async Task<List<string>> GetSheetNamesAsync()
    {
        try
        {
            var request = _sheetsService.Spreadsheets.Get(_spreadsheetId);
            var response = await request.ExecuteAsync();

            return response.Sheets.Select(sheet => sheet.Properties.Title).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при получении списка листов: {ex.Message}");
            return new List<string>();
        }
    }
}

