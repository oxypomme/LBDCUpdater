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
            int n = 1;
            foreach (var mod in toInstall)
            {
                var t = App.Manager.DownloadOptionalAsync(mod, (curr, max) =>
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        dialog.globalProgressionText.Content = $"{n}/{toInstall.Count} ({n * 100 / toInstall.Count}%)";
                        dialog.globalProgressionBar.Value = n * 100 / toInstall.Count;
                        dialog.fileProgressionText.Content = $"{mod.ModName} ({curr >> 10} / {max >> 10} Ko)";
                        dialog.fileProgressionBar.Value = (curr >> 10) * 100 / (max >> 10);
                        App.LogStream.Log(new($"Downloading {mod.ModName}..."));
                    }
                    catch (Exception ex) { App.LogStream.Log(new(ex.ToString(), LogSeverity.Error, ex)); }
                }), ct);
                n++;
                await t;
            }
            MessageBox.Show("Mods mis à jour", "Téléchargement", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void Window_Initialized(object sender, EventArgs e)
        {
            foreach (var optionalMod in App.Manager.OptionalMods)
                optionalListBox.Children.Add(
                    new ClientSideMod(() => optionalMod.GetImageAsync())
                    {
                        Title = optionalMod.ModName,
                        Description = optionalMod.Description,
                        Icon = await optionalMod.GetIconAsync(),
                        IsChecked = optionalMod.Installed
                    }
                );
        }
    }
}