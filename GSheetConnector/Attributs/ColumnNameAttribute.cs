namespace GSheetConnector.Attributs
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ColumnNameAttribute(string name, bool isReadOnlyPropert = false, string? alias = null)
        : Attribute
    {
        public string Name { get; } = name;
        public bool IsReadOnlyPropert { get; } = isReadOnlyPropert;
        public string? Alias { get; } = alias;
    }

}
