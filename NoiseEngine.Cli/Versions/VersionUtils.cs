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

    public static string? LatestInstalled(Platform platform) {
        string root = ConsoleCommandUtils.MakeRootedWithExeAsBase(Settings.Instance.InstallDirectory);
        string[] versions = Directory.GetDirectories(Path.Combine(root, platform.ToString())).Order().ToArray();

        return versions.Length == 0 ? null : Path.GetFileName(versions[^1]);
    }

}
