namespace GSheetConnector.Models
{
    public class ArticleModel
    {
        public int Number { get; set; }
        public string? Period { get; set; }
        public DateTime DateTime { get; set; }
        public CodeType CodeType { get; set; }
        public string? Comment { get; set; }
        public Code? Article { get; set; }
        public Card Card { get; set; }
    }
}
