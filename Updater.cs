﻿using System;
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
                var github = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("DofLog"));
                var lastRelease = await github.Repository.Release.GetLatest(270258000);
                var current = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                if (new Version(lastRelease.TagName) > current)
                {
                    var result = MessageBox.Show("Une nouvelle version du logiciel est disponible. Voulez-vous la télécharger ?", "Updater", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (result == MessageBoxResult.Yes)
                    {
                        WebClient webClient = new WebClient();
                        webClient.DownloadFile(new Uri("https://github.com/oxypomme/LBDCUpdater/releases/latest/download/LBDCUpdater.exe"), "NewLBDCUpdater.exe");
                        //TODO: Finir updater
                        Environment.Exit(0);
                    }
                }
                else if (new Version(lastRelease.TagName) < current)
                {
                    var result = MessageBox.Show("Cette version de DofLog est expérimentale : de nombreux bugs peuvent survenir et les nouvelles fonctionnalités peuvent ne pas être prêtes à l'utilisation. Voulez-vous continuer ?", "Updater", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.Yes);
                    if (result == MessageBoxResult.No)
                    {
                        Environment.Exit(1);
                    }
                }
            }
            catch (Exception e) { }
        }
    }
}