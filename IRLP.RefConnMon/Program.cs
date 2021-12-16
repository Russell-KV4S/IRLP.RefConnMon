using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace KV4S.AmateurRadio.IRLP.RefConnMon
{
    class Program
    {
        public static string URL = "http://status.irlp.net/index.php?PSTART=9";

        //load from App.config
        public static MailAddress from = new MailAddress(ConfigurationManager.AppSettings["EmailFrom"]);
        public static string toConfig = ConfigurationManager.AppSettings["EmailTo"];
        public static string smtpHost = ConfigurationManager.AppSettings["SMTPHost"];
        public static string smtpPort = ConfigurationManager.AppSettings["SMTPPort"];
        public static string smtpUser = ConfigurationManager.AppSettings["SMTPUser"];
        public static string smtpPswrd = ConfigurationManager.AppSettings["SMTPPassword"];

        private static List<string> _reflectorList = null;
        private static string ReflectorListString
        {
            set
            {
                string[] reflectorArray = value.Split(',');
                _reflectorList = new List<string>(reflectorArray.Length);
                _reflectorList.AddRange(reflectorArray);
            }
        }

        private static List<string> _emailAddressList = null;
        private static string EmailAddressListString
        {
            set
            {
                string[] emailAddressArray = value.Split(',');
                _emailAddressList = new List<string>(emailAddressArray.Length);
                _emailAddressList.AddRange(emailAddressArray);
            }
        }

        static void Main(string[] args)
        {
            try
            {
                NodeCollection NodesOnDisk = new NodeCollection();
                NodeCollection NodesOnWeb = new NodeCollection();

                Console.WriteLine("Welcome to the IRLP Reflector Connection Monitor Application by KV4S!");
                Console.WriteLine(" ");
                Console.WriteLine("Beginning download from " + URL);
                Console.WriteLine("Please Stand by.....");
                Console.WriteLine(" ");
                using (WebClient wc = new WebClient())
                {
                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    var irlpHTML = wc.DownloadString(URL);

                    if (irlpHTML.Contains("9050") &&        //seeing if the html includes the largest reflector. sometimes the data isn't loaded when the html is loaded.
                        irlpHTML.Contains("<hr><center>"))  //when writing html file out to disk saw bad symbols or blank spaces on some downloads and represents a bad download.
                    {
                        ReflectorListString = ConfigurationManager.AppSettings["Reflectors"].ToUpper();
                        foreach (string reflector in _reflectorList)
                        {
                            Console.WriteLine("Looking for connections to " + reflector);
                            string[] strBeginSplit = new string[] { "<tr><td>" };
                            string[] strRowSplit = irlpHTML.Split(strBeginSplit, StringSplitOptions.RemoveEmptyEntries);

                            int i = 1;
                            foreach (var item in strRowSplit)
                            {
                                if (i > 3)
                                {
                                    string[] strItemSplit = new string[] { "</td><td>", "</td></tr>" };
                                    string[] strFieldSplit = item.Split(strItemSplit, StringSplitOptions.RemoveEmptyEntries);

                                    if (strFieldSplit[strFieldSplit.Length - 1].ToString().Trim() == reflector)
                                    {
                                        Node node = new Node();
                                        node.Callsign = strFieldSplit[1].ToString().Trim();
                                        node.Number = strFieldSplit[0].Substring(strFieldSplit[0].IndexOf(">") + 1,
                                                      strFieldSplit[0].Length - strFieldSplit[0].LastIndexOf("<"));
                                        node.ConnectedReflector = strFieldSplit[strFieldSplit.Length - 1].ToString().Trim();
                                        NodesOnWeb.Add(node);
                                        Console.WriteLine("     " + node.Callsign + " node number " + node.Number + " is connected to reflector " + node.ConnectedReflector + ".");
                                    }
                                }
                                i++;
                            }

                            bool SomethingChanged = false;
                            if (File.Exists(reflector + ".txt"))
                            {
                                //Load Node object from disk.
                                using (StreamReader sr = File.OpenText(reflector + ".txt"))
                                {
                                    String s = "";
                                    while ((s = sr.ReadLine()) != null)
                                    {
                                        string[] strItemSplit = new string[] { "," };
                                        string[] strFieldSplit = s.Split(strItemSplit, StringSplitOptions.RemoveEmptyEntries);
                                        Node node = new Node();
                                        node.Number = strFieldSplit[0];
                                        node.Callsign = strFieldSplit[1];
                                        node.ConnectedReflector = strFieldSplit[2];
                                        NodesOnDisk.Add(node);
                                    }
                                }

                                //Compare web to disk and add missing as these represent new connections
                                foreach (var webNode in NodesOnWeb)
                                {
                                    bool found = false;
                                    foreach (var diskNode in NodesOnDisk)
                                    {
                                        if (webNode.Number == diskNode.Number)
                                        {
                                            found = true;
                                        }
                                    }
                                    if (!found)
                                    {
                                        SomethingChanged = true;
                                        NodesOnDisk.Add(webNode);
                                        Email(webNode.Callsign + " (" + webNode.Number + ") has connected to " + webNode.ConnectedReflector + ".");
                                    }
                                }

                                //compare disk to web and remove missing as these repesent disconnections
                                foreach (var diskNode in NodesOnDisk.ToList())
                                {
                                    bool found = false;
                                    foreach (var webNode in NodesOnWeb)
                                    {
                                        if (diskNode.Number == webNode.Number)
                                        {
                                            found = true;
                                        }
                                    }
                                    if (!found)
                                    {
                                        SomethingChanged = true;
                                        NodesOnDisk.Remove(diskNode);
                                        Email(diskNode.Callsign + " (" + diskNode.Number + ") has disconnected from " + diskNode.ConnectedReflector + ".");
                                    }
                                }

                                //delete and rewrite the new nodes on disk list.
                                if (SomethingChanged)
                                {
                                    File.Delete(reflector + ".txt");
                                    FileStream fs = null;
                                    fs = new FileStream(reflector + ".txt", FileMode.Append);
                                    StreamWriter log = new StreamWriter(fs);
                                    foreach (var node in NodesOnDisk)
                                    {
                                        log.WriteLine(node.Number + "," + node.Callsign + "," + node.ConnectedReflector);
                                    }
                                    log.Close();
                                    fs.Close();

                                    //debugging/testing only - adding code to write html to file to try to figure out if malformed html is reporting as a disconnect false positive.
                                    //FileStream fs1 = null;
                                    //fs1 = new FileStream("irlpHTML_" + DateTime.Now.ToString("HH_mm_ss") + ".txt", FileMode.Append);
                                    //StreamWriter html = new StreamWriter(fs1);
                                    //html.WriteLine(irlpHTML.ToString());
                                    //html.Close();
                                    //fs1.Close();
                                }
                            }
                            else
                            {
                                FileStream fs = null;
                                fs = new FileStream(reflector + ".txt", FileMode.Append);
                                StreamWriter log = new StreamWriter(fs);
                                foreach (var node in NodesOnWeb)
                                {
                                    log.WriteLine(node.Number + "," + node.Callsign + "," + node.ConnectedReflector);
                                }
                                log.Close();
                                fs.Close();
                            }
                            NodesOnWeb.Clear();
                            NodesOnDisk.Clear();
                        }
                    }
                }
                Console.WriteLine("Reflector Monitoring Complete!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Program encountered and error:");
                Console.WriteLine(ex.Message);
                LogError(ex.Message, ex.Source);
                if (ConfigurationManager.AppSettings["EmailError"] == "Y")
                {
                    EmailError(ex.Message, ex.Source);
                }
            }
            finally
            {
                if (ConfigurationManager.AppSettings["Unattended"] == "N")
                {
                    Console.WriteLine("Press any key on your keyboard to quit...");
                    Console.ReadKey();
                }
            }
        }

        private static void EmailError(string Message, string Source)
        {
            try
            {
                MailMessage mail = new MailMessage();
                mail.Subject = "IRLP.RefConnMon Error";
                mail.From = from;

                EmailAddressListString = toConfig;
                foreach (string emailAddress in _emailAddressList)
                {
                    mail.To.Add(emailAddress);
                }

                mail.Body = "Message: " + Message + " Source: " + Source;

                SmtpClient smtp = new SmtpClient();
                smtp.Host = smtpHost;
                smtp.Port = Convert.ToInt32(smtpPort);

                smtp.Credentials = new NetworkCredential(smtpUser, smtpPswrd);
                smtp.EnableSsl = true;
                smtp.Send(mail);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Program encountered and an error sending email:");
                Console.WriteLine(ex.Message);
                LogError(ex.Message, ex.Source);
            }
        }

        private static void Email(string body)
        {
            try
            {
                MailMessage mail = new MailMessage();
                mail.Subject = "IRLP Reflector Connnection Monitor";
                mail.From = from;

                EmailAddressListString = toConfig;
                foreach (string emailAddress in _emailAddressList)
                {
                    mail.To.Add(emailAddress);
                }

                mail.Body = body;

                SmtpClient smtp = new SmtpClient();
                smtp.Host = smtpHost;
                smtp.Port = Convert.ToInt32(smtpPort);

                smtp.Credentials = new NetworkCredential(smtpUser, smtpPswrd);
                smtp.EnableSsl = true;
                smtp.Send(mail);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending email:");
                Console.WriteLine(ex.Message);
                LogError(ex.Message, ex.Source);
            }
        }

        private static void LogError(string Message, string source)
        {
            try
            {
                FileStream fs = null;
                fs = new FileStream("ErrorLog.txt", FileMode.Append);
                StreamWriter log = new StreamWriter(fs);
                log.WriteLine(DateTime.Now + " Error: " + Message + " Source: " + source);
                log.Close();
                fs.Close();
            }
            catch (Exception)
            {
                Console.WriteLine("Error logging previous error.");
                Console.WriteLine("Make sure the Error log is not open.");
            }
        }
    }
}
