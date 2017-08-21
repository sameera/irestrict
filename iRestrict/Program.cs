using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace iRestrict
{

    struct Options
    {
        public string[] Profiles { get; set; }
        public int StartTime { get; set; }
        public int EndTime { get; set; }
    }

    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var options = GetOptions();
            var now = DateTime.Now;
            bool isMetered = now.Hour < options.StartTime || now.Hour >= options.EndTime;
            SetMetered(options.Profiles, isMetered);
        }

        private static Options GetOptions()
        {
            var commandLine = Environment.CommandLine;
            var profileExp = new Regex(@"\b\w+=(?<profile>\w+)", RegexOptions.Compiled  );

            var matches = profileExp.Matches(commandLine);
            var options = new Options() {
                Profiles = new string[matches.Count],
                StartTime = 0,
                EndTime = 8
            };

            for (int i = matches.Count - 1; i >= 0; i--)
            {
                options.Profiles[i] = matches[i].Groups["profile"].Value;
            }
            return options;
        }

        private static void SetMetered(string[] profiles, bool isMetered)
        {
            // Get all profiles
            using (var eventLog = new EventLog("Application"))
            {
                eventLog.Source = "Application";

                if (profiles.Length == 0)
                {
                    eventLog.WriteEntry(
                        "No WiFi Profiles have been specified. Aborting...",
                        EventLogEntryType.Error
                    );
                    return;
                }

                for (int i = profiles.Length - 1; i >= 0; i--)
                {
                    eventLog.WriteEntry(
                        ExecNetSh($"wlan set profileparameter name={profiles[i]} cost={(isMetered ? "Fixed" : "Unrestricted")}"),
                        EventLogEntryType.Information,
                        101,
                        1
                    );
                }
            }
        }

        private static string ExecNetSh(string command)
        {
            Process netsh = new Process();
            netsh.StartInfo.FileName = "netsh.exe";
            netsh.StartInfo.UseShellExecute = false;
            netsh.StartInfo.RedirectStandardOutput = true;
            netsh.StartInfo.Arguments = command;
            netsh.StartInfo.CreateNoWindow = true;
            netsh.Start();

            return netsh.StandardOutput.ReadToEnd();
        }
    }
}
