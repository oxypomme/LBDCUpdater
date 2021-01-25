using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LBDCUpdater
{
    internal class Manager
    {
        public EventHandler? ModListChanged;
        public EventHandler? OptionalModlistChanged;
        private IEnumerable<string> blacklist;
        private SftpClient client;
        private string ip;
        private string pass;
        private string user;

        public Manager()
        {
            MinecraftFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft");
            ModsFolder = Path.Combine(MinecraftFolder, "mods");
            using var stream = new StreamReader("connection.txt");
            var ip = stream.ReadLine();
            var user = stream.ReadLine();
            var pass = stream.ReadLine();
            if (ip == null || user == null || pass == null)
                throw new Exception("Invalid file");
            this.ip = ip;
            this.user = user;
            this.pass = pass;
            client = new SftpClient(new ConnectionInfo(ip,
                                        user,
                                        new PasswordAuthenticationMethod(user, pass)));
            LocalMods = new List<(string, bool)>();
            MissingMods = Array.Empty<Mod>();
            AllMods = Array.Empty<Mod>();
            OptionalMods = Array.Empty<OptionalMod>();
            ModListChanged = null;
            OptionalModlistChanged = null;
            blacklist = Array.Empty<string>();
            LoadBlacklist();
        }

        public List<(string, bool)> LocalMods { get; private set; }
        public IEnumerable<Mod> MissingMods { get; private set; }
        public IEnumerable<OptionalMod> OptionalMods { get; private set; }
        private IEnumerable<Mod> AllMods { get; set; }
        private string MinecraftFolder { get; }
        private string ModsFolder { get; }

        public void AddBlacklist(string modname)
        {
            blacklist = blacklist.Concat(new[] { modname });
            LocalMods.Remove(LocalMods.First(c => c.Item1 == modname));
            LocalMods.Add((modname, true));
            SaveBlacklist();
        }

        public async Task DownloadMissingAsync(Action<string, int, int>? listProgress, Action<long, long>? dataProgress, EventHandler cancelEvent)
        {
            int count = MissingMods.Count();
            int modNb = 1;
            bool abort = false;
            cancelEvent += (sender, e) => abort = true;
            foreach (var mod in MissingMods)
            {
                listProgress?.Invoke(mod.ModName, modNb, count);
                using var localFile = new BufferedStream(new FileStream(Path.Combine(ModsFolder, mod.ModName), FileMode.Create, FileAccess.Write), 1024 << 5);
                long size = await Task.Run(() => client.Get($"/home/mcftp/server2/mods/{mod.ModName}").Length);
                using var serverFile = await Task.Run(() => client.OpenRead($"/home/mcftp/server2/mods/{mod.ModName}"));
                long writtenBytes = 0;
                while (writtenBytes < size)
                {
                    if (abort)
                    {
                        localFile.Close();
                        File.Delete(Path.Combine(ModsFolder, mod.ModName));
                        return;
                    }
                    byte[] bytes = new byte[1024];
                    int written = await serverFile.ReadAsync(bytes.AsMemory(0, 1024));
                    localFile.Write(bytes, 0, written);
                    writtenBytes += written;
                    dataProgress?.Invoke(writtenBytes, size);
                }
                localFile.Flush();
                modNb++;
            }
            await RefreshAsync();
            await CheckModsAsync();
        }

        public async Task DownloadOptionalAsync(OptionalMod mod, Action<long, long>? dataProgress, EventHandler cancelEvent)
        {
            using var localFile = new BufferedStream(new FileStream(Path.Combine(ModsFolder, mod.ModName), FileMode.Create, FileAccess.Write), 1024 << 5);
            long size = await Task.Run(() => client.Get($"/home/mcftp/server2/mods/{mod.ModName}").Length);
            using var serverFile = await Task.Run(() => client.OpenRead($"/home/mcftp/server2/mods/{mod.ModName}"));
            long writtenBytes = 0;
            bool abort = false;
            cancelEvent += (sender, e) => abort = true;
            while (writtenBytes < size)
            {
                if (abort)
                {
                    localFile.Close();
                    File.Delete(Path.Combine(ModsFolder, mod.ModName));
                    return;
                }
                byte[] bytes = new byte[1024];
                int written = await serverFile.ReadAsync(bytes.AsMemory(0, 1024));
                localFile.Write(bytes, 0, written);
                writtenBytes += written;
                dataProgress?.Invoke(writtenBytes, size);
            }
            localFile.Flush();
            mod.Installed = true;
        }

        public async Task InitAsync()
        {
            await Task.Run(client.Connect);
            await RefreshAsync();
            await CheckModsAsync();
        }

        public void RemoveBlacklist(string modname)
        {
            blacklist = blacklist.Except(new[] { modname });
            LocalMods.Remove(LocalMods.First(c => c.Item1 == modname));
            LocalMods.Add((modname, false));
            SaveBlacklist();
        }

        public void RemoveLocalMods()
        {
            foreach (var item in LocalMods)
                if (!item.Item2)
                    File.Delete(Path.Combine(ModsFolder, item.Item1));
        }

        public void RemoveOptional(OptionalMod mod)
        {
            File.Delete(Path.Combine(ModsFolder, mod.ModName));
            mod.Installed = false;
        }

        private async Task CheckModsAsync()
        {
            var localMods = Directory.GetFiles(ModsFolder);
            var list = new LinkedList<Mod>();
            MissingMods = list;
            foreach (var m in AllMods)
            {
                if (m is OptionalMod om)
                    om.Installed = localMods.Any(name => name == m.ModName);
                if (localMods.Contains(m.ModName))
                {
                    if (new FileInfo(Path.Combine(ModsFolder, m.ModName)).Length != await Task.Run(() => client.Get($"/home/mcftp/server2/mods/{m.ModName}").Length))
                        list.AddLast(m);
                }
                else
                    list.AddLast(m);
            }
            LocalMods = (from name in localMods where AllMods.All(m => m.ModName != name) select (name, blacklist.Any(m => m == name))).ToList();
        }

        private void LoadBlacklist()
        {
            try
            {
                using var stream = new StreamReader("clientside.txt");
                blacklist = stream.ReadToEnd().Split('\n');
            }
            catch (IOException)
            {
                blacklist = Array.Empty<string>();
            }
        }

        private async Task RefreshAsync()
        {
            var optMods = Newtonsoft.Json.JsonConvert.DeserializeObject<FtpMod[]>(await Task.Run(() => client.ReadAllText("/home/mcftp/server2/optionalMods.json")));
            var list = new LinkedList<Mod>();
            foreach (var item in await Task.Run(() => client.ListDirectory("/home/mcftp/server2/mods")))
                if (item.Name is not ("." or ".."))
                {
                    var mod = optMods.FirstOrDefault(m => m.name == item.Name);
                    list.AddLast(mod == null ?
                        new Mod(item.Name) :
                        new OptionalMod(mod.name, mod.description)
                        {
                            Icon = mod.icon != null ? () => Task.Run(() => client.ReadAllBytes(mod.icon)) : null,
                            Image = mod.image != null ? () => Task.Run(() => client.ReadAllBytes(mod.image)) : null
                        });
                }
            OptionalMods = from mod in list where mod is OptionalMod select mod as OptionalMod;
            AllMods = list;
        }

        private void SaveBlacklist()
        {
            using var stream = new StreamWriter("clientside.txt");
            foreach (var item in blacklist)
                stream.WriteLine(item);
            stream.Flush();
        }

        private class FtpMod
        {
            public string? description = null;
            public string? icon = null;
            public string? image = null;
            public string name = "";
        };
    }

    internal class Mod
    {
        public Mod(string modname) => ModName = modname;

        public string ModName { get; init; }

        public override string ToString() => ModName;
    }

    internal class OptionalMod : Mod
    {
        private ImageSource? iconSource;
        private ImageSource? imageSource;

        public OptionalMod(string modname, string? description = null, Func<Task<byte[]>>? icon = null, Func<Task<byte[]>>? image = null) : base(modname) => (Description, Icon, Image) = (description, icon, image);

        public string? Description { get; init; }
        public Func<Task<byte[]>>? Icon { private get; init; }
        public Func<Task<byte[]>>? Image { private get; init; }
        public bool Installed { get; set; }

        public async Task<ImageSource?> GetIconAsync()
        {
            if (Icon == null)
                return null;
            if (iconSource == null)
            {
                var image = new BitmapImage();
                using (var mem = new MemoryStream(await Icon()))
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

        public async Task<ImageSource?> GetImageAsync()
        {
            if (Image == null)
                return null;
            if (imageSource == null)
            {
                var image = new BitmapImage();
                using (var mem = new MemoryStream(await Image()))
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