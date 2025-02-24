using GSheetConnector.Attributs;

namespace GSheetConnector.Models.GoogleTables
{
    public class ArticleModel
    {
        [ColumnName("№", isReadOnlyPropert: true)] public int Number { get; set; }
        [ColumnName("Period", isReadOnlyPropert: true)] public string? Period { get; set; }
        [ColumnName("Date", alias:"Time")] public DateTime DateTime { get; set; }
        [ColumnName("Rev/costs")] public CodeType CodeType { get; set; }
        [ColumnName("Description of the operation")] public string? Description { get; set; }
        [ColumnName("Comments")] public string? Comment { get; set; }
        [ColumnName("Article", isReadOnlyPropert: true)] public Code? Article { get; set; } = null;
        [ColumnName("Card", isReadOnlyPropert: true)] public Card Card { get; set; } = null;
        [ColumnName("Total")] public decimal Sum { get; set; }

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
