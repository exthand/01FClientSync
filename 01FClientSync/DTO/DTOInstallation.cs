using System;
using System.Collections.Generic;
using System.Text;

namespace GetBankStatements.DTO
{
    //public class DTOInstallationList
    //{
    //    public List<DTOInstallation> owned { get; set; }
    //    public List<DTOInstallation> managed { get; set; }
    //}

    public class DTOInstallation
    {
        public Guid id { get; set; }

        public Guid installationId { get; set; }
        public string label { get; set; }
        public DateTime? createdAt { get; set; }
        public DateTime? acceptedAt { get; set; }
        public int status { get; set; }
        public string statusText { get; set; }

        public int connectionCount { get; set; }

        public int accountCount { get; set; }
        public string Link { get; set; }

        public DTOClient client { get; set; }
        public DTOClient manager { get; set; }
        public DTOClient psu { get; set; }

    }
}
