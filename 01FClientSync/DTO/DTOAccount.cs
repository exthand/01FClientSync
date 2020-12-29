using System;
using System.Collections.Generic;
using System.Text;

namespace GetBankStatements.DTO
{
    public class DTOAccount
    {
        public Guid id { get; set; }
        public string currency { get; set; }
        public string iban { get; set; }
        public string description { get; set; }
        public DateTime validUntil { get; set; }

        public string consentGrouping { get; set; }

        public DTOFinancialInstitution financialInstitution { get; set; }
    }

    public class DTOFinancialInstitution
    {
        public Guid id { get; set; }
        public string fullname { get; set; }
        public int connectorId { get; set; }

        public string country { get; set; }
    }
}
