namespace GSheetConnector.Attributs
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ColumnNameAttribute(string Name, bool IsReadOnlyPropert = false, string? ALias = null) : Attribute
    {

    }
}
