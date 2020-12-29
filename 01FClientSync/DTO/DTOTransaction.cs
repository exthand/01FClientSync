using System;
using System.Collections.Generic;
using System.Text;

namespace GetBankStatements.DTO
{
    public class DTOTransactionContainer
    {
        public List<DTOTransaction> transactions { get; set; }
        public int page { get; set; }
        public int size { get; set; }
        public int total { get; set; }
        public DateTime dateFrom { get; set; }
    }

    public class DTOTransaction
    {
        public Guid id { get; set; }
        public decimal amount { get; set; }
        public string counterpartReference { get; set; }
        public string counterpartName { get; set; }
        public string description { get; set; }
        public DateTime executionDate { get; set; }
        public DateTime valueDate { get; set; }
        public DateTime requestedAt { get; set; }

        public Guid accountID { get; set; }

        public string currency { get; set; }

    }
}
