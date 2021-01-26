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
                InitializeComponent();
            }
            catch (Exception ex)
            {
                App.LogStream.Log(new(ex.ToString(), LogSeverity.Critical, ex));
                throw;
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new LoadingWindow();
                dialog.Owner = this;
                var ts = new CancellationTokenSource();
                CancellationToken ct = ts.Token;
                dialog.Canceled += ts.Cancel;
                App.Manager.DeleteImageCache();
                App.LogStream.Log(new LogMessage("Image cache deleted"));
                var t1 = App.Manager.UpdateOfflineSkinAsync(ct).ContinueWith(t => App.LogStream.Log(new LogMessage("Offline skin config updated.")));
                App.LogStream.Log(new LogMessage("Updating offline skin config..."));
                App.LogStream.Log(new LogMessage("Updating mod files..."));
                var t2 = App.Manager.DownloadMissingAsync(
                    (mod, n, max) =>
                        Dispatcher.Invoke(() =>
                        {
                            try
                            {
                                dialog.globalProgressionText.Content = $"{n}/{max} ({n * 100 / max}%)";
                                dialog.globalProgressionBar.Value = n * 100 / max;
                                App.LogStream.Log(new($"Downloading {mod}..."));
                            }
                            catch (Exception ex) { App.LogStream.Log(new(ex.ToString(), LogSeverity.Error, ex)); }
                        }),
                    (mod, current, max) =>
                        Dispatcher.Invoke(() =>
                        {
                            try
                            {
                                dialog.fileProgressionText.Content = $"{mod} ({current >> 10} / {max >> 10} Ko)";
                                dialog.fileProgressionBar.Value = (current >> 10) * 100 / (max >> 10);
                            }
                            catch (Exception ex) { App.LogStream.Log(new(ex.ToString(), LogSeverity.Error, ex)); }
                        }), ct);
                var globalTask = Task.WhenAll(t1, t2).ContinueWith(t =>
                {
                    Dispatcher.Invoke(dialog.Close);
                    Dispatcher.Invoke(CheckProblems);
                });
                dialog.ShowDialog();
                await globalTask;
                App.LogStream.Log(new LogMessage("Finished updating files."));
            }
            catch (Exception ex) { App.LogStream.Log(new(ex.ToString(), LogSeverity.Error, ex)); }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            new ClientSideInstallWindow { Owner = this }.ShowDialog();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            try
            {
                DisplayBlacklist();
            }
            catch (Exception ex) { App.LogStream.Log(new(ex.ToString(), LogSeverity.Error, ex)); }
        }

        private async void Button_Click_3(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new LoadingWindow();
                dialog.Owner = this;
                var ts = new CancellationTokenSource();
                CancellationToken ct = ts.Token;
                dialog.Canceled += ts.Cancel;
                dialog.globalProgressionBar.Value = 0;
                var t = App.Manager.ImportConfigAsync(
                    (n, max) =>
                        Dispatcher.Invoke(() =>
                        {
                            try
                            {
                                dialog.globalProgressionText.Content = $"{n}/{max} ({n * 100 / Math.Max(max, 1)}%)";
                            }
                            catch (Exception ex) { App.LogStream.Log(new(ex.ToString(), LogSeverity.Error, ex)); }
                        }),
                    (config, n, max) =>
                        Dispatcher.Invoke(() =>
                        {
                            try
                            {
                                dialog.globalProgressionText.Content = $"{n}/{max} ({n * 100 / max}%)";
                                dialog.globalProgressionBar.Value = n * 100 / max;
                                App.LogStream.Log(new($"Downloading {config}..."));
                            }
                            catch (Exception ex) { App.LogStream.Log(new(ex.ToString(), LogSeverity.Error, ex)); }
                        }),
                    (config, current, max) =>
                        Dispatcher.Invoke(() =>
                        {
                            try
                            {
                                current >>= 10;
                                max >>= 10;
                                if (current < 1)
                                    current = 1;
                                if (max < 1)
                                    max = 1;
                                dialog.fileProgressionText.Content = $"{config} ({current} / {max} Ko)";
                                dialog.fileProgressionBar.Value = current * 100 / max;
                            }
                            catch (Exception ex) { App.LogStream.Log(new(ex.ToString(), LogSeverity.Error, ex)); }
                        }), ct).ContinueWith(t =>
                        {
                            Dispatcher.Invoke(dialog.Close);
                            Dispatcher.Invoke(CheckProblems);
                        });

                dialog.ShowDialog();
                await t;
            }
            catch (Exception ex) { App.LogStream.Log(new(ex.ToString(), LogSeverity.Error, ex)); }
        }

        private void CheckProblems()
        {
            try
            {
                var missing = App.Manager.MissingMods.Count();
                if (missing > 0)
                {
                    infoControl.Mode = InstallationInfo.DisplayMode.ERROR;
                    infoControl.Text = missing == 1 ? $"{missing} mod est manquant. Il faut mettre à jour." : $"{missing} mods sont manquants. Il faut mettre à jour.";
                    infoControl.Click = null;
                    infoControl.Clickable = false;
                    return;
                }
                var conflicts = App.Manager.LocalMods.Sum(c => c.Item2 ? 0 : 1);
                if (conflicts > 0)
                {
                    infoControl.Mode = InstallationInfo.DisplayMode.WARNING;
                    infoControl.Text = conflicts == 1 ? $"{conflicts} mod est en trop. Cliquez pour choisir l'action à réaliser." : $"{conflicts} mods sont en trop. Cliquez pour choisir l'action à réaliser.";
                    infoControl.Click = DisplayBlacklist;
                    infoControl.Clickable = true;
                    return;
                }
                infoControl.Mode = InstallationInfo.DisplayMode.VALIDATE;
                infoControl.Text = "Installation correcte";
                infoControl.Click = null;
                infoControl.Clickable = false;
            }
            catch (Exception ex) { App.LogStream.Log(new(ex.ToString(), LogSeverity.Error, ex)); }
        }

        private void DisplayBlacklist()
        {
            var dialog = new Blacklist { Owner = this };
            dialog.ShowDialog();
            CheckProblems();
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
                App.LogStream.Log(new("Analyzing missing files..."));
                infoControl.Mode = InstallationInfo.DisplayMode.INFO;
                infoControl.Text = "Analyse de l'installation...";
                infoControl.Click = null;
                infoControl.Clickable = false;
                await App.Manager.InitAsync();
                App.LogStream.Log(new($"Found {App.Manager.MissingMods.Count()} missing mods."));
                IsEnabled = true;
            }
            catch (Exception ex) { App.LogStream.Log(new(ex.ToString(), LogSeverity.Error, ex)); }
            CheckProblems();
        }
    }
}