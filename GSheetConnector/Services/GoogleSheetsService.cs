using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Sheets.v4;

namespace GSheetConnector.Services;

public class GoogleSheetsService
{
    private readonly SheetsService _sheetsService;
    private readonly string _spreadsheetId;

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
    /// Возвращает список всех листов 
    /// </summary>
    public async Task<List<string>> GetSheetNamesAsync()
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
    /// Метод для считывания всего листа
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

    // Метод для нахождения первой пустой ячейки в первой колонке и обновления её текущей датой и временем
    public async Task UpdateFirstEmptyCellInColumnAsync()
    {
        // Читаем значения из первой колонки (A)
        var listSheets = await GetSheetNamesAsync();
        var sheet = await ReadEntireSheetAsync(listSheets.FirstOrDefault());
        var range = "Лист1!A1:C4"; // Укажите ваш лист и колонку
        var values = await ReadRangeAsync(range);

        // Находим первую пустую ячейку
        int rowIndex = 0;
        while (rowIndex < values.Count && values.Count() > rowIndex && values[rowIndex].Any(cell => !string.IsNullOrEmpty(cell?.ToString())))
        {
            rowIndex++;
        }

        if (rowIndex < values.Count)
        {
            // Обновляем первую пустую ячейку с текущей датой и временем
            var cellRange = $"Sheet1!A{rowIndex + 1}"; // Индексы в Google Sheets начинаются с 1
            await UpdateCellWithCurrentDateTimeAsync(cellRange);
        }
        else
        {
            // Если все ячейки заполнены, добавляем значение в следующую строку
            var cellRange = $"Sheet1!A{values.Count + 1}";
            await UpdateCellWithCurrentDateTimeAsync(cellRange);
        }
    }

}

