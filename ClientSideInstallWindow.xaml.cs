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
                InitializeModList();
            }
            catch (Exception ex)
            {
                App.LogStream.Log(new(ex.ToString(), LogSeverity.Critical, ex));
                throw;
            }
        }

        private async void InitializeModList()
        {
            try
            {
                foreach (var optionalMod in App.Manager.OptionalMods)
                    optionalListBox.Items.Add(
                        new ClientSideMod(() => optionalMod.GetImageAsync())
                        {
                            Title = optionalMod.ModName,
                            Description = optionalMod.Description,
                            Icon = await optionalMod.GetIconAsync()
                        }
                    );
            }
            catch (Exception ex) { App.LogStream.Log(new(ex.ToString(), LogSeverity.Error, ex)); }
        }
    }
}