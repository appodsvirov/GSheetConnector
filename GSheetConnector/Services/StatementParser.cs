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


        static string NormalizeCurrencySpacing(string line)
        {
            return Regex.Replace(line, @"([+-])\s*([\d\s.,]+)\s*(₽)", m =>
                $"{m.Groups[1].Value}{m.Groups[2].Value.Replace(" ", "").Replace("\u20bd", "")}{m.Groups[3].Value}");
        }

        static Transaction? ParseTransaction(string[] splitLine, string[] splitNextLine)
        {

            try
            {
                var culture = new CultureInfo("ru-RU");

                // Формируем DateTime с учетом времени
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

        static decimal ParseCurrency(string value)
        {
            // Убираем пробелы, ₽ и другие символы перед конвертацией
            string cleanedValue = value.Replace(" ", "").Replace("₽", "").Trim();
            return decimal.Parse(cleanedValue, CultureInfo.InvariantCulture);
        }
    }
}
