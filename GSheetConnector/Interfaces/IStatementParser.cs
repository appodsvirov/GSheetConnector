using GSheetConnector.Models;

namespace GSheetConnector.Interfaces;

public interface IStatementParser
{
    public IEnumerable<Transaction> ParseTransactions(string text);
}