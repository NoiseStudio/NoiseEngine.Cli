using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace NoiseEngine.Cli.Versions;

public static class VersionUtils {

    public static string IndexCacheFilePath { get; } = Path.Combine(
        ConsoleCommandUtils.MakeRootedWithExeAsBase(Settings.Instance.InstallDirectory),
        "index_cache.json");

    /// <summary>
    /// Uses the cached index if it exists, otherwise downloads it.
    /// </summary>
    public static async Task<VersionIndex?> GetIndex() {

        if (File.Exists(IndexCacheFilePath)) {
            VersionIndex? result = JsonSerializer.Deserialize<VersionIndex>(
                await File.ReadAllBytesAsync(IndexCacheFilePath),
                ConsoleCommandUtils.JsonOptions);

            if (result is not null) {
                return result;
            }
        }

        VersionIndex? index = await DownloadIndex();

        if (index is not null) {
            await File.WriteAllBytesAsync(IndexCacheFilePath, JsonSerializer.SerializeToUtf8Bytes(index, ConsoleCommandUtils.JsonOptions));
        }

        return index;
    }

    /// <summary>
    /// Always downloads the index.
    /// </summary>
    public static async Task<VersionIndex?> DownloadIndex() {
        foreach (string url in Settings.Instance.InstallUrls) {
            if (await ConsoleCommandUtils.TryDownloadFile(new Uri($"{url}index.json")) is not { } indexFile) {
                continue;
            }

            VersionIndex? index = JsonSerializer.Deserialize<VersionIndex>(
                await File.ReadAllBytesAsync(indexFile),
                ConsoleCommandUtils.JsonOptions);

            if (index is null) {
                continue;
            }

            await File.WriteAllBytesAsync(
                IndexCacheFilePath,
                JsonSerializer.SerializeToUtf8Bytes(index, ConsoleCommandUtils.JsonOptions));

            return index;
        }

        ConsoleCommandUtils.WriteLineError("Could not download version index.\n");
        return null;
    }

    public static async Task<VersionDetails?> DownloadDetails(string version) {
        foreach (string url in Settings.Instance.InstallUrls) {
            string? detailsFile = await ConsoleCommandUtils.TryDownloadFile(new Uri($"{url}details/{version}.json"));

            if (detailsFile is null) {
                continue;
            }

            VersionDetails? details = JsonSerializer.Deserialize<VersionDetails>(
                await File.ReadAllBytesAsync(detailsFile),
                ConsoleCommandUtils.JsonOptions);

            if (details is null) {
                continue;
            }

            return details;
        }

        ConsoleCommandUtils.WriteLineError($"Could not download version details for `{version}`.\n");
        return null;
    }

    public static bool IsInstalled(string version, Platform platform) {
        string root = ConsoleCommandUtils.MakeRootedWithExeAsBase(Settings.Instance.InstallDirectory);
        return Directory.Exists(Path.Combine(root, platform.ToString(), version));
    }

    public static bool Uninstall(Settings settings, string version, Platform platform) {
        string root = ConsoleCommandUtils.MakeRootedWithExeAsBase(settings.InstallDirectory);
        string path = Path.Combine(root, platform.ToString(), version);

        if (!Directory.Exists(path)) {
            ConsoleCommandUtils.WriteLineError($"Version `{version}` ({platform}) is not installed.");
            return false;
        }

        Console.WriteLine($"Uninstalling version `{version}` ({platform})...");

        try {
            Directory.Delete(path, true);
        } catch (Exception e) {
            ConsoleCommandUtils.WriteLineError($"Could not uninstall version `{version}` ({platform}): {e.Message}");
            return false;
        }

        Console.WriteLine($"Uninstalled version `{version}` ({platform}).");
        return true;
    }

    public static async Task<string?> LatestInstalled(Platform platform) {
        VersionIndex? index = await GetIndex();

        if (index is null) {
            return null;
        }

        string root = ConsoleCommandUtils.MakeRootedWithExeAsBase(Settings.Instance.InstallDirectory);
        string[] versions = Directory.GetDirectories(Path.Combine(root, platform.ToString()))
            .Select(Path.GetFileName)
            .Cast<string>()
            .ToArray();

        string? result = index.Versions.FirstOrDefault(x => !x.PreRelease && versions.Contains(x.Version))?.Version;

        string? s = result;
        if (s != null) {
            Console.WriteLine("1");
            return s;
        }

        return index.Versions.FirstOrDefault(x => versions.Contains(x.Version))?.Version;
    }

    public static async Task<string?> LatestAvailable() {
        VersionIndex? index = await GetIndex();

        return index?.Versions.FirstOrDefault(x => !x.PreRelease)?.Version;
    }

    public static bool CheckCacheShouldUpdate() {
        if (!Settings.Instance.AutoDownloadIndex) {
            return false;
        }

        if (!File.Exists(IndexCacheFilePath)) {
            return true;
        }

        return
            DateTime.UtcNow - File.GetLastWriteTimeUtc(IndexCacheFilePath) >
            Settings.Instance.AutoDownloadIndexInterval;
    }

}
