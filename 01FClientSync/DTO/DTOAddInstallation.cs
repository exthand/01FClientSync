using System;
using System.Collections.Generic;
using System.Text;

namespace GetBankStatements.DTO
{
    public class DTOAddInstallation
    {
        public string label { get; set; }
        public string clientReference { get; set;}
        public bool inClientName { get; set;}
        public bool skipNotification { get; set; }
        public string clientUrl { get; set; }
        public string intermediaryEmail { get; set; }
    }
}
