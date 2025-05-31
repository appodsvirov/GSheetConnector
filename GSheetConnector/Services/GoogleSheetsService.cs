using System.Reflection;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Sheets.v4;
using GSheetConnector.Interfaces;
using GSheetConnector.Models.GoogleTables;
using GSheetConnector.Attributs;

namespace GSheetConnector.Services;

public class GoogleSheetsService
{
    private readonly SheetsService _sheetsService;
    private readonly string _spreadsheetId;
    private readonly ISheetParser<ArticleModel> _articleParser = new ArticleSheetParser();
    public GoogleSheetsService(string credentialsPath, string spreadsheetId)
    {
        _spreadsheetId = spreadsheetId;

        using var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read);
        var credential = GoogleCredential.FromStream(stream)
            .CreateScoped(SheetsService.Scope.Spreadsheets);

        _sheetsService = new SheetsService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Google Sheets API Example"
        });
    }

    public async Task<IList<IList<object>>> ReadRangeAsync(string range)
    {
        var request = _sheetsService.Spreadsheets.Values.Get(_spreadsheetId, range);
        var response = await request.ExecuteAsync();
        return response.Values;
    }

    // TODO
    public async Task UpdateArticleAsync(List<ArticleModel> statements)
    {
        var sheetName = "Article";
        var listSheets = await GetSheetNamesAsync();

        if (!listSheets.Contains(sheetName))
        {
            Console.WriteLine($"Лист {sheetName} не найден.");
            return;
        }

        // Читаем заголовки (первая строка)
        var headers = await ReadHeadersAsync(sheetName);

        if (headers.Count == 0)
        {
            Console.WriteLine("Не удалось прочитать заголовки колонок.");
            return;
        }

        await AppendArticlesToSheetAsync(statements, sheetName, headers);
    }

    /// <summary>
    /// Возвращает список названий всех листов 
    /// </summary>
    private async Task<List<string>> GetSheetNamesAsync()
    {
        try
        {
            var request = _sheetsService.Spreadsheets.Get(_spreadsheetId);
            var response = await request.ExecuteAsync();

            var sheetNames = response.Sheets
                .Select(sheet => sheet.Properties.Title)
                .ToList();

            return sheetNames;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при получении списка листов: {ex.Message}");
            return new List<string>();
        }
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
                .Where(p => p.Attribute != null && !p.Attribute.IsReadOnlyPropert) // Игнорируем ReadOnly
                .ToList();

            var propertyMap = new Dictionary<string, PropertyInfo>();

            foreach (var prop in properties)
            {
                if (headers.Contains(prop.Attribute.Name))
                    propertyMap[prop.Attribute.Name] = prop.Property;

                if (!string.IsNullOrEmpty(prop.Attribute.Alias) && headers.Contains(prop.Attribute.Alias))
                    propertyMap[prop.Attribute.Alias] = prop.Property;
            }

            var values = new List<IList<object>>();

            foreach (var article in articles)
            {
                var row = new object[headers.Count]; // Создаем строку нужной длины

                foreach (var header in headers)
                {
                    if (propertyMap.TryGetValue(header, out var property))
                    {
                        object? value = property.GetValue(article);

                        if (property.PropertyType == typeof(DateTime))
                        {
                            if (header == "Time")
                                value = ((DateTime)value).ToShortTimeString();
                            else if (header == "Date")
                                value = ((DateTime)value).ToShortDateString();
                        }
                        else if (property.PropertyType == typeof(CodeType))
                        {
                            value = value?.ToString();
                        }

                        row[headers.IndexOf(header)] = value ?? "";
                    }
                }

                values.Add(row);
            }

            var range = $"{sheetName}!A2"; // Начинаем со второй строки
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
}

