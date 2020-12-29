using System;
using System.Collections.Generic;
using System.Text;

namespace GetBankStatements.DTO
{
    public class DTOAddConnection
    {
        public string label { get; set; }
        public string psuEmail { get; set;}
        public bool skipNotification { get; set; }
        public string clientUrl { get; set; }

    }
}
