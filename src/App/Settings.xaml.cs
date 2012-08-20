using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.Threading;
using BeatMachine.Model;
using NLog;
using NLog.Targets;
using System.Text;
using Microsoft.Phone.Tasks;

namespace BeatMachine
{
    public partial class Settings : PhoneApplicationPage
    {
        public Settings()
        {
            InitializeComponent();



        }

        private void ReanalyzeButton_Click(object sender, RoutedEventArgs e)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(o =>
            {
                using (BeatMachineDataContext context = new BeatMachineDataContext(
                    BeatMachineDataContext.DBConnectionString))
                {
                    context.AnalyzedSongs.DeleteAllOnSubmit(
                        context.AnalyzedSongs.ToList());
                    context.Summary.DeleteAllOnSubmit(
                        context.Summary.ToList());
                    context.SubmitChanges();
                }
            }));
        }

        private void SendErrorLogsButton_Click(object sender, RoutedEventArgs e)
        {
            MemoryTarget target = LogManager.Configuration.FindTargetByName("memory") as MemoryTarget;

            StringBuilder sb = new StringBuilder();
            foreach (string entry in target.Logs)
            {
                sb.AppendLine(entry);
            }


            EmailComposeTask emailComposeTask = new EmailComposeTask();

            emailComposeTask.Subject = "mu:ru error log";
            emailComposeTask.Body = sb.ToString();
            emailComposeTask.To = "beatmachineapp@live.com";
            emailComposeTask.Show();
        }

    }
}