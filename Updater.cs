using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace LBDCUpdater
{
    internal static class Updater
    {
        /// <summary>
        /// Check if there's a new update on GitHub
        /// </summary>
        public async static void CheckUpdates()
        {
            try
            {
                App.LogStream.Log(new("Checking updates..."));
                var github = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("LBDCUpdater"));
                var lastRelease = await github.Repository.Release.GetLatest(332078429);
                var current = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                App.LogStream.Log(new($"Versions : Current = {current}, Latest = {lastRelease.TagName}"));
                if (new Version(lastRelease.TagName) > current)
                {
                    var result = MessageBox.Show("Une nouvelle version du logiciel est disponible. Voulez-vous la télécharger ?", "Updater", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (result == MessageBoxResult.Yes)
                    {
                        WebClient webClient = new WebClient();
                        App.LogStream.Log(new("Downloading update..."));
                        webClient.DownloadFile(new Uri("https://github.com/oxypomme/LBDCUpdater/releases/latest/download/LBDCUpdater-Setup.exe"), "LBDCUpdater-Setup.exe");
                        App.LogStream.Log(new("Installing update..."));
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("cmd.exe /k echo \"Mise à jour en cours...\" timeout 5 > NUL && move LBDCUpdater_new.exe LBDCUpdater.exe && LBDCUpdater.exe"));
                        App.LogStream.Log(new("Restarting..."));
                        Environment.Exit(0);
                    }
                }
                else if (new Version(lastRelease.TagName) < current)
                {
                    var result = MessageBox.Show("Cette version du logiciel est expérimentale : de nombreux bugs peuvent survenir et les nouvelles fonctionnalités peuvent ne pas être prêtes à l'utilisation. Voulez-vous continuer ?", "Updater", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.Yes);
                    if (result == MessageBoxResult.No)
                    {
                        Environment.Exit(1);
                    }
                }
            }
            catch (Exception e) { App.LogStream.Log(new(e.ToString(), LogSeverity.Error, e)); }
        }
    }
}