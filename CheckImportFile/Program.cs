using Dapper;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Mail;

namespace IEPImportMonitor
{
    class Program
    {
        public const string ConnectionString = "Data Source=10.55.30.41,3180;Initial Catalog=Athena.Logs;User=RobBurke;Password=RepeatedActions27.";
        static void Main(string[] args)
        {
            Console.Title = "IEP Import Monitor";
            Console.SetWindowSize(120, 20);

            Console.WriteLine("Gathering Import Data...");
            Console.WriteLine("");
            SqlConnection connection = Program.GetOpenConnection();
            
            string OutputInfo = "";

            string[] Configuration = GetConfiguration(ConfigurationManager.AppSettings["Sites Config File"].ToString());

            List<Site> Sites = GetSiteConfiguration(Configuration);

            foreach (Site Site in Sites)
            {
                Site.ImportIncomingFiles = GetIncomingFiles(Site.ImportIncomingPath);

                if (Site.ImportIncomingFiles[0] == "No Pending Incoming Files.")
                {
                    Site.PendingIncoming = false;
                }
                else
                {
                    Site.PendingIncoming = true;
                }

                string Date = DateTime.Now.ToString("yyyy-MM-dd");
                string SQLGetLogs = "SELECT *" +
                                    "  FROM [Athena.Logs].[dbo].[ELMAH_Error]" +
                                    " WHERE [Application] = '" + Site.CountryCode + ".athena.service'" +
                                    "   AND [Type] = 'Service.SuccessLogEvent'" +
                                    "   AND [Message] LIKE '%Import%'" +
                                    "   AND [TimeUtc] >= '" + Date + "'";

                var Logs = connection.Query<ELMAHLog>(SQLGetLogs);

                Site.DBProcessedTodayFiles = new List<String>();

                if (Logs.Count() == 0)
                {
                    Site.DBProcessedToday = false;
                }
                else
                {
                    Site.DBProcessedToday = true;
                    foreach (ELMAHLog Log in Logs)
                    {
                        string[] LogFilePath = Log.Message.Split('\\');
                        string[] LogFileName = LogFilePath[LogFilePath.Length - 1].Split(' ');
                        string FileName = LogFileName[0];
                        Site.DBProcessedTodayFiles.Add(FileName);
                    }
                }

                /*
                 *                Files Processed Today
                 *         True                False
                 *    
                 *   True    CHECK OK           ALERT
                 *   
                 *   False   OKAY               ALERT
                */
            }

            OutputInfo += "|-------|--------------------|---------------------------|-------------------------------|\r\n";
            OutputInfo += "| Site  | Database           | Incoming                  | Action                        |\r\n";
            OutputInfo += "|-------|--------------------|---------------------------|-------------------------------|\r\n";

            foreach (Site Site in Sites)
            {
                if (DateTime.Now.Hour > Site.HourToCheck)
                {
                    if ((Site.DBProcessedToday) & (Site.PendingIncoming))
                    {
                        OutputInfo += String.Format("| {0}    | Files Processed OK | Files Waiting in Incoming | CHECK: Process Incoming Files |\r\n", Site.CountryCode);
                    }
                    else if ((!Site.DBProcessedToday) & (Site.PendingIncoming))
                    {
                        OutputInfo += String.Format("| {0}    | No Files Processed | Files Waiting in Incoming | ALERT: Process Incoming Files |\r\n", Site.CountryCode);
                    }
                    else if ((!Site.DBProcessedToday) & (!Site.PendingIncoming))
                    {
                        OutputInfo += String.Format("| {0}    | No Files Processed | No Files in Incoming      | ALERT: No ECOMMDATA Files     |\r\n", Site.CountryCode);
                    }
                    else
                    {
                        OutputInfo += String.Format("| {0}    | Files Processed OK | No Files in Incoming      | OKAY:  No Action Required     |\r\n", Site.CountryCode);
                    }
                }
                else
                {                    
                    OutputInfo += String.Format("| {0}    | None Expected Yet  | None Expected Yet         | OKAY:  Nothing Expected Yet   |\r\n", Site.CountryCode);
                }

            }
            OutputInfo += "|-------|--------------------|---------------------------|-------------------------------|\r\n";

            OutputInfo += "\r\n";
            OutputInfo += "Details:\r\n";
            OutputInfo += "\r\n";

            foreach (Site Site in Sites)
            {
                if (DateTime.Now.Hour < Site.HourToCheck)
                {
                    OutputInfo += String.Format("{0}: No Files Expected Yet\r\n", Site.CountryCode);
                    OutputInfo += "\r\n";
                }
                else
                {
                    if ((Site.DBProcessedToday) & (Site.PendingIncoming))
                    {
                        OutputInfo += String.Format("{0}: CHECK: Process Incoming Files\r\n", Site.CountryCode);
                        OutputInfo += String.Format("{0}: Files in Incoming:\r\n", Site.CountryCode);
                        foreach (string File in Site.ImportIncomingFiles)
                        {
                            OutputInfo += String.Format("    {0}\r\n", File);
                        }
                        OutputInfo += String.Format("{0}: Files Processed in DB:\r\n", Site.CountryCode);
                        foreach (string File in Site.DBProcessedTodayFiles)
                        {
                            OutputInfo += String.Format("    {0}\r\n", File);
                        }
                        OutputInfo += "\r\n";
                    }
                    else if ((!Site.DBProcessedToday) & (Site.PendingIncoming))
                    {
                        OutputInfo += String.Format("{0}: ALERT: Process Incoming Files\r\n", Site.CountryCode);
                        OutputInfo += String.Format("{0}: Files in Incoming:\r\n", Site.CountryCode);
                        foreach (string File in Site.ImportIncomingFiles)
                        {
                            OutputInfo += String.Format("    {0}\r\n", File);
                        }
                        OutputInfo += "\r\n";
                    }
                    else if ((!Site.DBProcessedToday) & (!Site.PendingIncoming))
                    {
                        OutputInfo += String.Format("{0}: ALERT: No ECOMMDATA Files\r\n", Site.CountryCode);
                        OutputInfo += String.Format("{0}: Contact Apollo / NAV for Files, None in DB, None in Incoming\r\n", Site.CountryCode);
                        OutputInfo += "\r\n";
                    }
                    else
                    {

                    }
                }
            }

            SendNotificationMail(OutputInfo);

            Console.WriteLine(OutputInfo);
        }

        private static void SendNotificationMail(string OutputInfo)
        {
            MailMessage mail = new MailMessage(ConfigurationManager.AppSettings["Mail From"].ToString(), ConfigurationManager.AppSettings["Mail To"].ToString());
            SmtpClient client = new SmtpClient();
            client.Port = 25;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.UseDefaultCredentials = false;
            client.Credentials = new System.Net.NetworkCredential(ConfigurationManager.AppSettings["SMTP Username"].ToString(), ConfigurationManager.AppSettings["SMTP Password"].ToString());
            client.Host = ConfigurationManager.AppSettings["SMTP Server"].ToString();
            mail.Subject = ConfigurationManager.AppSettings["Mail Subject"].ToString();
            mail.Body = OutputInfo;
            client.Send(mail);
        }

        static string[] GetConfiguration(string ConfigurationFile)
        {
            string[] Configuration = File.ReadAllLines(ConfigurationFile);
            return Configuration;
        }
        static List<Site> GetSiteConfiguration(string[] Configuration)
        {
            List<Site> Sites = new List<Site>();

            foreach (string SiteData in Configuration)
            {
                string[] SiteDataDetails = SiteData.Split(',');

                Site NewSite = new Site();
                NewSite.CountryCode = SiteDataDetails[0];
                NewSite.DatabaseName = SiteDataDetails[1];
                NewSite.ImportRootPath = SiteDataDetails[2];
                NewSite.HourToCheck = Convert.ToInt32(SiteDataDetails[3]);
                NewSite.ImportProcessedPath = NewSite.ImportRootPath + @"Processed\";
                NewSite.ImportIncomingPath = NewSite.ImportRootPath + @"Incoming\"; ;
                Sites.Add(NewSite);
            }

            return Sites;
        }

        public static SqlConnection GetOpenConnection()
        {
            var connection = new SqlConnection(ConnectionString);
            connection.Open();
            return connection;
        }

        public static List<String> GetIncomingFiles(string IncomingPath)
        {
            string[] IncomingFiles = Directory.GetFiles(IncomingPath, "*.zip");
            List<String> FilesList = new List<String>();

            if (IncomingFiles.Length == 0)
            {
                FilesList.Add("No Pending Incoming Files.");
            }
            else
            {
                for (int i = 0; i <= IncomingFiles.Length - 1; i++)
                {
                    FilesList.Add(IncomingFiles[i]);
                }
            }

            return FilesList;
        }
    }
}
