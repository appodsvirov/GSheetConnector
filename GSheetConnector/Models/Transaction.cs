namespace GSheetConnector.Models
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text.RegularExpressions;

    public class Transaction
    {
        public DateTime OperationDate { get; set; }  // Дата и время операции
        public DateTime DebitDate { get; set; }      // Дата и время списания
        public decimal Amount { get; set; }          // Сумма в валюте операции
        public decimal AmountInCardCurrency { get; set; } // Сумма операции в валюте карты
        public string Description { get; set; } = ""; // Описание операции
        public string CardNumber { get; set; }       // Последние 4 цифры карты
    }


}
