using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FluentFTP;

namespace LBDCUpdater
{
    internal class Manager
    {
        public EventHandler? ModListChanged;
        public EventHandler? OptionalModlistChanged;
        private FtpClient client;
        private string ip;
        private string pass;
        private string user;

        public Manager()
        {
            using var stream = new StreamReader("connection.txt");
            var ip = stream.ReadLine();
            var user = stream.ReadLine();
            var pass = stream.ReadLine();
            if (ip == null || user == null || pass == null)
                throw new Exception("Invalid file");
            this.ip = ip;
            this.user = user;
            this.pass = pass;
            client = new FtpClient(ip, new System.Net.NetworkCredential(user, pass)) { EncryptionMode = FtpEncryptionMode.Explicit, DataConnectionEncryption = true };
            client.ValidateCertificate += (sender, e) => e.Accept = true;
            client.Connect();
            Conflicts = Array.Empty<Mod>();
            MissingMods = Array.Empty<Mod>();
            OptionalMods = Array.Empty<OptionalMod>();
            ModListChanged = null;
            OptionalModlistChanged = null;
        }

        public Mod[] Conflicts { get; private set; }

        public Mod[] MissingMods { get; private set; }

        public OptionalMod[] OptionalMods { get; private set; }

        public async Task InitAsync()
        {
            await RefreshAsync();
        }

        private async Task RefreshAsync()
        {
            MessageBox.Show(await client.GetWorkingDirectoryAsync());
        }
    }

    internal class Mod
    {
        public Mod(string modname, Stream content) => (ModName, Installed, Content) = (modname, false, content);

        public Stream Content { get; init; }
        public bool Installed { get; set; }
        public string ModName { get; init; }

        public override string ToString() => ModName;
    }

    internal class OptionalMod : Mod
    {
        private ImageSource? iconSource;
        private ImageSource? imageSource;

        public OptionalMod(string modname, Stream content, string? description = null, byte[]? icon = null, byte[]? image = null) : base(modname, content) => (Description, Icon, Image) = (description, icon, image);

        public string? Description { get; init; }
        public byte[]? Icon { get; init; }

        public ImageSource? IconSource
        {
            get
            {
                if (Image == null)
                    return null;
                if (iconSource == null)
                {
                    var image = new BitmapImage();
                    using (var mem = new MemoryStream(Image))
                    {
                        mem.Position = 0;
                        image.BeginInit();
                        image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.UriSource = null;
                        image.StreamSource = mem;
                        image.EndInit();
                    }
                    image.Freeze();
                    iconSource = image;
                }
                return iconSource;
            }
        }

        public byte[]? Image { get; init; }

        public ImageSource? ImageSource
        {
            get
            {
                if (Image == null)
                    return null;
                if (imageSource == null)
                {
                    var image = new BitmapImage();
                    using (var mem = new MemoryStream(Image))
                    {
                        mem.Position = 0;
                        image.BeginInit();
                        image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.UriSource = null;
                        image.StreamSource = mem;
                        image.EndInit();
                    }
                    image.Freeze();
                    imageSource = image;
                }
                return imageSource;
            }
        }
    }
}