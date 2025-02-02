using System.Reflection;
using GSheetConnector.Attributs;
using GSheetConnector.Interfaces;
using GSheetConnector.Models.GoogleTables;

namespace GSheetConnector.Services
{
    public class ArticleSheetParser: ISheetParser<ArticleModel>
    {
        public List<ArticleModel> ParseSheet(IList<IList<object>> sheet)
        {
            if (sheet == null || sheet.Count < 2) return new List<ArticleModel>();

            var header = sheet[0].Select(x => x.ToString()).ToList();
            var properties = typeof(ArticleModel).GetProperties()
                .Where(p => p.GetCustomAttribute<ColumnNameAttribute>() != null)
                .ToDictionary(p => p.GetCustomAttribute<ColumnNameAttribute>()!.Name, p => p);

            var result = new List<ArticleModel>();
            for (int i = 1; i < sheet.Count; i++)
            {
                var row = sheet[i];
                var model = new ArticleModel();

                for (int j = 0; j < row.Count; j++)
                {
                    if (header.Count <= j || !properties.TryGetValue(header[j], out var prop))
                        continue;

                    object value = row[j];
                    if (prop.PropertyType == typeof(int) && int.TryParse(value.ToString(), out int intVal))
                        prop.SetValue(model, intVal);
                    else if (prop.PropertyType == typeof(string))
                        prop.SetValue(model, value.ToString());
                    else if (prop.PropertyType == typeof(DateTime) && DateTime.TryParse(value.ToString(), out DateTime dateVal))
                        prop.SetValue(model, dateVal);
                    else if (prop.PropertyType == typeof(CodeType) && Enum.TryParse(value.ToString(), out CodeType codeTypeVal))
                        prop.SetValue(model, codeTypeVal);
                    else if (prop.PropertyType == typeof(decimal) && decimal.TryParse(value.ToString(), out decimal decimalVal))
                        prop.SetValue(model, decimalVal);
                }

                result.Add(model);
            }
            return result;
        }

        public void Merge(List<ArticleModel> articles, List<ArticleModel> statements)
        {
            var existingEntries = new HashSet<(DateTime, decimal)>(articles.Select(a => (a.DateTime, a.Sum)));
            var newEntries = statements.Where(s => !existingEntries.Contains((s.DateTime, s.Sum))).ToList();
            articles.AddRange(newEntries);
            articles.Sort((a, b) => a.DateTime.CompareTo(b.DateTime));
        }
    }
}
