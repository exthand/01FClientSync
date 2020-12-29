using System;
using System.Collections.Generic;
using System.Text;

namespace GetBankStatements.DTO
{
    public class DTOConnection
    {
        public Guid Id { get; set; }

        public Guid InstallationId { get; set; }
        public string Label { get; set; }
        public int status { get; set; }

        public int AccountCount { get; set; }
        public string Link { get; set; }

        public DTOClient CompanyDTO { get; set; }

    }
}
