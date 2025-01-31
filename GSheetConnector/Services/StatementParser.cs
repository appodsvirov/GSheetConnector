using System.Globalization;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.RegularExpressions;
using GSheetConnector.Models;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;

namespace GSheetConnector.Services
{
    public class StatementParser
    {
        /// <summary>
        /// Считывает текст из PDF файла.
        /// </summary>
        /// <param name="filePath">Путь к PDF файлу.</param>
        /// <returns>Текст из PDF файла.</returns>
        /// <exception cref="FileNotFoundException">Если файл не найден.</exception>
        public string ReadPdf(string filePath)
        {
            if (!File.Exists(filePath))
            {
                //throw new FileNotFoundException("PDF файл не найден", filePath);
                return "";
            }

            var textBuilder = new StringBuilder();

            using (var pdfReader = new PdfReader(filePath))
            using (var pdfDocument = new PdfDocument(pdfReader))
            {
                for (int page = 1; page <= pdfDocument.GetNumberOfPages(); page++)
                {
                    var pageText = PdfTextExtractor.GetTextFromPage(pdfDocument.GetPage(page));
                    textBuilder.AppendLine(pageText);
                }
            }

            return textBuilder.ToString();
        }

        public IEnumerable<Transaction> ParseTransactions(string text)
        {
            var transactions = new List<Transaction>();
            var lines = text.Split(new[] { Environment.NewLine, "\n" }, StringSplitOptions.RemoveEmptyEntries);
            var currentTransactionLines = new List<string>();
            var transactionStartPattern = @"^\d{2}\.\d{2}\.\d{4}"; // Начало транзакции (дата)
            var transactionEndPattern = @"\d{4}$";                 // Конец транзакции (4 цифры номера карты)

            foreach (var line in lines)
            {
                if (Regex.IsMatch(line, transactionStartPattern)) // Начало новой транзакции
                {
                    if (currentTransactionLines.Count > 0) // Обработка предыдущей транзакции
                    {
                        if (TryParseTransaction(currentTransactionLines, out var transaction))
                        {
                            transactions.Add(transaction);
                        }
                        currentTransactionLines.Clear();
                    }
                }

                currentTransactionLines.Add(line);

                if (Regex.IsMatch(line, transactionEndPattern)) // Конец текущей транзакции
                {
                    if (TryParseTransaction(currentTransactionLines, out var transaction))
                    {
                        transactions.Add(transaction);
                    }
                    currentTransactionLines.Clear();
                }
            }

            // Обработка последней транзакции
            if (currentTransactionLines.Count > 0)
            {
                if (TryParseTransaction(currentTransactionLines, out var transaction))
                {
                    transactions.Add(transaction);
                }
            }

            return transactions;
        }

        private static bool TryParseTransaction(List<string> transactionLines, out Transaction transaction)
        {
            transaction = null;
            try
            {
                var fullText = string.Join(" ", transactionLines); // Соединяем строки в один текст
                var pattern = @"(?<opDate>\d{2}\.\d{2}\.\d{4})\s+(?<chDate>\d{2}\.\d{2}\.\d{4})\s+(?<opAmount>[+-]?\d{1,3}(?:\s?\d{3})*(?:,\d{2})?\s₽)\s+(?<cardAmount>[+-]?\d{1,3}(?:\s?\d{3})*(?:,\d{2})?\s₽)\s+(?<description>.+?)\s+(?<cardNumber>\d{4})";

                var match = Regex.Match(fullText, pattern);
                if (!match.Success)
                {
                    return false;
                }

                transaction = new Transaction
                {
                    OperationDateTime = DateTime.ParseExact(
                        $"{match.Groups["opDate"].Value} {transactionLines[0].Split(' ')[1]}",
                        "dd.MM.yyyy HH:mm",
                        CultureInfo.InvariantCulture),
                    ChargeDate = DateTime.ParseExact(
                        $"{match.Groups["chDate"].Value} {transactionLines[0].Split(' ')[2]}",
                        "dd.MM.yyyy HH:mm",
                        CultureInfo.InvariantCulture),
                    OperationAmount = decimal.Parse(match.Groups["opAmount"].Value.Replace("₽", "").Trim(), CultureInfo.InvariantCulture),
                    CardAmount = decimal.Parse(match.Groups["cardAmount"].Value.Replace("₽", "").Trim(), CultureInfo.InvariantCulture),
                    Description = match.Groups["description"].Value.Trim(),
                    CardNumber = match.Groups["cardNumber"].Value.Trim()
                };

                return true;
            }
            catch
            {
                return false;
            }
        }

    }
}
