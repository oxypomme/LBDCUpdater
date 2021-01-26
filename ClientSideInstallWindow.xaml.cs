using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace LBDCUpdater
{
    /// <summary>
    /// Logique d'interaction pour Window1.xaml
    /// </summary>
    public partial class ClientSideInstallWindow : Window
    {
        public ClientSideInstallWindow()
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
            var toInstall = new LinkedList<OptionalMod>();
            var toRemove = new LinkedList<OptionalMod>();
            foreach (var mod in optionalListBox.Children.Cast<ClientSideMod>())
            {
                var distantMod = App.Manager.OptionalMods.First(m => m.ModName == mod.Title);
                if (mod.IsChecked && !distantMod.Installed)
                    toInstall.AddLast(distantMod);
                if (!mod.IsChecked && distantMod.Installed)
                    toRemove.AddLast(distantMod);
            }
            foreach (var mod in toRemove)
                App.Manager.RemoveOptional(mod);
            var dialog = new LoadingWindow();
            dialog.Owner = this;
            var ts = new CancellationTokenSource();
            CancellationToken ct = ts.Token;
            dialog.Canceled += ts.Cancel;
            var t = App.Manager.DownloadOptionalAsync(toInstall, (modname, curr, max) =>
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        dialog.globalProgressionText.Content = $"{curr}/{max} ({curr * 100 / max}%)";
                        dialog.globalProgressionBar.Value = curr * 100 / max;
                        App.LogStream.Log(new($"Downloading {modname}..."));
                    }
                    catch (Exception ex) { App.LogStream.Log(new(ex.ToString(), LogSeverity.Error, ex)); }
                }), (modname, curr, max) =>
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        dialog.fileProgressionText.Content = $"{modname} ({curr >> 10} / {max >> 10} Ko)";
                        dialog.fileProgressionBar.Value = (curr >> 10) * 100 / (max >> 10);
                    }
                    catch (Exception ex) { App.LogStream.Log(new(ex.ToString(), LogSeverity.Error, ex)); }
                }), ct).ContinueWith(t =>
                {
                    Dispatcher.Invoke(dialog.Close);
                    Dispatcher.Invoke(() => MessageBox.Show("Mods mis à jour", "Téléchargement", MessageBoxButton.OK, MessageBoxImage.Information));
                });
            dialog.ShowDialog();
            await t;
        }

        private async void Window_Initialized(object sender, EventArgs e)
        {
            foreach (var optionalMod in App.Manager.OptionalMods)
            {
                var modControl = new ClientSideMod(() => optionalMod.GetImageAsync())
                {
                    Title = optionalMod.ModName,
                    Description = optionalMod.Description,
                    IsChecked = optionalMod.Installed
                };
                await optionalMod.GetIconAsync().ContinueWith(t => Dispatcher.Invoke(() => modControl.Icon = t.Result));
                optionalListBox.Children.Add(modControl);
            }
        }
    }
}