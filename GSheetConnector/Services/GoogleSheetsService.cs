using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Sheets.v4;
using GSheetConnector.Interfaces;
using GSheetConnector.Models.GoogleTables;

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

    /// <summary>
    /// Метод, который ищет первую незаполненную строку на указанном листе и возвращает её адрес в виде строки:
    /// </summary>
    public async Task<string> GetFirstEmptyRowAddressAsync(string sheetName, string column = "A")
    {
        try
        {
            // Считываем весь лист
            var values = await ReadEntireSheetAsync(sheetName);

            // Находим первую пустую строку в указанной колонке
            int rowIndex = 0;
            while (rowIndex < values.Count && rowIndex < 1000) // Ограничение в 1000 строк
            {
                var cellValue = rowIndex < values.Count && values[rowIndex].Count > 0
                    ? values[rowIndex][0]?.ToString()
                    : null;

                if (string.IsNullOrEmpty(cellValue))
                {
                    break;
                }
                rowIndex++;
            }

            // Возвращаем адрес первой пустой строки
            return $"{sheetName}!{column}{rowIndex + 1}";  // Строки начинаются с 1
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при поиске первой пустой строки: {ex.Message}");
            return string.Empty;
        }
    }


    public async Task UpdateCellAsync(string range, string value)
    {
        var valueRange = new ValueRange
        {
            Values = new[] { new[] { value } }
        };

        var updateRequest = _sheetsService.Spreadsheets.Values.Update(valueRange, _spreadsheetId, range);
        updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;

        await updateRequest.ExecuteAsync();
    }

    public async Task<IList<IList<object>>> ReadRangeAsync(string range)
    {
        var request = _sheetsService.Spreadsheets.Values.Get(_spreadsheetId, range);
        var response = await request.ExecuteAsync();
        return response.Values;
    }



    public async Task UpdateCellWithCurrentDateTimeAsync(string range)
    {
        // Получаем текущую дату и время
        var currentDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        var valueRange = new ValueRange
        {
            Values = new[] { new[] { currentDateTime } }
        };

        // Выполняем обновление указанного диапазона
        var updateRequest = _sheetsService.Spreadsheets.Values.Update(valueRange, _spreadsheetId, range);
        updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;

        // Выполняем запрос
        await updateRequest.ExecuteAsync();
    }

    // TODO
    public async Task UpdateArticleAsync(List<ArticleModel> statements)
    {
        var sheetName = "Article";
        var listSheets = await GetSheetNamesAsync();
        var sheet = await ReadEntireSheetAsync(listSheets.FirstOrDefault(x => x == sheetName));
        var articles = _articleParser.ParseSheet(sheet);

        _articleParser.Merge(articles, statements);
        await UpdateArticlesToSheetAsync(articles, sheetName);
    }


    public async Task UpdateArticlesToSheetAsync(List<ArticleModel> articles, string sheetName)
    {
        try
        {
            var range = $"{sheetName}!A2"; // Начинаем со второй строки, первая строка остается заголовком

            var values = new List<IList<object>>();
            foreach (var article in articles)
            {
                values.Add(new List<object>
                {
                    article.Number,
                    article.Period,
                    article.DateTime.ToString(),
                    article.CodeType.ToString(),
                    article.Comment,
                    article.Article?.ToString() ?? "",
                    article.Card?.ToString() ?? "",
                    article.Sum
                });
            }

            var valueRange = new ValueRange
            {
                Values = values
            };

            var appendRequest = _sheetsService.Spreadsheets.Values.Append(valueRange, _spreadsheetId, range);
            appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;

            await appendRequest.ExecuteAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при добавлении данных в Google Sheets: {ex.Message}");
        }
    }


    /// <summary>
    /// Возвращает список всех листов 
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


    /// <summary>
    /// Метод для считывания всего листа sheetName
    /// </summary>
    public async Task<IList<IList<object>>> ReadEntireSheetAsync(string sheetName)
    {
        try
        {
            // Указываем диапазон как "SheetName"
            var request = _sheetsService.Spreadsheets.Values.Get(_spreadsheetId, sheetName);
            var response = await request.ExecuteAsync();

            // Если данных нет, возвращаем пустой список
            return response.Values ?? new List<IList<object>>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при чтении листа {sheetName}: {ex.Message}");
            return new List<IList<object>>();
        }
    }
}

