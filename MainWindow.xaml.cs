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
            Manager = new Manager();
            InitializeComponent();
        }

        private Manager Manager { get; }

        private async void Button_Click(object sender, RoutedEventArgs e)
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
                        App.LogStream.Log(new($"Downloading {mod}...", LogSeverity.Info));
                    }),
                (mod, current, max) =>
                    Dispatcher.Invoke(() =>
                    {
                        dialog.fileProgressionText.Content = $"{mod} ({current >> 10} / {max >> 10} Ko)";
                        dialog.fileProgressionBar.Value = (current >> 10) * 100 / (max >> 10);
                    }), ct).ContinueWith(t =>
                    {
                        Dispatcher.Invoke(dialog.Close);
                        Dispatcher.Invoke(CheckProblems);
                    });

            dialog.ShowDialog();
            await t;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            //TODO mods optionels
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            DisplayBlacklist();
        }

        private void CheckProblems()
        {
            var missing = Manager.MissingMods.Count();
            if (missing > 0)
            {
                infoControl.Mode = InstallationInfo.DisplayMode.ERROR;
                infoControl.Text = missing == 1 ? $"{missing} mod est manquant. Il faut mettre à jour." : $"{missing} mods sont manquants. Il faut mettre à jour.";
                infoControl.Click = null;
                infoControl.Clickable = false;
                return;
            }
            var conflicts = Manager.LocalMods.Sum(c => c.Item2 ? 0 : 1);
            if (conflicts > 0)
            {
                infoControl.Mode = InstallationInfo.DisplayMode.WARNING;
                infoControl.Text = conflicts == 1 ? $"{conflicts} mod est en trop. Cliquez pour choisir l'action à réaliser." : $"{missing} mods sont en trop. Cliquez pour choisir l'action à réaliser.";
                infoControl.Click = DisplayBlacklist;
                infoControl.Clickable = true;
                return;
            }
            infoControl.Mode = InstallationInfo.DisplayMode.VALIDATE;
            infoControl.Text = "Installation correcte";
            infoControl.Click = null;
            infoControl.Clickable = false;
        }

        private void DisplayBlacklist()
        {
            var dialog = new Blacklist();
            dialog.Owner = this;
            dialog.ShowDialog();
        }

        private void Forge_Click(object sender, RoutedEventArgs e)
                                => new Process
                                {
                                    StartInfo = new ProcessStartInfo
                                    {
                                        UseShellExecute = true,
                                        FileName = "https://files.minecraftforge.net/maven/net/minecraftforge/forge/1.12.2-14.23.5.2854/forge-1.12.2-14.23.5.2854-installer.jar"
                                    }
                                }.Start();

        private async void Window_Initialized(object sender, EventArgs e)
        {
            App.LogStream.Log(new("Analizing missing files...", LogSeverity.Info));
            infoControl.Mode = InstallationInfo.DisplayMode.INFO;
            infoControl.Text = "Analize de l'installation...";
            infoControl.Click = null;
            infoControl.Clickable = false;
            await Manager.InitAsync();
            App.LogStream.Log(new($"Found {Manager.MissingMods.Count()} missing mods.", LogSeverity.Info));
            CheckProblems();
            IsEnabled = true;
        }
    }
}