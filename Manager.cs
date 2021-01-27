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
            ConfigsFolder = Path.Combine(MinecraftFolder, "config");
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
            LocalMods = new LinkedList<(string, bool)>();
            MissingMods = new LinkedList<Mod>();
            AllMods = Array.Empty<Mod>();
            OptionalMods = Array.Empty<OptionalMod>();
            ModListChanged = null;
            OptionalModlistChanged = null;
            blacklist = Array.Empty<string>();
            LoadBlacklist();
        }

        public LinkedList<(string, bool)> LocalMods { get; private set; }
        public LinkedList<Mod> MissingMods { get; private set; }
        public IEnumerable<OptionalMod> OptionalMods { get; private set; }
        private IEnumerable<Mod> AllMods { get; set; }
        private string ConfigsFolder { get; }
        private string MinecraftFolder { get; }
        private string ModsFolder { get; }

        public void AddBlacklist(string modname)
        {
            blacklist = blacklist.Concat(new[] { modname });
            LocalMods.Remove(LocalMods.First(c => c.Item1 == modname));
            LocalMods.AddLast((modname, true));
            SaveBlacklist();
        }

        public void DeleteImageCache()
        {
            if (Directory.Exists(Path.Combine(MinecraftFolder, "cachedImages")))
                Directory.Delete(Path.Combine(MinecraftFolder, "cachedImages"), true);
        }

        public async Task DownloadMissingAsync(Action<string, int, int>? listProgress, Action<string, long, long>? dataProgress, CancellationToken token)
        {
            if (!Directory.Exists(ModsFolder))
                Directory.CreateDirectory(ModsFolder);
            int count = MissingMods.Count;
            int modNb = 1;
            for (var it = MissingMods.First; it != null;)
            {
                listProgress?.Invoke(it.Value.ModName, modNb, count);
                using var localFile = new BufferedStream(new FileStream(Path.Combine(ModsFolder, it.Value.ModName), FileMode.Create, FileAccess.Write), 1024 << 5);
                long size = await Task.Run(() => client.Get($"/home/mcftp/server2/mods/{it.Value.ModName}").Length);
                using var serverFile = await Task.Run(() => client.OpenRead($"/home/mcftp/server2/mods/{it.Value.ModName}"));
                long writtenBytes = 0;
                while (writtenBytes < size)
                {
                    byte[] bytes = new byte[1024 << 2];
                    int written = await serverFile.ReadAsync(bytes.AsMemory(0, 1024 << 2), token);
                    if (token.IsCancellationRequested)
                    {
                        localFile.Close();
                        File.Delete(Path.Combine(ModsFolder, it.Value.ModName));
                        await CheckModsAsync();
                        return;
                    }
                    localFile.Write(bytes, 0, written);
                    writtenBytes += written;
                    dataProgress?.Invoke(it.Value.ModName, writtenBytes, size);
                }
                localFile.Flush();
                modNb++;
                var cpyIt = it.Next;
                MissingMods.Remove(it);
                it = cpyIt;
            }
        }

        public async Task DownloadOptionalAsync(IEnumerable<OptionalMod> mods, Action<string, int, int>? listProgress, Action<string, long, long>? dataProgress, CancellationToken token)
        {
            if (!Directory.Exists(ModsFolder))
                Directory.CreateDirectory(ModsFolder);
            int nbMod = 1;
            int count = mods.Count();
            foreach (var mod in mods)
            {
                listProgress?.Invoke(mod.ModName, nbMod, count);
                using var localFile = new BufferedStream(new FileStream(Path.Combine(ModsFolder, mod.ModName), FileMode.Create, FileAccess.Write), 1024 << 5);
                long size = await Task.Run(() => client.Get($"/home/mcftp/server2/" + (mod.ClientOnly ? "clientMods" : "mods") + $"/{mod.ModName}").Length);
                using var serverFile = await Task.Run(() => client.OpenRead($"/home/mcftp/server2/" + (mod.ClientOnly ? "clientMods" : "mods") + $"/{mod.ModName}"));
                long writtenBytes = 0;
                while (writtenBytes < size)
                {
                    byte[] bytes = new byte[1024 << 2];
                    int written = await serverFile.ReadAsync(bytes.AsMemory(0, 1024 << 2), token);
                    if (token.IsCancellationRequested)
                    {
                        localFile.Close();
                        File.Delete(Path.Combine(ModsFolder, mod.ModName));
                        return;
                    }
                    localFile.Write(bytes, 0, written);
                    writtenBytes += written;
                    dataProgress?.Invoke(mod.ModName, writtenBytes, size);
                }
                localFile.Flush();
                mod.Installed = true;
                nbMod++;
            }
        }

        public async Task ImportConfigAsync(Action<int, int>? countProgress, Action<string, int, int>? listProgress, Action<string, long, long>? dataProgress, CancellationToken token)
        {
            var relativeTo = $"/home/mcftp/server2/config";
            var count = 0;
            async Task<int> recursiveCount(string folder)
            {
                var files = await Task.Run(() => from f in client.ListDirectory(folder) where f is not { Name: "." or ".." } select f, token);
                int c = 0;
                foreach (var item in files)
                {
                    if (item.IsDirectory)
                        c += await recursiveCount(item.FullName);
                    else
                    {
                        countProgress?.Invoke(0, count);
                        c++;
                    }
                }
                count += c;
                return c;
            }
            var nbMod = 1;
            count = await recursiveCount(relativeTo);
            async Task ImportFolder(string folder)
            {
                var files = await Task.Run(() => from f in client.ListDirectory(folder) where f is not { Name: "." or ".." } select f, token);
                foreach (var file in from entry in files where entry.IsDirectory select entry.FullName)
                {
                    var localFolder = file[(relativeTo.Length + 1)..];
                    {
                        var folderpath = string.IsNullOrEmpty(localFolder) ? ConfigsFolder : Path.Combine(ConfigsFolder, localFolder);
                        if (!Directory.Exists(folderpath))
                            Directory.CreateDirectory(folderpath);
                    }
                    await ImportFolder(file);
                    if (token.IsCancellationRequested)
                        return;
                }
                foreach (var file in from entry in files where !entry.IsDirectory select entry.FullName)
                {
                    var localRelativeDirectory = Path.GetDirectoryName(file[(relativeTo.Length + 1)..]);
                    var fileName = Path.GetFileName(file);
                    var localFilePath = string.IsNullOrEmpty(localRelativeDirectory) ? Path.Combine(ConfigsFolder, fileName) : Path.Combine(ConfigsFolder, localRelativeDirectory, fileName);
                    using var localFile = new FileStream(localFilePath, FileMode.Create, FileAccess.Write);
                    using var distantFile = await Task.Run(() => client.OpenRead(file), token);
                    if (token.IsCancellationRequested)
                    {
                        localFile.Close();
                        File.Delete(localFilePath);
                        return;
                    }
                    listProgress?.Invoke(fileName, nbMod, count);
                    long size = await Task.Run(() => client.Get(file).Length);
                    long writtenBytes = 0;
                    while (writtenBytes < size)
                    {
                        byte[] bytes = new byte[1024 << 2];
                        int written = await distantFile.ReadAsync(bytes.AsMemory(0, 1024 << 2), token);
                        if (token.IsCancellationRequested)
                        {
                            localFile.Close();
                            File.Delete(localFilePath);
                            return;
                        }
                        localFile.Write(bytes, 0, written);
                        writtenBytes += written;
                        dataProgress?.Invoke(fileName, writtenBytes, size);
                    }
                    localFile.Flush();
                    nbMod++;
                }
            }
            await ImportFolder(relativeTo);
            var serverScripts = $"/home/mcftp/server2/scripts";
            var localScripts = Path.Combine(MinecraftFolder, "scripts");
            if (!Directory.Exists(localScripts))
                Directory.CreateDirectory(localScripts);
            var scripts = await Task.Run(() => client.ListDirectory(serverScripts).Where(s => !s.IsDirectory));
            count = scripts.Count();
            nbMod = 1;
            foreach (var file in scripts)
            {
                listProgress?.Invoke(file.Name, nbMod, count);
                using var localFile = new BufferedStream(new FileStream(Path.Combine(localScripts, file.Name), FileMode.Create, FileAccess.Write), 1024 << 5);
                long size = await Task.Run(() => client.Get(file.FullName).Length);
                using var serverFile = await Task.Run(() => client.OpenRead(file.FullName));
                long writtenBytes = 0;
                while (writtenBytes < size)
                {
                    byte[] bytes = new byte[1024 << 2];
                    int written = await serverFile.ReadAsync(bytes.AsMemory(0, 1024 << 2), token);
                    if (token.IsCancellationRequested)
                    {
                        localFile.Close();
                        File.Delete(Path.Combine(localScripts, file.Name));
                        await CheckModsAsync();
                        return;
                    }
                    localFile.Write(bytes, 0, written);
                    writtenBytes += written;
                    dataProgress?.Invoke(file.Name, writtenBytes, size);
                }
                localFile.Flush();
                nbMod++;
            }
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
            LocalMods.AddLast((modname, false));
            SaveBlacklist();
        }

        public void RemoveLocalMods()
        {
            for (var it = LocalMods.First; it != null;)
            {
                var tmpIt = it.Next;
                if (!it.Value.Item2)
                {
                    File.Delete(Path.Combine(ModsFolder, it.Value.Item1));
                    LocalMods.Remove(it);
                }
                it = tmpIt;
            }
        }

        public void RemoveOptional(OptionalMod mod)
        {
            File.Delete(Path.Combine(ModsFolder, mod.ModName));
            mod.Installed = false;
        }

        public async Task UpdateOfflineSkinAsync(CancellationToken token)
        {
            if (!Directory.Exists(ConfigsFolder))
                Directory.CreateDirectory(ConfigsFolder);
            using var serverFile = await Task.Run(() => client.OpenRead("/home/mcftp/server2/config/offlineskins.cfg"));
            using var localFile = new FileStream(Path.Combine(ConfigsFolder, "offlineskins.cfg"), FileMode.Create, FileAccess.Write);
            await serverFile.CopyToAsync(localFile, token);
            if (token.IsCancellationRequested)
            {
                localFile.Close();
                File.Delete(Path.Combine(ConfigsFolder, "offlineskins.cfg"));
                return;
            }
            localFile.Flush();
        }

        private async Task CheckModsAsync()
        {
            var localMods = from file in Directory.GetFiles(ModsFolder) select Path.GetFileName(file);
            var list = new LinkedList<Mod>();
            MissingMods = list;
            foreach (var m in AllMods)
            {
                if (m is OptionalMod om)
                    om.Installed = localMods.Any(name => name == m.ModName);
                else if (localMods.Contains(m.ModName))
                {
                    if (new FileInfo(Path.Combine(ModsFolder, m.ModName)).Length != await Task.Run(() => client.Get($"/home/mcftp/server2/mods/{m.ModName}").Length))
                        list.AddLast(m);
                }
                else
                    list.AddLast(m);
            }
            LocalMods = new LinkedList<(string, bool)>((from name in localMods where AllMods.All(m => m.ModName != name) select (name, blacklist.Any(m => m == name))));
        }

        private void LoadBlacklist()
        {
            try
            {
                using var stream = new StreamReader("clientside.txt");
                blacklist = from mod in stream.ReadToEnd().Split('\n') where !string.IsNullOrWhiteSpace(mod) select mod.Trim();
            }
            catch (IOException)
            {
                blacklist = Array.Empty<string>();
            }
        }

        private async Task RefreshAsync()
        {
            if (!Directory.Exists(ModsFolder))
                Directory.CreateDirectory(ModsFolder);
            var optMods = Newtonsoft.Json.JsonConvert.DeserializeObject<FtpMod[]>(await Task.Run(() => client.ReadAllText("/home/mcftp/server2/optionalMods.json")));
            var list = new LinkedList<Mod>();
            foreach (var item in await Task.Run(() => client.ListDirectory("/home/mcftp/server2/mods")))
                if (!item.IsDirectory)
                    if (!optMods.Any(m => m.name == item.Name))
                        list.AddLast(new Mod(item.Name));
            foreach (var item in optMods)
            {
                list.AddLast(
                    new OptionalMod(item.name, item.description)
                    {
                        ClientOnly = item.clientOnly,
                        Icon = item.icon != null ? () => Task.Run(() => client.ReadAllBytes($"/home/mcftp/server2/optionalModsImages/{item.icon}")) : null,
                        Image = item.image != null ? () => Task.Run(() => client.ReadAllBytes($"/home/mcftp/server2/optionalModsImages/{item.image}")) : null
                    });
            }
            OptionalMods = from mod in list where mod is OptionalMod select mod as OptionalMod;
            AllMods = list;
        }

        private void SaveBlacklist()
        {
            using var stream = new StreamWriter("clientside.txt", false);
            foreach (var item in blacklist)
                stream.WriteLine(item);
            stream.Flush();
        }

        public class DownloadCancelation
        {
            public Action? Cancel = null;
        }

        private class FtpMod
        {
            public bool clientOnly = false;
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

        public bool ClientOnly { get; init; }
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