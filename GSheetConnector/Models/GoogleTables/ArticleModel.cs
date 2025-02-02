using GSheetConnector.Attributs;

namespace GSheetConnector.Models.GoogleTables
{
    public class ArticleModel
    {
        [ColumnName("№")] public int Number { get; set; }
        [ColumnName("Period")] public string? Period { get; set; }
        [ColumnName("Date", "Time")] public DateTime DateTime { get; set; }
        [ColumnName("Rev/costs")] public CodeType CodeType { get; set; }
        [ColumnName("Description of the operation")] public string? Description { get; set; }
        [ColumnName("Comments")] public string? Comment { get; set; }
        [ColumnName("Article")] public Code? Article { get; set; } = null;
        [ColumnName("Card")] public Card Card { get; set; } = null;
        [ColumnName("Sum")] public decimal Sum { get; set; }

        public ArticleModel()
        {
            
        }

        public ArticleModel(Transaction transaction)
        {
            DateTime = transaction.OperationDate;
            Sum = Math.Abs(transaction.Amount);
            CodeType = transaction.Amount >= 0 ? CodeType.Revenue : CodeType.Cost;
            Description = transaction.Description;
        }
    }
}
