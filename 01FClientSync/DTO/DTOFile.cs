using System;
using System.Collections.Generic;
using System.Text;

namespace GetBankStatements.DTO
{
    public class DTOFile
    {
        public int transactionCount { get; set; }
        public int sequenceNumber { get; set; }
        public DateTime requestedPeriodFrom { get; set; }
        public int periodType { get; set; }
        public string periodTypeText { get; set; }
        public DateTime dateFrom { get; set; }
        public DateTime dateTo { get; set; }

        public string[] coda { get; set; }
        public string camt { get; set; }
        public string html { get; set; }
        public bool resultTruncated { get; set; }
    }
}
