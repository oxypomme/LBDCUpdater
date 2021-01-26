using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LBDCUpdater
{
    /// <summary>
    /// Logique d'interaction pour LocalModControl.xaml
    /// </summary>
    public partial class LocalModControl : UserControl
    {
        public LocalModControl(string mod)
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex) { App.LogStream.Log(new(ex.ToString(), LogSeverity.Critical, ex)); }
            modname.Content = mod;
        }
    }
}