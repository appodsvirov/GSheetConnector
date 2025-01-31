namespace GSheetConnector.Models
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text.RegularExpressions;

    public class Transaction
    {
        public DateTime OperationDateTime { get; set; }
        public DateTime ChargeDate { get; set; }
        public decimal OperationAmount { get; set; }
        public decimal CardAmount { get; set; }
        public string Description { get; set; }
        public string CardNumber { get; set; }

    }

}
