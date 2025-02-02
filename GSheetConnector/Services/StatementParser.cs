using System.Globalization;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.RegularExpressions;
using GSheetConnector.Interfaces;
using GSheetConnector.Models;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;


//TODO
// 1) Все методы с Parse заменить на TryParse, избегая исключений
// 2) Сделать проверки данных
// 3) Добавить лог

namespace GSheetConnector.Services
{
    public class StatementParser: IStatementParser
    {
        /// <summary>
        /// Метод для парсинга text и извлечения всех транзакций из него
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public IEnumerable<Transaction> ParseTransactions(string text)
        {
            var transactions = new List<Transaction>();
            var lines = text.Split('\n');
            var firstLineTransactionRegex = new Regex(@"\d{2}\.\d{2}\.\d{4}\s+\d{2}\.\d{2}\.\d{4}.*");

            Transaction? currentTransaction = new();

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var match = firstLineTransactionRegex.Match(line);

                if (match.Success)
                {
                    var normalizeLine = NormalizeCurrencySpacing(line);
                    var splitLine = normalizeLine.Split(" ");

                    var nextLine = lines[++i];

                    if (i >= lines.Length)
                    {
                        return transactions;
                    }

                    var splitNextLine = nextLine.Split(" ");


                    currentTransaction = ParseTransaction(splitLine, splitNextLine);


                    while (true)
                    {
                        nextLine = lines[++i];

                        if (nextLine.Contains("Пополнения:"))
                        {
                            transactions.Add(currentTransaction);
                            return transactions;
                        }

                        if (i >= lines.Length)
                        {
                            return transactions;
                        }

                        // Если каждая следующая строка не начало следующей транзакции, значит она является продолжением описания текущей транзакции
                        if (!firstLineTransactionRegex.Match(nextLine).Success)
                        {
                            currentTransaction.Description += nextLine;
                        }
                        else
                        {
                            i--;
                            transactions.Add(currentTransaction);
                            break;

                        }
                    }
                }
                else if (currentTransaction != null)
                {

                }
            }

            return transactions;
        }

        /// <summary>
        /// В строке с суммой между символами [+-] и ₽ удаляет все пробелы и удаляет знак ₽ 
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private static string NormalizeCurrencySpacing(string line)
        {
            return Regex.Replace(line, @"([+-])\s*([\d\s.,]+)\s*(₽)", m =>
                $"{m.Groups[1].Value}{m.Groups[2].Value.Replace(" ", "").Replace("\u20bd", "")}{m.Groups[3].Value}");
        }

        /// <summary>
        /// Формирование транзакции по двум строкам. Следующие строки, если они имеются -- это продолжение Description
        /// </summary>
        /// <param name="splitLine"></param>
        /// <param name="splitNextLine"></param>
        /// <returns></returns>
        private static Transaction? ParseTransaction(string[] splitLine, string[] splitNextLine)
        {
            try
            {
                var culture = new CultureInfo("ru-RU");

                // Формируем DateTime-ы c с учетом, что даты и время находятся в разных строках
                DateTime operationDateTime = DateTime.ParseExact(
                    splitLine[0] + " " + splitNextLine[0], "dd.MM.yyyy HH:mm", culture);

                DateTime debitDateTime = DateTime.ParseExact(
                    splitLine[1] + " " + splitNextLine[1], "dd.MM.yyyy HH:mm", culture);

                return new Transaction
                {
                    OperationDate = operationDateTime,
                    DebitDate = debitDateTime,
                    Amount = ParseCurrency(splitLine[2]),
                    AmountInCardCurrency = ParseCurrency(splitLine[3]),
                    CardNumber = splitLine[^1], // Последний элемент — номер карты
                    Description = string.Join(" ", splitLine[4..^1]) + " " + string.Join(" ", splitNextLine[2..])
                };
            }
            catch
            {
                return null;
            }
        }

        // Получение decimal из string
        private static decimal ParseCurrency(string value)
        {
            string cleanedValue = value.Replace(" ", "").Replace("₽", "").Trim();
            return decimal.Parse(cleanedValue, CultureInfo.InvariantCulture);
        }
    }
}
