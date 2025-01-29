namespace GSheetConnector.Models.GoogleTables
{
    public class Table : List<Dictionary<string, string?>>
    {
        public string Name { get; set; }
        private List<string> _headers = new();

        public Table() { }

        public Table(IList<IList<object>> data)
        {
            if (data != null && data.Count > 0)
            {
                // Заполняем заголовки из первой строки
                _headers = data[0].Select(cell => cell?.ToString() ?? string.Empty).ToList();

                // Преобразуем оставшиеся строки в словари
                for (int i = 1; i < data.Count; i++)
                {
                    var row = data[i];
                    var rowDict = new Dictionary<string, string?>();

                    for (int j = 0; j < _headers.Count; j++)
                    {
                        var header = _headers[j];
                        var value = j < row.Count ? row[j]?.ToString() : string.Empty;

                        rowDict[header] = value;
                    }

                    this.Add(rowDict);
                }
            }
        }

        // Метод для получения заголовковqq
        public List<string> GetHeaders()
        {
            return _headers;
        }
    }

}
