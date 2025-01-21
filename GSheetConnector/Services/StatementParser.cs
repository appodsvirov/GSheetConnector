﻿using System.Reflection.PortableExecutable;
using System.Text;
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
                throw new FileNotFoundException("PDF файл не найден", filePath);
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
    }
}
