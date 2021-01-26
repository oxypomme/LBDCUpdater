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
        private bool isChecked;

        public LocalModControl(string mod)
        {
            try
            {
                InitializeComponent();
                modname.Content = mod;
                isChecked = App.Manager.LocalMods.First(c => c.Item1 == mod).Item2;
                if (isChecked)
                {
                    back.Background = new SolidColorBrush(Color.FromRgb(60, 255, 90));
                    switchButton.IsChecked = true;
                }
                else
                {
                    back.Background = new SolidColorBrush(Color.FromRgb(255, 140, 50));
                    switchButton.IsChecked = false;
                }
            }
            catch (Exception ex) { App.LogStream.Log(new(ex.ToString(), LogSeverity.Critical, ex)); }
        }

        private void switchButton_Click(object sender, RoutedEventArgs e)
        {
            if (isChecked != switchButton.IsChecked)
            {
                if (switchButton.IsChecked.HasValue)
                {
                    isChecked = switchButton.IsChecked.Value;
                    if (isChecked)
                    {
                        App.Manager.AddBlacklist((string)modname.Content);
                        back.Background = new SolidColorBrush(Color.FromRgb(60, 255, 90));
                        switchButton.IsChecked = true;
                    }
                    else
                    {
                        App.Manager.RemoveBlacklist((string)modname.Content);
                        back.Background = new SolidColorBrush(Color.FromRgb(255, 140, 50));
                        switchButton.IsChecked = false;
                    }
                }
            }
        }
    }
}