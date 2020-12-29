using System;
using System.Collections.Generic;
using System.Text;

namespace GetBankStatements.DTO
{
    public class DTOClient
    {
        public Guid id { get; set; }
        public DateTime createdAt { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        public string vatid { get; set; }
        public int status { get; set; }
        public string statusText { get; set; }
    }
}
