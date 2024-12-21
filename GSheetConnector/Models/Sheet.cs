namespace GSheetConnector.Models
{
    public class Sheet: List<List<string?>>
    {
        public string Name { get; set; }

        public Sheet()
        {
            
        }

        public Sheet(IList<IList<object>> data)
        {
            if (data != null)
            {
                foreach (var row in data)
                {
                    var stringRow = row.Select(cell => cell?.ToString() ?? string.Empty).ToList();
                    this.Add(stringRow);
                }
            }
        }
    }
}
