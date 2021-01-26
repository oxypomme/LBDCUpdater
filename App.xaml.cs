using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace LBDCUpdater
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        internal static LogStream LogStream = new LogStream("latest.log");
        internal static Manager Manager { get; private set; }

        private App()
        {
            try
            {
                Updater.CheckUpdates();
                Manager = new Manager();
            }
            catch (Exception ex) { LogStream.Log(new(ex.ToString(), LogSeverity.Critical, ex)); }
        }
    }
}