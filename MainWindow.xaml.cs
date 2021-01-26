using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            try
            {
                Manager = new Manager();
            }
            catch (Exception ex) { App.LogStream.Log(new(ex.ToString(), LogSeverity.Error, ex)); }
            InitializeComponent();
        }

        private Manager Manager { get; }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new LoadingWindow();
                dialog.Owner = this;
                var ts = new CancellationTokenSource();
                CancellationToken ct = ts.Token;
                dialog.Canceled += ts.Cancel;
                var t = Manager.DownloadMissingAsync(
                    (mod, n, max) =>
                        Dispatcher.Invoke(() =>
                        {
                            dialog.globalProgressionText.Content = $"{n}/{max} ({n * 100 / max}%)";
                            dialog.globalProgressionBar.Value = n * 100 / max;
                            App.LogStream.Log(new($"Downloading {mod}..."));
                        }),
                    (mod, current, max) =>
                        Dispatcher.Invoke(() =>
                        {
                            dialog.fileProgressionText.Content = $"{mod} ({current >> 10} / {max >> 10} Ko)";
                            dialog.fileProgressionBar.Value = (current >> 10) * 100 / (max >> 10);
                        }), ct
                ).ContinueWith(t => Dispatcher.Invoke(dialog.Close));

                dialog.ShowDialog();
                await t;
            }
            catch (Exception ex) { App.LogStream.Log(new(ex.ToString(), LogSeverity.Error, ex)); }
        }

        private void Forge_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        UseShellExecute = true,
                        FileName = "https://files.minecraftforge.net/maven/net/minecraftforge/forge/1.12.2-14.23.5.2854/forge-1.12.2-14.23.5.2854-installer.jar"
                    }
                }.Start();
            }
            catch (Exception ex) { App.LogStream.Log(new(ex.ToString(), LogSeverity.Error, ex)); }
        }

        private async void Window_Initialized(object sender, EventArgs e)
        {
            try
            {
                App.LogStream.Log(new("Analizing missing files..."));
                await Manager.InitAsync();
                App.LogStream.Log(new($"Found {Manager.MissingMods.Count()} missing mods."));
                IsEnabled = true;
            }
            catch (Exception ex) { App.LogStream.Log(new(ex.ToString(), LogSeverity.Error, ex)); }
        }
    }
}