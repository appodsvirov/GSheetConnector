using GSheetConnector.Models.GoogleTables;

namespace GSheetConnector.Interfaces
{
    public interface ISheetParser<T>
    {
        public List<T> ParseSheet(IList<IList<object>> sheet);
        public void Merge(List<T> first, List<T> second);
    }
}
