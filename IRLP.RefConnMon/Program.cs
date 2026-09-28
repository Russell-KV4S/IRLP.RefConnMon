using System.Configuration;
using System.Net;
using System.Text.RegularExpressions;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Telegram.Bot;

namespace KV4S.AmateurRadio.IRLP.RefConnMon
{
    class Program
    {
        const string URL = "http://status.irlp.net/index.php?PSTART=9";
        const string StatusTitle = "IRLP Reflector Connection Monitor";

        //Telegram allows roughly one message per second to the same chat.
        static readonly TimeSpan TelegramDelay = TimeSpan.FromSeconds(1);

        //State and log files live next to the executable so Task Scheduler/cron runs don't depend on the working directory.
        static readonly string BaseDir = AppContext.BaseDirectory;
        static readonly string ErrorLogPath = Path.Combine(BaseDir, "ErrorLog.txt");

        static readonly Regex HtmlTag = new Regex("<.*?>", RegexOptions.Compiled);

        static TelegramBotClient? _bot;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Welcome to the IRLP Reflector Connection Monitor Application by KV4S!");
            Console.WriteLine(" ");
            try
            {
                Console.WriteLine("Beginning download from " + URL);
                Console.WriteLine("Please Stand by.....");
                Console.WriteLine(" ");

                string irlpHTML;
                using (var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) })
                {
                    irlpHTML = await http.GetStringAsync(URL);
                }

                if (!irlpHTML.Contains("9050") ||        //seeing if the html includes the largest reflector. sometimes the data isn't loaded when the html is loaded.
                    !irlpHTML.Contains("<hr><center>"))  //when writing html file out to disk saw bad symbols or blank spaces on some downloads and represents a bad download.
                {
                    Console.WriteLine("Downloaded page looks incomplete; skipping this run so no false disconnects are reported.");
                    return;
                }

                List<Node> allNodes = ParseNodes(irlpHTML);

                foreach (string reflector in SplitList(Setting("Reflectors").ToUpper()))
                {
                    Console.WriteLine("Looking for connections to " + reflector);
                    List<Node> nodesOnWeb = allNodes
                        .Where(n => n.ConnectedReflector == reflector)
                        .GroupBy(n => n.Number)
                        .Select(g => g.First())
                        .ToList();
                    foreach (var node in nodesOnWeb)
                    {
                        Console.WriteLine("     " + node.Callsign + " node number " + node.Number + " is connected to reflector " + node.ConnectedReflector + ".");
                    }

                    string statePath = Path.Combine(BaseDir, reflector + ".txt");
                    if (File.Exists(statePath))
                    {
                        List<Node> nodesOnDisk = LoadNodes(statePath);
                        var diskNumbers = nodesOnDisk.Select(n => n.Number).ToHashSet();
                        var webNumbers = nodesOnWeb.Select(n => n.Number).ToHashSet();

                        var connected = nodesOnWeb.Where(n => !diskNumbers.Contains(n.Number)).ToList();
                        var disconnected = nodesOnDisk.Where(n => !webNumbers.Contains(n.Number)).ToList();

                        foreach (var node in connected)
                        {
                            await NotifyStatus(node.Callsign + " (" + node.Number + ") has connected to " + node.ConnectedReflector + ".");
                        }
                        foreach (var node in disconnected)
                        {
                            await NotifyStatus(node.Callsign + " (" + node.Number + ") has disconnected from " + node.ConnectedReflector + ".");
                        }

                        if (connected.Count > 0 || disconnected.Count > 0)
                        {
                            SaveNodes(statePath, nodesOnWeb);
                        }
                    }
                    else
                    {
                        SaveNodes(statePath, nodesOnWeb);
                    }
                }
                Console.WriteLine("Reflector Monitoring Complete!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Program encountered an error:");
                Console.WriteLine(ex.Message);
                LogError(ex);
                if (Flag("EmailError"))
                {
                    await SendEmail("IRLP.RefConnMon Error", "Message: " + ex.Message + " Source: " + ex.Source);
                }
                if (Flag("TelegramError"))
                {
                    await SendTelegram("IRLP.RefConnMon Error - Message: " + ex.Message + " Source: " + ex.Source);
                }
            }
            finally
            {
                if (IsNo("Unattended") && !Console.IsInputRedirected)
                {
                    Console.WriteLine("Press any key on your keyboard to quit...");
                    try
                    {
                        Console.ReadKey();
                    }
                    catch (InvalidOperationException)
                    {
                        //no interactive console available (e.g. running as a scheduled task).
                    }
                }
            }
        }

        //Each table row starts with "<tr><td>"; the first three chunks are the page header and table header rows.
        //Cells are: node number, callsign, ..., connected reflector (last cell).
        static List<Node> ParseNodes(string html)
        {
            var nodes = new List<Node>();
            string[] rows = html.Split(new[] { "<tr><td>" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string row in rows.Skip(3))
            {
                int rowEnd = row.IndexOf("</td></tr>", StringComparison.Ordinal);
                string cellsHtml = rowEnd >= 0 ? row.Substring(0, rowEnd) : row;
                string[] cells = cellsHtml.Split(new[] { "</td><td>" }, StringSplitOptions.RemoveEmptyEntries);
                if (cells.Length < 3)
                {
                    continue;
                }

                var node = new Node
                {
                    Number = CellText(cells[0]),
                    Callsign = CellText(cells[1]),
                    ConnectedReflector = CellText(cells[cells.Length - 1]),
                };
                if (node.Number.Length > 0)
                {
                    nodes.Add(node);
                }
            }
            return nodes;
        }

        static string CellText(string cellHtml)
        {
            return WebUtility.HtmlDecode(HtmlTag.Replace(cellHtml, "")).Trim();
        }

        //State file format: one "Number,Callsign,Reflector" line per connected node.
        static List<Node> LoadNodes(string path)
        {
            var nodes = new List<Node>();
            foreach (string line in File.ReadLines(path))
            {
                string[] fields = line.Split(',');
                if (fields.Length < 3 || fields[0].Trim().Length == 0)
                {
                    continue;
                }
                nodes.Add(new Node
                {
                    Number = fields[0].Trim(),
                    Callsign = fields[1].Trim(),
                    ConnectedReflector = fields[2].Trim(),
                });
            }
            return nodes;
        }

        //Write to a temp file and swap it in so a failed write can't wipe out the saved state.
        static void SaveNodes(string path, IEnumerable<Node> nodes)
        {
            string tempPath = path + ".tmp";
            File.WriteAllLines(tempPath, nodes.Select(n => n.Number + "," + n.Callsign + "," + n.ConnectedReflector));
            File.Move(tempPath, path, overwrite: true);
        }

        static async Task NotifyStatus(string message)
        {
            if (Flag("StatusEmails"))
            {
                await SendEmail(StatusTitle, message);
            }
            if (Flag("TelegramStatus"))
            {
                await SendTelegram(StatusTitle + " - " + message);
                await Task.Delay(TelegramDelay);
            }
        }

        static async Task SendEmail(string subject, string body)
        {
            try
            {
                var mail = new MimeMessage();
                mail.Subject = subject;
                foreach (string address in SplitList(Setting("EmailFrom")))
                {
                    mail.From.Add(MailboxAddress.Parse(address));
                }
                foreach (string address in SplitList(Setting("EmailTo")))
                {
                    mail.To.Add(MailboxAddress.Parse(address));
                }
                mail.Body = new TextPart("plain") { Text = body };

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(Setting("SMTPHost"), int.Parse(Setting("SMTPPort")), SecureSocketOptions.Auto);
                await smtp.AuthenticateAsync(Setting("SMTPUser"), Setting("SMTPPassword"));
                await smtp.SendAsync(mail);
                await smtp.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending email:");
                Console.WriteLine(ex.Message);
                LogError(ex);
            }
        }

        static async Task SendTelegram(string message)
        {
            try
            {
                _bot ??= new TelegramBotClient(Setting("BotToken"));
                await _bot.SendMessage(Setting("DestinationID"), message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending Telegram message:");
                Console.WriteLine(ex.Message);
                LogError(ex);
            }
        }

        static void LogError(Exception ex)
        {
            try
            {
                File.AppendAllText(ErrorLogPath, DateTime.Now + " Error: " + ex + Environment.NewLine);
            }
            catch (Exception)
            {
                Console.WriteLine("Error logging previous error.");
                Console.WriteLine("Make sure the Error log is not open.");
            }
        }

        static string Setting(string key)
        {
            return ConfigurationManager.AppSettings[key] ?? throw new ConfigurationErrorsException("Missing setting '" + key + "' in the .config file.");
        }

        static bool Flag(string key)
        {
            return string.Equals(ConfigurationManager.AppSettings[key]?.Trim(), "Y", StringComparison.OrdinalIgnoreCase);
        }

        static bool IsNo(string key)
        {
            return string.Equals(ConfigurationManager.AppSettings[key]?.Trim(), "N", StringComparison.OrdinalIgnoreCase);
        }

        static IEnumerable<string> SplitList(string value)
        {
            return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
    }
}
