namespace GSheetConnector.Attributs
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ColumnNameAttribute : Attribute
    {
        public string Name { get; }
        public string? Alias { get; }

        public ColumnNameAttribute(params string[] names)
        {
            Name = names[0];
            Alias = names.Length >= 2? names[1]: null;
        }
    }
}
