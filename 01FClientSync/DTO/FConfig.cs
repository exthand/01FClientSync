using System;
using System.Collections.Generic;
using System.Text;

namespace GetBankStatements.DTO
{
    public class FConfig
    {
        public string clientName { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Path { get; set; }
        public string Frequence { get; set; }
        public int FileType { get; set; }
        public int PathPersist { get; set; }


        public List<Connection> connections { get; set; }

    }

    public class Connection
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public List<BankAccount> bankAccounts { get; set; }
    }

    public class BankAccount
    {
        public Guid Id { get; set; }
        public string IBAN { get; set; }
        public int Sequence { get; set; }
        public DateTime LastRun { get; set; }
    }
}
