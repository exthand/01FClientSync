using System;
using System.Collections.Generic;
using System.Text;

namespace GetBankStatements.DTO
{
    public class DTOBank
    {
        public Guid id { get; set; }
        public string fullname { get; set; }
        public int connectorId { get; set; }
        public DTOCountry country { get; set; }
    }
}
