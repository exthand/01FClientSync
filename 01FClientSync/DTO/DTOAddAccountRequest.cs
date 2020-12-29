using System;
using System.Collections.Generic;
using System.Text;

namespace GetBankStatements.DTO
{
    public class DTOAddAccountRequest
    {
        public int connectorId { get; set; }
        public string iban { get; set;}

    }
}
