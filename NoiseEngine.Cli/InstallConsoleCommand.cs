using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NoiseEngine.Cli;
using NoiseEngine.Cli.Versions;

public class InstallConsoleCommand : IConsoleCommand {

    private readonly Settings settings;

    public string Name => "install";
    public string[] Aliases => Array.Empty<string>();
    public string Description => "Installs NoiseEngine versions.";
    public string Usage => $"{ConsoleCommandUtils.ExeName} {Name} [OPTIONS]";

    public ConsoleCommandOption[] Options { get; } = {
        new ConsoleCommandOption(
            new string[] { "--force", "-f" },
            "Forces the installation of the specified version, even if it is already installed.")
    };

    public string LongDescription =>
        $"Use `{ConsoleCommandUtils.ExeName} versions list` to list installed versions " +
        $"and `{ConsoleCommandUtils.ExeName} versions available` to list available versions.";

    public InstallConsoleCommand(Settings settings) {
        this.settings = settings;
    }

    public bool Execute(ReadOnlySpan<string> args) {
        if (args.Length is 0 or > 2) {
            InvalidUsageMessage("Invalid usage.");
            return false;
        }

        if (args.Length == 1) {
            return InstallVersion(args[0], false);
        }

        if (args[0] != "--force" && args[0] != "-f") {
            InvalidUsageMessage("Invalid usage.");
            return false;
        }

        return InstallVersion(args[1], true).Result;
    }

    private void InvalidUsageMessage(string error) {
         ConsoleCommandUtils.WriteLineError(error);
         Console.WriteLine();
         Console.WriteLine($"Usage: `{Usage}`");
     }

    private async Task<bool> InstallVersion(string version, bool force) {
        VersionIndex? index = await VersionUtils.DownloadIndex(settings);

        if (index is null) {
            return false;
        }

        VersionInfo? vi = index.Versions.FirstOrDefault(x => x.Version == version);

        if (vi is null) {
            ConsoleCommandUtils.WriteLineError($"Version `{version}` not found.");
            return false;
        }

        if (VersionUtils.IsInstalled(settings, vi.Version)) {
            if (force) {
                VersionUtils.Uninstall(settings, vi.Version);
            } else {
                ConsoleCommandUtils.WriteLineError($"Version `{version}` is already installed.");
                return false;
            }
        }

        VersionDetails? details = await VersionUtils.DownloadDetails(settings, vi.Version);

        if (details is null) {
            return false;
        }

        Console.WriteLine($"Installing version `{vi.Version}`...");

        string zipFile = await ConsoleCommandUtils.TryDownloadFile(new Uri(details.));
    }

}
// using System;
// using System.IO;
// using System.IO.Compression;
// using System.Linq;
// using System.Net;
// using System.Net.Http;
// using System.Text.Json;
// using System.Threading.Tasks;
//
// namespace NoiseEngine.Cli;
//
// public class InstallConsoleCommand : IConsoleCommand {
//
//     private readonly Settings settings;
//
//     public string Name => "install";
//     public string[] Aliases { get; } = Array.Empty<string>();
//     public string Description => "Installing and displaying information about NoiseEngine versions.";
//
//     public string Usage =>
//         $"{ConsoleCommandUtils.ExeName} {Name} <VERSION|latest> [options] | " +
//         $"{ConsoleCommandUtils.ExeName} {Name} <list|available|l|a>";
//
//     public ConsoleCommandOption[] Options { get; } = {
//         new ConsoleCommandOption(
//             new[] { "--force", "-f" },
//             "Forces the installation of the specified version, even if it is already installed."),
//     };
//
//     public string LongDescription =>
//         $"Use `{ConsoleCommandUtils.ExeName} {Name} <VERSION|latest>` to install a version of NoiseEngine.\n" +
//         $"Use `{ConsoleCommandUtils.ExeName} {Name} <list|l>` to list installed versions.\n" +
//         $"Use `{ConsoleCommandUtils.ExeName} {Name} <available|a>` to list available versions.\n";
//
//     public InstallConsoleCommand(Settings settings) {
//         this.settings = settings;
//     }
//
//     public bool Execute(ReadOnlySpan<string> args) {
//         if (args.Length is 0 or > 2) {
//             InvalidUsageMessage("Invalid usage.");
//             return false;
//         }
//
//         string version = args[0];
//
//         switch (version) {
//             case "list":
//             case "l":
//                 if (args.Length > 1) {
//                     InvalidUsageMessage("Too many arguments.");
//                     return false;
//                 }
//
//                 ListInstalledVersions();
//                 return true;
//             case "available":
//             case "a":
//                 if (args.Length > 1) {
//                     InvalidUsageMessage("Too many arguments.");
//                     return false;
//                 }
//
//                 return ListAvailableVersions();
//             default:
//                 if (args.Length == 1) {
//                     return InstallVersion(version, false);
//                 }
//
//                 if (args.Contains("--force") || args.Contains("-f"))
//                     return InstallVersion(version, true);
//
//                 InvalidUsageMessage($"Invalid argument: `{args[1]}.");
//                 return false;
//         }
//     }
//
//     private void InvalidUsageMessage(string error) {
//         ConsoleCommandUtils.WriteLineError(error);
//         Console.WriteLine();
//         Console.WriteLine($"Usage: `{Usage}`");
//     }
//
//     private bool InstallVersion(string version, bool force) {
//         string[] installed = GetInstalledVersions();
//         InstallIndex? index = GetInstallIndex().Result;
//
//         if (index is null) {
//             ConsoleCommandUtils.WriteLineError("Could not deserialize index.");
//             return false;
//         }
//
//         if (version == "latest") {
//             version = index.Latest;
//         }
//
//         if (!force && installed.Contains(version)){
//             ConsoleCommandUtils.WriteLineError($"Version `{version}` is already installed.");
//             return false;
//         }
//
//         if (!index.Versions.Contains(version)) {
//             ConsoleCommandUtils.WriteLineError($"Version `{version}` not found.");
//             return false;
//         }
//
//         string root = settings.InstallDirectory;
//
//         if (!Path.IsPathRooted(root)) {
//             root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, root);
//         }
//
//         if (!Directory.Exists(root)) {
//             Directory.CreateDirectory(root);
//         }
//
//         string path = root;
//
//         path = Path.Combine(path, version);
//         path += ".zip";
//
//         if (File.Exists(path)) {
//             File.Delete(path);
//         }
//
//         string? result = DownloadFile(version + ".zip", path).Result;
//
//         if (result is null) {
//             ConsoleCommandUtils.WriteLineError($"Could not download version `{version}`.");
//             return false;
//         }
//
//         Console.WriteLine($"Downloaded version `{version}.zip`.");
//         Console.WriteLine("Unpacking...");
//
//         if (Directory.Exists(Path.Combine(root, version))) {
//             Directory.Delete(Path.Combine(root, version), true);
//         }
//
//         try {
//             ZipFile.ExtractToDirectory(path, root);
//         } catch (Exception e) {
//             ConsoleCommandUtils.WriteLineError($"Could not unpack version `{version}`.");
//             Console.WriteLine(e);
//             return false;
//         }
//
//         Console.WriteLine($"Unpacked version `{version}`.");
//         File.Delete(path);
//
//         return true;
//     }
//
//     private bool ListAvailableVersions() {
//         InstallIndex? index = GetInstallIndex().Result;
//
//         if (index is null) {
//             ConsoleCommandUtils.WriteLineError("Could not deserialize index.");
//             return false;
//         }
//
//         string[] installed = GetInstalledVersions();
//
//         Console.WriteLine("Available versions:");
//
//         foreach (string version in index.Versions) {
//             string line = $"{version}";
//
//             if (version == index.Latest) {
//                 line += " (latest)";
//             }
//
//             if (installed.Contains(version)) {
//                 line += " (installed)";
//             }
//
//             Console.WriteLine(ConsoleCommandUtils.Indent(line));
//         }
//
//         return true;
//     }
//
//     private void ListInstalledVersions() {
//         string[] installedVersions = GetInstalledVersions();
//
//         if (installedVersions.Length == 0) {
//             Console.WriteLine("No versions installed.");
//             return;
//         }
//
//         Console.WriteLine("Installed versions:");
//         Console.WriteLine(ConsoleCommandUtils.Indent(string.Join('\n', GetInstalledVersions())));
//     }
//
//     private string[] GetInstalledVersions() {
//         string path = settings.InstallDirectory;
//
//         if (!Path.IsPathRooted(path)) {
//             path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
//         }
//
//         if (!Directory.Exists(path))
//             return Array.Empty<string>();
//
//         return Directory.GetDirectories(path)
//             .Select(Path.GetFileName)
//             .ToArray()!;
//     }
//
//     private async Task<InstallIndex?> GetInstallIndex() {
//         string? result = await DownloadFile("index.json");
//
//         if (result is null) {
//             return null;
//         }
//
//         byte[] data = await File.ReadAllBytesAsync(result);
//         File.Delete(result);
//         return JsonSerializer.Deserialize<InstallIndex>(data);
//     }
//
//     /// <summary>
//     /// Returns path to the temporary file. Null if could not download.
//     /// </summary>
//     private async Task<string?> DownloadFile(string name, string? path = null) {
//         Console.WriteLine($"Downloading file `{name}`...");
//         using HttpClient client = new HttpClient();
//         Uri uri = new Uri(settings.InstallUrl + name);
//         using HttpResponseMessage response = await client.GetAsync(uri);
//
//         if (response.StatusCode != HttpStatusCode.OK) {
//             ConsoleCommandUtils.WriteLineError(
//                 $"Could not access file at `{uri}`. Status code: `{(int)response.StatusCode} {response.StatusCode}`.");
//             return null;
//         }
//
//         if (response.Content.Headers.ContentLength is null) {
//             ConsoleCommandUtils.WriteLineWarning("Could not determine file size. This is likely a bug on the server.");
//         }
//
//         path ??= Path.GetTempFileName();
//         await using FileStream file = File.Create(path);
//         await using Stream stream = await response.Content.ReadAsStreamAsync();
//
//         byte[] buffer = new byte[4096];
//         int read;
//         long? total = response.Content.Headers.ContentLength;
//         long totalRead = 0;
//
//         while ((read = await stream.ReadAsync(buffer)) > 0) {
//             await file.WriteAsync(buffer.AsMemory(0, read));
//             totalRead += read;
//
//             if (total is null)
//                 continue;
//
//             if (total < 1024) {
//                 ConsoleCommandUtils.UpdateProgressBar(totalRead, total.Value);
//                 Console.Write($" {totalRead}/{total} bytes");
//             } else if (total < 1024 * 1024) {
//                 ConsoleCommandUtils.UpdateProgressBar(totalRead, total.Value);
//                 Console.Write($" {totalRead / 1024:F2}/{total / 1024:F2} KiB");
//             } else {
//                 ConsoleCommandUtils.UpdateProgressBar(totalRead, total.Value);
//                 Console.Write($" {totalRead / 1024 / 1024:F2}/{total / 1024 / 1024:F2} MiB");
//             }
//         }
//
//         if (response.Content.Headers.ContentLength is not null) {
//             Console.WriteLine();
//             Console.WriteLine();
//         }
//
//         return path;
//     }
//
// }
