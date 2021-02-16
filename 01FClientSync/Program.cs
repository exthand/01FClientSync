using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;

using GetBankStatements.DTO;

namespace _01FClientSync
{
    class Program
    {

        

        //private static string URLAuth = "https://test.digiteal.eu";
        //private static string URLConn = "https://staging-api.01financials.eu";

        private static string URLAuth = "https://api.digiteal.eu";
        private static string URLConn = "https://api.01financials.eu";

        private static FConfig fConfig;
        private static StreamWriter errorLog;
        private static HttpClient httpClient;
        private static bool IsAuthenticated = false;
        private static List<DTOBank> lstBanks;

        static void Main(string[] args)
        {
            bool quit = false;

            errorLog = new StreamWriter( DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log", false, Encoding.UTF8);

            AddTrace("Starting 01Financials Sync.", false, true);
            Console.Title = "01Financials Sync";

            try
            {
                //Loading Config File
                while (!File.Exists("01financials.config"))
                {
                    AddTrace("01financials.config not found.", true, true);
                    AskConfig();
                }

                StreamReader sr = new StreamReader("01financials.config");
                string configContent = sr.ReadToEnd();
                sr.Close();
                sr.Dispose();



                AddTrace("Loading config", false, true);

                fConfig = JsonSerializer.Deserialize<FConfig>(configContent);

                if (!Directory.Exists(fConfig.Path))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Path to storage not found.");
                    Console.ForegroundColor = ConsoleColor.Green;
                    AskConfig();
                }
                AddTrace("01Financials Sync initialized.", false, true);

                FConfig tempConfig = new FConfig()
                {
                    ClientId = fConfig.ClientId,
                    ClientSecret = "*******",
                    clientName = fConfig.clientName,
                    FileType = fConfig.FileType,
                    Frequence = fConfig.Frequence,
                    Path = fConfig.Path
                };

                AddTrace("Config " + JsonSerializer.Serialize<FConfig>(tempConfig), false, true, true);
                tempConfig = null;

                AddTrace("Authenticating...", false, true);
                if (Authenticating())
                    AddTrace("Authentication succeeded.", false, true);
                else
                    AddTrace("Failed to authenticate. Check config.", true, true);


                DisplayHelp();

                string command="";


                if (args!=null && args.Length>0)
                {
                    command = args[0];
                    quit = true;
                }

                do
                {
                    try
                    {
                        if (command=="")
                            command = ReadString("> ");
                        AddTrace("Command " + command.ToUpper(), false, true, true);

                        string[] commands = command.Split(" ");

                        command = commands[0].ToUpper();

                        switch (command)
                        {
                            case "QUIT":
                                quit = true;
                                AddTrace("Exiting.", false, true);
                                break;
                            case "BANKS":
                                ListBanks();
                                break;
                            case "CONFIG":
                                AskConfig();
                                break;
                            case "DCONFIG":
                                DisplayConfig();
                                break;
                            case "LAST":
                                string[] param = CleanArray(commands);
                                ListTransactions(param[1], param[2]);
                                break;
                            case "LIST":
                                ListAccount();
                                break;
                            case "SYNC":
                                Sync();
                                break;
                            case "HELP":
                                DisplayHelp();
                                break;
                            case "ADDCLIENT":
                                AddClient();
                                break;
                            default:
                                DisplayHelp();
                                break;
                        }
                        command = "";
                    }
                    catch (Exception e)
                    {
                        AddTrace(e.Message, true, true);
                    }
                } while (!quit);
            }
            catch(Exception e)
            {
                AddTrace(e.Message, true, true);
            }
            errorLog.Close();
            errorLog.Dispose();
        }

        #region UTILITIES
        
        static string[] CleanArray(string[] inArray)
        {
            string[] outArray = new string[inArray.Length];
            int y = 0;
            for(int z =0; z<inArray.Length;z++)
            {
                if (!string.IsNullOrEmpty(inArray[z]))
                {
                    outArray[y] = inArray[z];
                    y++;
                }
            }
            return outArray;
        }
        
        static void DisplayHelp()
        {
            Console.WriteLine();
            Console.WriteLine("01Financisls commands:");
            Console.WriteLine("help      : display this help.");
            Console.WriteLine();
            //Console.WriteLine("addclient : add a new installation for a new client.");
            Console.WriteLine("banks     : list all banks connected.");
            Console.WriteLine("config    : allows you to enter configuration parameters.");
            Console.WriteLine("dconfig   : displays current configuration parameters.");
            Console.WriteLine("last x y  : displays last X transactions for bank account Y.");
            Console.WriteLine("list      : displays all linked bank accounts.");
            Console.WriteLine("sync      : starts downloading CAMT & CODA files from server.");
            Console.WriteLine("quit      : Closes the app.");
            Console.WriteLine();
        }

        static void AddTrace(string line, bool isError, bool isLogged, bool isSilent = false)
        {

            line = DateTime.Now.ToString("HH:mm:ss") + " | " + line;

            if (isError && !isSilent)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(line);
                Console.ForegroundColor = ConsoleColor.DarkGreen;
            }
            if (!isError && !isSilent)
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine(line);
            }

            if (isLogged)
            {
                errorLog.WriteLine(line);
                errorLog.Flush();
            }
        }
        static string ReadString(string textmessage)
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Write(textmessage);
            Console.ForegroundColor = ConsoleColor.Green;
            string rvalue = Console.ReadLine();
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            return rvalue;
        }

        static string ReadEmail(string textmessage)
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Write(textmessage);
            Console.ForegroundColor = ConsoleColor.Green;
            string rvalue = Console.ReadLine();
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            return rvalue;
        }

        static int ReadInt(string textmessage)
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Write(textmessage);
            Console.ForegroundColor = ConsoleColor.Green;
            int rvalue = int.Parse(Console.ReadLine());
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            return rvalue;
        }

        static bool ReadBool(string textmessage)
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Write(textmessage + " [Y/N] ");
            Console.ForegroundColor = ConsoleColor.Green;
            bool cont = false;
            bool rValue = false;
            do
            {
                string yesno = Console.ReadLine();
                if (yesno.ToUpper() == "Y" || yesno.ToUpper()=="N")
                {
                    cont = true;
                    if (yesno.ToUpper() == "Y")
                        rValue = true;
                }
            } while (!cont);
            
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            return rValue;
        }

        static void AskConfig()
        {
            bool cont = false;
            fConfig = new FConfig();
            Console.WriteLine();
            AddTrace("We need your input before going ahead", false, true);
            fConfig.ClientId = ReadString("Your cliend ID    : ");
            fConfig.ClientSecret = ReadString("Your cliend Secret: ");
            do
            {
                fConfig.Path = ReadString("Path to storage   : ");
                if (Directory.Exists(fConfig.Path))
                    cont = true;
                else
                {
                    AddTrace("Path not found.", true, true, false);
                }
            } while (!cont);

            Console.WriteLine("File naming :");
            Console.WriteLine(" 0 : Your Path\\CONNECTION\\SEQUENCE-DATE-IBAN");
            Console.WriteLine(" 1 : Your Path\\CONNECTION\\IBAN\\SEQUENCE-DATE");
            Console.WriteLine(" 2 : Your Path\\CONNECTION\\YEAR\\IBAN\\SEQUENCE-DATE");
            fConfig.PathPersist = ReadInt("Your choice: ");


            Console.WriteLine("File types: 0 = CODA, 1 = CAMT, 2 = CAMT and CODA");
            fConfig.FileType = ReadInt("Your choice: ");

            Console.WriteLine("Frequence of each file: 0 = Daily, 1 = Weekly, 2 = Monthly, 3 = Quaterly, 4 = Yearly");
            fConfig.Frequence = ReadString("Your choice: ");
            fConfig.Frequence = "0"; // We force daily process at the moment.
            SaveConfig(fConfig);

            FConfig tempConfig = new FConfig()
            {
                ClientId = fConfig.ClientId,
                ClientSecret = "*******",
                clientName = fConfig.clientName,
                FileType = fConfig.FileType,
                Frequence = fConfig.Frequence,
                Path = fConfig.Path
            };
            AddTrace("Config " + JsonSerializer.Serialize<FConfig>(tempConfig), false, true, true);
            tempConfig = null;

            IsAuthenticated = false;
            AddTrace("Authenticating...", false, true);
            if (Authenticating())
                AddTrace("Authentication succeeded.", false, true);
            else
                AddTrace("Failed to authenticate. Check config.", true, true);

        }

        static void DisplayConfig()
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Current config");
            Console.WriteLine(" Client ID     : " + fConfig.ClientId);
            if (string.IsNullOrEmpty(fConfig.ClientSecret))
                Console.WriteLine(" Client Secret : Not set");
            else
                Console.WriteLine(" Client Secret : ******************");
            Console.WriteLine(" Path          : " + fConfig.Path);
            switch (fConfig.PathPersist)
            {
                case 0:
                    Console.WriteLine(" File Naming   : Your Path\\CONNECTION\\SEQUENCE-DATE-IBAN");
                    break;
                case 1:
                    Console.WriteLine(" File Naming   : Your Path\\CONNECTION\\IBAN\\SEQUENCE-DATE");
                    break;
                case 2:
                    Console.WriteLine(" File Naming   : Your Path\\CONNECTION\\YEAR\\IBAN\\SEQUENCE-DATE");
                    break;
                default:
                    Console.WriteLine(" File Naming   : Your Path\\CONNECTION\\SEQUENCE-DATE-IBAN");
                    break;
            }
            switch (fConfig.Frequence)
            {
                case "0":
                    Console.WriteLine(" Frequence     : Daily");
                    break;
                case "1":
                    Console.WriteLine(" Frequence     : Weekly");
                    break;
                case "2":
                    Console.WriteLine(" Frequence     : Monthy");
                    break;
                case "3":
                    Console.WriteLine(" Frequence     : Quarterly");
                    break;
                case "4":
                    Console.WriteLine(" Frequence     : Yearly");
                    break;
                default:
                    Console.WriteLine(" Frequence     : Not Set");
                    break;
            }
            switch (fConfig.FileType)
            {
                case 0:
                    Console.WriteLine(" File type     : CODA");
                    break;
                case 1:
                    Console.WriteLine(" File type     : CAMT");
                    break;
                case 2:
                    Console.WriteLine(" File type     : CODA + CAMT");
                    break;
                default:
                    Console.WriteLine(" File type     : Not Set");
                    break;
            }
        }

        static void SaveConfig(FConfig _config)
        {
            StreamWriter streamWriter = new StreamWriter("01financials.config");
            streamWriter.Write(JsonSerializer.Serialize<FConfig>(_config));
            streamWriter.Flush();
            streamWriter.Close();
            streamWriter.Dispose();

            AddTrace("Config saved.", false, true);
        }
        #endregion

        #region CLIENT MGMT

        static void AddClient()
        {
            AddTrace("Adding new client", false, true, false);
            DTOAddInstallation addInstallation = new DTOAddInstallation()
            {
                label = ReadString("Client Name....: "),
                clientReference = ReadString("Client Ref.....: "),
                skipNotification = false,
                clientUrl="",
                inClientName=true,
                intermediaryEmail=""
            };
            DTOAddConnection addConnection = new DTOAddConnection()
            {
                label = addInstallation.label,
                psuEmail = ReadEmail("Client Email...: "),
                clientUrl="",
                skipNotification=false
            };

            bool cont = false;
            List<DTOAddAccountRequest> listAddAccountRequests = new List<DTOAddAccountRequest>();
            do
            {
                ListBanks();

                DTOAddAccountRequest addAccountRequest = new DTOAddAccountRequest() {
                    connectorId = ReadInt("Bank ID........: "),
                    iban = ReadString("IBAN...........: ")
                };
                listAddAccountRequests.Add(addAccountRequest);
                cont = ReadBool("Do you want to add one more bank account ?");
            } while (cont);


            AddTrace("Resume of your request:", false, true);
            AddTrace("Client Name....: " + addInstallation.label,false,true);
            AddTrace("Client Ref.....: " + addInstallation.clientReference, false, true);
            AddTrace("Client Email...: " + addConnection.psuEmail, false, true);
            foreach (DTOAddAccountRequest dTOAddAccountRequest in listAddAccountRequests)
                AddTrace(" Bank ID " + dTOAddAccountRequest.connectorId.ToString() + " " + dTOAddAccountRequest.iban, false, true);

            cont = ReadBool("Do you confirm account creation?");
            if (cont)
            {
                // /api/Integrator/Installations
                // DTOInstallation = ... 
                string jsonContent;
                Spinner spinner = new Spinner();
                AddTrace("Creating Installation.", false, true);
                spinner.Start(Console.CursorTop - 1);
                HttpResponseMessage response = httpClient.PostAsync("/api/Integrator/Installations", 
                    new StringContent(JsonSerializer.Serialize(addInstallation), Encoding.UTF8, "application/json")).Result;
                spinner.Stop();
                DTOInstallation dtoInstallation;
                if (response.IsSuccessStatusCode)
                {
                    jsonContent = response.Content.ReadAsStringAsync().Result;
                    AddTrace(jsonContent, false, true, true);
                    dtoInstallation = JsonSerializer.Deserialize<DTOInstallation>(jsonContent);
                    AddTrace("Installation " + dtoInstallation.label + " created, status is " + dtoInstallation.statusText + ".", false, true);

                    // /api/Integrator/Installations/{installationId}/accessrequests
                    // DTOConnection = ...

                    AddTrace("Creating Connection.", false, true);
                    string url = "/api/Integrator/Installations/" + dtoInstallation.id.ToString() + "/accessrequests";
                    AddTrace("Calling " + url + " with " + JsonSerializer.Serialize(addConnection), false, true, true);
                    spinner.Start(Console.CursorTop - 1);
                    response = httpClient.PostAsync(url,
                        new StringContent(JsonSerializer.Serialize(addConnection), Encoding.UTF8, "application/json")).Result;
                    spinner.Stop();
                    if (response.IsSuccessStatusCode)
                    {
                        jsonContent = response.Content.ReadAsStringAsync().Result;
                        AddTrace(jsonContent, false, true, true);
                        DTOConnection dtoConnection= JsonSerializer.Deserialize<DTOConnection>(jsonContent);
                        AddTrace("Connection " + dtoConnection.Label + " created, status is " + dtoConnection.status + ".", false, true);

                        // /api/Integrator/Installations/{installationId}/connections/{connectionId}/accountrequests
                        // 
                        AddTrace("Creating Bank accounts.", false, true);
                        foreach (DTOAddAccountRequest dTOAddAccountRequest in listAddAccountRequests)
                        {
                            AddTrace(" Creating " + dTOAddAccountRequest.iban + " from " + dTOAddAccountRequest.connectorId + ".",false,true);
                            spinner.Start(Console.CursorTop - 1);
                            response = httpClient.PostAsync("/api/Integrator/Installations/" + dtoInstallation.id.ToString() + "/connections/"+dtoConnection.Id.ToString()+"/accountrequests",
                                new StringContent(JsonSerializer.Serialize(addConnection), Encoding.UTF8, "application/json")).Result;
                            spinner.Stop();
                            if (response.IsSuccessStatusCode)
                            {
                                AddTrace(" Created.", false, true);
                            }
                            else
                            {
                                AddTrace("Failed to bank account. (" + response.StatusCode.ToString() + ")", true, true);
                                AddTrace("Error: '" + response.ReasonPhrase + "' from " + URLConn, true, true);
                            }
                        }
                    }
                    else
                    {
                        AddTrace("Failed to create connection. (" + response.StatusCode.ToString() + ")", true, true);
                        AddTrace("Error: '" + response.ReasonPhrase + "' from " + URLConn, true, true);
                    }
                }
                else
                {
                    AddTrace("Failed to create installation. (" + response.StatusCode.ToString() + ")", true, true);
                    AddTrace("Error: '" + response.ReasonPhrase + "' from " + URLConn, true, true);
                }
            }
            return;
        }

        #endregion

        #region FILE MGMT
        static void SaveCoda(DTOFile dtoFile, string iban, string connection, string installation, DateTime dateProcessed, int sequenceNumber)
        {
            string fileName = GetFileName(sequenceNumber.ToString(), dtoFile.dateFrom, iban, connection) + ".COD";
            StreamWriter streamWriter = new StreamWriter(fileName);
            foreach (string s in dtoFile.coda)
                streamWriter.WriteLine(s);
            streamWriter.Flush();
            streamWriter.Close();
            streamWriter.Dispose();
            AddTrace(" Writing " + fileName, false, true);
            SaveHTML("CODA", dtoFile, iban, connection, installation, dateProcessed, sequenceNumber);
            return;
        }

        static void SaveCamt(DTOFile dtoFile, string iban, string connection, string installation, DateTime dateProcessed, int sequenceNumber)
        {
            string fileName = GetFileName(sequenceNumber.ToString(), dtoFile.dateFrom, iban, connection) + ".XML";
            StreamWriter streamWriter = new StreamWriter(fileName);
            streamWriter.WriteLine(dtoFile.camt);
            streamWriter.Flush();
            streamWriter.Close();
            streamWriter.Dispose();
            AddTrace(" Writing " + fileName, false, true);
            SaveHTML("CAMT", dtoFile, iban, connection, installation, dateProcessed,sequenceNumber);
            return;
        }

        static void SaveHTML(string fileType, DTOFile dtoFile, string iban, string connection, string installation, DateTime dateProcessed, int sequenceNumber)
        {
            string fileName = GetFileName(sequenceNumber.ToString(), dtoFile.dateFrom, iban, connection) + "." + fileType + ".HTML";
            StreamWriter streamWriter = new StreamWriter(fileName);
            streamWriter.WriteLine(dtoFile.html);
            streamWriter.Flush();
            streamWriter.Close();
            streamWriter.Dispose();
            AddTrace(" Writing " + fileName, false, true);
            return;
        }


        static string GetFileName(string sequenceNumber, DateTime dateFrom, string iban, string connection)
        {
            switch (fConfig.PathPersist)
            {
                case 0:
                    return fConfig.Path + Path.DirectorySeparatorChar + connection.Replace(" ", "-") + Path.DirectorySeparatorChar + sequenceNumber + "-" + dateFrom.ToString("yyyyMMdd") + "-" + iban;
                    break;
                case 1:
                    return fConfig.Path + Path.DirectorySeparatorChar + connection.Replace(" ", "-") + Path.DirectorySeparatorChar + iban + Path.DirectorySeparatorChar + sequenceNumber + "-" + dateFrom.ToString("yyyyMMdd");
                    break;
                case 2:
                    return fConfig.Path + Path.DirectorySeparatorChar + connection.Replace(" ", "-") + Path.DirectorySeparatorChar + dateFrom.ToString("yyyy") + Path.DirectorySeparatorChar + iban + Path.DirectorySeparatorChar + sequenceNumber + "-" + dateFrom.ToString("yyyyMMdd");
                    break;
                default:
                    return fConfig.Path + Path.DirectorySeparatorChar + connection.Replace(" ", "-") + Path.DirectorySeparatorChar + sequenceNumber + "-" + dateFrom.ToString("yyyyMMdd") + "-" + iban;
                    break;
            }
        }

        static bool DoesFileExist(string iban, string connection, DateTime dateProcessed, string Sequence)
        {
            string directory = fConfig.Path +Path.DirectorySeparatorChar + connection.Replace(" ", "-");
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            if (fConfig.PathPersist==1)
            {
                directory += Path.DirectorySeparatorChar + iban;
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
            }

            if (fConfig.PathPersist == 2)
            {
                directory += Path.DirectorySeparatorChar + dateProcessed.ToString("yyyy");
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                directory += Path.DirectorySeparatorChar + iban;
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
            }

            //string filename =  dateProcessed.DayOfYear.ToString() + "-" + dateProcessed.ToString("yyyyMMdd") + "-" + iban /*+ "-" + connection + "-" + installation*/;
            string filename = GetFileName(Sequence, dateProcessed, iban, connection);
            if (fConfig.FileType == 0)
                filename += ".COD";
            if (fConfig.FileType == 1)
                filename += ".XML";
            if (fConfig.FileType == 2)
                filename += ".COD";
            if (File.Exists(filename))
                return true;
            return false;
        }

        #endregion

        static bool Authenticating()
        {
            bool rvalue = false;
            string jsonContent;
            Spinner spinner = new Spinner();

            DigitealAuth auth = new DigitealAuth()
            {
                client_id = fConfig.ClientId,
                client_secret = fConfig.ClientSecret,
                grant_type = "client_credentials"
            };

            spinner.Start(Console.CursorTop - 1);

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            HttpClientHandler httpHandler = new HttpClientHandler { UseDefaultCredentials = true };
            httpClient = new HttpClient(httpHandler, false);
            httpClient.BaseAddress = new Uri(URLAuth);
            HttpResponseMessage response = httpClient.PostAsync("/api/v1/01financials/tokens", new StringContent(JsonSerializer.Serialize(auth), Encoding.UTF8, "application/json")).Result;

            spinner.Stop();

            if (response.IsSuccessStatusCode)
            {
                AddTrace("Authenticated", false, true);
                jsonContent = response.Content.ReadAsStringAsync().Result;
                DigitealAuthResult oAuth = JsonSerializer.Deserialize<DigitealAuthResult>(jsonContent);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"{oAuth.token_type} {oAuth.access_token}");
                string token = oAuth.access_token;
                oAuth.access_token = "*************";
                AddTrace(JsonSerializer.Serialize(oAuth), false, true, true);
                oAuth.access_token = token;

                spinner.Start(Console.CursorTop - 1);

                response = httpClient.GetAsync("/api/v1/01financials/info").Result;
                jsonContent = response.Content.ReadAsStringAsync().Result;
                AddTrace(jsonContent, false, true, true);
                DTOClient dtoClient = JsonSerializer.Deserialize<DTOClient>(jsonContent);
                spinner.Stop();

                AddTrace("Getting data for " + dtoClient.name.ToUpper(), false, true);

                fConfig.clientName = dtoClient.name;
                SaveConfig(fConfig);

                httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"{oAuth.token_type} {oAuth.access_token}");
                httpClient.BaseAddress = new Uri(URLConn);
                
                IsAuthenticated= rvalue = true;
            }
            else
            {
                AddTrace("Authentication failed. (" + response.StatusCode.ToString() + ")", true, true);
                AddTrace("Error: '" + response.ReasonPhrase + "' from " + URLAuth, true, true);
            }



            return rvalue;
        }

        static List<DTOBank> GetBanks()
        {
            string jsonContent;
            Spinner spinner = new Spinner();

            spinner.Start(Console.CursorTop - 1);
            HttpResponseMessage response = httpClient.GetAsync("/api/installation/banks").Result;
            spinner.Stop();
            if (response.IsSuccessStatusCode)
            {
                jsonContent = response.Content.ReadAsStringAsync().Result;
                AddTrace(jsonContent, false, true, true);
                lstBanks = JsonSerializer.Deserialize<List<DTOBank>>(jsonContent);
                return lstBanks;
            }
            else
            {
                AddTrace("Getting Bank List failed. (" + response.StatusCode.ToString() + ")", true, true);
                AddTrace("Error: '" + response.ReasonPhrase + "' from " + URLConn, true, true);
            }
            return null;
        }


        static DTOInstallation GetInstallation()
        {
            string jsonContent;
            Spinner spinner = new Spinner();

            spinner.Start(Console.CursorTop - 1);
            HttpResponseMessage response = httpClient.GetAsync("/api/installation").Result;
            spinner.Stop();
            if (response.IsSuccessStatusCode)
            {
                jsonContent = response.Content.ReadAsStringAsync().Result;
                AddTrace(jsonContent, false, true, true);
                return JsonSerializer.Deserialize<DTOInstallation>(jsonContent);
            }
            else
            {
                AddTrace("Getting Installation failed. (" + response.StatusCode.ToString() + ")", true, true);
                AddTrace("Error: '" + response.ReasonPhrase + "' from " + URLConn, true, true);
            }
            return null;
        }

        static List<DTOInstallation> GetConnections(Guid installationId)
        {
            string jsonContent;
            Spinner spinner = new Spinner();
            spinner.Start(Console.CursorTop - 1);
            HttpResponseMessage response = httpClient.GetAsync($"/api/installation/connections").Result;
            spinner.Stop();
            if (response.IsSuccessStatusCode)
            {
                jsonContent = response.Content.ReadAsStringAsync().Result;
                AddTrace(jsonContent, false, true, true);
                return JsonSerializer.Deserialize<List<DTOInstallation>>(jsonContent);
            }
            else
            {
                AddTrace("Getting installations failed. (" + response.StatusCode.ToString() + ")", true, true);
                AddTrace("Error: '" + response.ReasonPhrase + "' from " + URLConn, true, true);
            }
            return null;
        }


        static List<DTOAccount> GetAccounts(Guid installationID, Guid connectionID)
        {
            
            string jsonContent;
            Spinner spinner = new Spinner();
            spinner.Start(Console.CursorTop - 1);
            HttpResponseMessage response = httpClient.GetAsync($"/api/installation/connections/{connectionID}/accounts").Result;
            spinner.Stop();
            if (response.IsSuccessStatusCode)
            {
                jsonContent = response.Content.ReadAsStringAsync().Result;
                AddTrace(jsonContent, false, true, true);
                return JsonSerializer.Deserialize<List<DTOAccount>>(jsonContent);
            }
            else
            {
                AddTrace("Getting Connections failed. (" + response.StatusCode.ToString() + ")", true, true);
                AddTrace("Error: '" + response.ReasonPhrase + "' from " + URLConn, true, true);
            }
            return null;
        }

        static DTOTransactionContainer GetTransactions(Guid installationID, Guid connectionID, Guid accountID, int last)
        {
            string url= $"/api/installation/connections/{connectionID}/accounts/{accountID}/transactions?size={last}";
            string jsonContent;
            Spinner spinner = new Spinner();
            spinner.Start(Console.CursorTop - 1);
            AddTrace("Calling :" + url, false, true);
            HttpResponseMessage response = httpClient.GetAsync(url).Result;
            spinner.Stop();
            if (response.IsSuccessStatusCode)
            {
                jsonContent = response.Content.ReadAsStringAsync().Result;
                AddTrace(jsonContent, false, true, true);
                return JsonSerializer.Deserialize<DTOTransactionContainer>(jsonContent);
            }
            else
            {
                AddTrace("Getting Transactions failed. (" + response.StatusCode.ToString() + ")", true, true);
                AddTrace("Error: '" + response.ReasonPhrase + "' from " + URLConn, true, true);
            }
            return null;
        }


        static void ListBanks()
        {
            AddTrace("List of available blanks.", false, true);
            List<DTOBank> lstBanks;
            lstBanks = GetBanks().OrderBy(b => b.fullname).ToList();
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            if (lstBanks != null)
            {
                string line = "";
                for(int i= 0; i<lstBanks.Count; i++)
                {
                    line = lstBanks[i].connectorId.ToString().PadRight(4) + lstBanks[i].fullname.PadRight(20) + lstBanks[i].country.niceName.PadRight(20) + ("").PadLeft(10);
                    if ((i+1<lstBanks.Count))
                    {
                        i++;
                        line += lstBanks[i].connectorId.ToString().PadRight(4) + lstBanks[i].fullname.PadRight(20) + lstBanks[i].country.niceName.PadRight(20);
                    }
                    Console.WriteLine(line);
                }
            }
            else
            {
                AddTrace("No banks found.", true, true);
            }
            return;
        }

        static void ListTransactions(string number, string bankAccount)
        {
            Spinner spinner = new Spinner();
            int i = 0;

            AddTrace("Connecting...", false, true);

            if (!IsAuthenticated)
            {
                AddTrace("Not authenticated.", true, true);
                return;
            }

            DTOInstallation currentInstallation = GetInstallation();

            if (currentInstallation != null)
            {

                AddTrace("Processing installation " + currentInstallation.label, false, true);

                //Connecting to connections: 
                List<DTOInstallation> connections = GetConnections(currentInstallation.id);

                if (connections != null && connections.Count > 0)
                {
                    // Browsing connections.
                    foreach (DTOInstallation connection in connections)
                    {
                        AddTrace("Getting accounts for " + connection.label, false, true);
                        List<DTOAccount> accounts = GetAccounts(currentInstallation.id, connection.id);
                        if (accounts != null && accounts.Count > 0)
                        {
                            // Browsing accounts
                            foreach (DTOAccount account in accounts)
                            {
                                if (account.iban==bankAccount)
                                {
                                    // We get the last X transactions
                                    DTOTransactionContainer dtoContainer= GetTransactions(currentInstallation.id, connection.id, account.id, int.Parse(number));
                                    if (dtoContainer != null)
                                    {
                                        AddTrace("Fetched " + dtoContainer.ToString() + " transactions.", false, true);
                                        foreach (DTOTransaction transaction in dtoContainer.transactions)
                                        {
                                            AddTrace(transaction.executionDate.ToString("dd/MM/yyyy") + " " + transaction.valueDate.ToString("dd/MM/yyyy") +"\t" + transaction.amount + transaction.currency + "\t" + transaction.counterpartName + "\t" + transaction.counterpartReference, false, true);
                                        }
                                    }
                                }
                            }
                        }

                    }
                }
            }


            return;
        }

        static void ListAccount()
        {
            Spinner spinner = new Spinner();
            int i = 0;

            AddTrace("Connecting...", false, true);

            if (!IsAuthenticated)
            {
                AddTrace("Not authenticated.", true, true);
                return;
            }

            DTOInstallation currentInstallation = GetInstallation();

            if (currentInstallation != null)
            {

                    AddTrace("Processing installation " + currentInstallation.label, false, true);

                    //Connecting to connections: 
                    List<DTOInstallation> connections = GetConnections(currentInstallation.id);

                    if (connections != null && connections.Count > 0)
                    {
                        // Browsing connections.
                        foreach (DTOInstallation connection in connections)
                        {
                            AddTrace("Getting accounts for " + connection.label, false, true);
                            List<DTOAccount> accounts = GetAccounts(currentInstallation.id, connection.id);
                            if (accounts != null && accounts.Count > 0)
                            {
                                // Browsing accounts
                                foreach (DTOAccount account in accounts)
                                {
                                    AddTrace(" Account " + account.iban + " (" + account.description + ") at '"+ account.financialInstitution.fullname + "' valid until " + account.validUntil.ToString("dd/MM/yyyy"), false, true);
                                }
                            }

                        }
                    }
            }


            return;
        }

        static void Sync()
        {
            Spinner spinner = new Spinner();
            DTOFile dtoFile;
            DateTime dateToGet;
            bool hasTransaction = false;
            bool cannotContinue = false;
            DateTime yesterday = new DateTime(DateTime.Now.AddDays(-1).Year, DateTime.Now.AddDays(-1).Month, DateTime.Now.AddDays(-1).Day, 23, 59, 59);
            int i = 0;
            HttpResponseMessage response;
            string jsonContent = "";

            AddTrace("Connecting...", false, true);

            if (!IsAuthenticated)
            {
                AddTrace("Not authenticated.", true, true);
                return;
            }

            DTOInstallation currentInstallation = GetInstallation();

            if (currentInstallation != null)
            { 
                //Found installations.

                AddTrace("Processing installation " + currentInstallation.label,false,true);

                //Connecting to installation: 
                List<DTOInstallation> connections = GetConnections(currentInstallation.id);

                if (connections != null && connections.Count > 0)
                {
                    // Browsing connections.
                    Connection currentConnection;
                    foreach (DTOInstallation connection in connections)
                    {
                        if (fConfig.connections == null)
                            fConfig.connections = new List<Connection>();
                        currentConnection = fConfig.connections.Where(c => c.Id == connection.id).FirstOrDefault();
                        if (currentConnection is null)
                        {
                            currentConnection = new Connection() { Id = connection.id, Name = connection.label, bankAccounts = new List<BankAccount>() };
                            fConfig.connections.Add(currentConnection);
                        }

                        AddTrace("Getting accounts for " + connection.label, false, true);
                        List<DTOAccount> accounts = GetAccounts(currentInstallation.id,connection.id);
                        if (accounts != null && accounts.Count > 0)
                        {
                            // Browsing accounts
                            foreach (DTOAccount account in accounts)
                            {
                                dateToGet = DateTime.Now.AddDays(-90);
                                // CannotContine would be true if consent is expired.
                                cannotContinue = false;

                                BankAccount currentBankAccount = currentConnection.bankAccounts.Where(b => b.IBAN == account.iban).FirstOrDefault();
                                if (currentBankAccount is null)
                                {
                                    currentBankAccount = new BankAccount() { Id = account.id, IBAN = account.iban };
                                    AddTrace("New bank account found: " + account.iban, false, true);


                                    // Checking for the latests transaction date 
                                    AddTrace("Checking boundaries.", false, true);
                                    response = httpClient.GetAsync($"/api/installation/connections/{connection.id}/accounts/{account.id}/transactions/boundaries").Result;
                                    if (response.IsSuccessStatusCode)
                                    {
                                        jsonContent = response.Content.ReadAsStringAsync().Result;
                                        AddTrace(jsonContent, false, true, true);
                                        DTOBoundaries dtoBoundaries = JsonSerializer.Deserialize<DTOBoundaries>(jsonContent);
                                        if (dtoBoundaries.dateFrom is null)
                                        {
                                            AddTrace("Bottom boundary is null. Starting at " + dateToGet.ToString("dd/MM/yyyy"), true, true);
                                        }
                                        else
                                            dateToGet = dtoBoundaries.dateFrom.Value;
                                        AddTrace("Boundaries found, loading from " + dateToGet.ToString("dd/MM/yyyy"), false, true);
                                    }
                                    else
                                    {
                                        AddTrace("Getting boundaries failed. (" + response.StatusCode.ToString() + ")", true, true);
                                        AddTrace("Error: '" + response.ReasonPhrase + "' from " + URLConn, true, true);
                                        AddTrace("Starting at " + dateToGet.ToString("dd/MM/yyyy"), true, true);
                                    }

                                    currentBankAccount.Sequence = ReadInt("Enter new sequence number for " + dateToGet.ToString("dd/MM/yyyy") + ": ");
                                    currentConnection.bankAccounts.Add(currentBankAccount);
                                }
                                else
                                {
                                    // Checking if consent is expired.

                                    if (account.validUntil < DateTime.Now)
                                    {
                                        cannotContinue = true;
                                        AddTrace("Account access expired on " + account.validUntil.ToString("dd/MM/yyyy"), true, true);
                                    }
                                    else
                                    {
                                        AddTrace("Processing account " + account.iban, false, true);
                                        dateToGet = currentBankAccount.LastRun.AddDays(1);
                                    }
                                }

                                int previousYear = dateToGet.Year;

                                while (dateToGet <= yesterday && !cannotContinue)
                                {

                                    if (previousYear != dateToGet.Year)
                                    {
                                        // We manage new year's reset to 1 sequence.
                                        previousYear = dateToGet.Year;
                                        currentBankAccount.Sequence = 1;
                                    }
                                    // IF FILE DOESN'T EXIST THEN GET THE FILES
                                    if (!DoesFileExist(account.iban, connection.label, dateToGet, currentBankAccount.Sequence.ToString()))
                                    {

                                        AddTrace(" Getting files for " + dateToGet.ToString("dd/MM/yyyy"),false,true);
                                        if (fConfig.FileType == 0 || fConfig.FileType == 2)
                                        {
                                            response = httpClient.GetAsync($"/api/installation/connections/{connection.id}/accounts/{account.id}/coda?periodtype=" + fConfig.Frequence + "&periodfrom=" + dateToGet.ToString("yyyy-MM-dd") + "&index=" + currentBankAccount.Sequence.ToString()).Result;
                                            if (response.IsSuccessStatusCode)
                                            {
                                                dtoFile = JsonSerializer.Deserialize<DTOFile>(response.Content.ReadAsStringAsync().Result);
                                                if (dtoFile.transactionCount == 0)
                                                {
                                                    AddTrace("No data for " + dateToGet.ToString("dd/MM/yyyy"), false, true);
                                                }
                                                else
                                                {
                                                    AddTrace(" Transactions: " + dtoFile.transactionCount.ToString() + " Sequence: " + dtoFile.sequenceNumber.ToString(), false, true);
                                                    SaveCoda(dtoFile, account.iban, connection.label, currentInstallation.label, dateToGet, currentBankAccount.Sequence);
                                                    i++;
                                                    hasTransaction = true;
                                                    
                                                }
                                            }
                                            else
                                            {
                                                AddTrace("Getting CODA failed. (" + response.StatusCode.ToString() + ")", true, true);
                                                AddTrace("Error: '" + response.ReasonPhrase + "' from " + URLConn, true, true);
                                            }
                                        }
                                        if (fConfig.FileType == 1 || fConfig.FileType == 2)
                                        {
                                            response = httpClient.GetAsync($"/api/installation/connections/{connection.id}/accounts/{account.id}/camt53?periodtype=" + fConfig.Frequence + "&periodfrom=" + dateToGet.ToString("yyyy-MM-dd") + "&index=" + currentBankAccount.Sequence.ToString()).Result;
                                            if (response.IsSuccessStatusCode)
                                            {
                                                dtoFile = JsonSerializer.Deserialize<DTOFile>(response.Content.ReadAsStringAsync().Result);
                                                if (dtoFile.transactionCount == 0)
                                                {
                                                    AddTrace("No data for " + dateToGet.ToString("dd/MM/yyyy"), false, true);
                                                }
                                                else
                                                {
                                                    AddTrace(" Transactions: " + dtoFile.transactionCount.ToString() + " Sequence: " + dtoFile.sequenceNumber.ToString(), false, true);
                                                    SaveCamt(dtoFile, account.iban, connection.label, currentInstallation.label, dateToGet, currentBankAccount.Sequence);
                                                    i++;
                                                    hasTransaction = true;
                                                }
                                            }
                                            else
                                            {
                                                AddTrace("Getting CAMT failed. (" + response.StatusCode.ToString() + ")", true, true);
                                                AddTrace("Error: '" + response.ReasonPhrase + "' from " + URLConn, true, true);
                                            }
                                        }
                                        if (hasTransaction)
                                        {
                                            currentBankAccount.Sequence++;
                                            hasTransaction = false;
                                        }
                                    }
                                    dateToGet = dateToGet.AddDays(1);
                                }
                                if (!cannotContinue)
                                    currentBankAccount.LastRun = dateToGet.AddDays(-1);
                                AddTrace(" > " + i.ToString() + " file(s) generated.",false,true);
                            }
                        }
                    }
                }
            }
            SaveConfig(fConfig);
            return;
        }
    }
}
