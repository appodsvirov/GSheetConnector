namespace GSheetConnector.Models
{
    public enum CodeType
    {
        Revenue,
        Cost
    }

    public class Code
    {
        public string? Name { get; set; }
        public CodeType Type { get; set; }
    }
}
