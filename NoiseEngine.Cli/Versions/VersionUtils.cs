﻿using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace NoiseEngine.Cli.Versions;

public static class VersionUtils {

    public static async Task<VersionIndex?> DownloadIndex(Settings settings) {
        foreach (string url in settings.InstallUrls) {
            if (await ConsoleCommandUtils.TryDownloadFile(new Uri($"{url}/index.json")) is not { } indexFile) {
                continue;
            }

            if (JsonSerializer.Deserialize<VersionIndex>(await File.ReadAllBytesAsync(indexFile)) is not { } index) {
                continue;
            }

            return index;
        }

        ConsoleCommandUtils.WriteLineError("Could not download version index.\n");
        return null;
    }

    public static async Task<VersionDetails?> DownloadDetails(Settings settings, string version) {
        foreach (string url in settings.InstallUrls) {
            string? detailsFile = await ConsoleCommandUtils.TryDownloadFile(new Uri($"{url}/details/{version}.json"));

            if (detailsFile is null) {
                continue;
            }

            VersionDetails? details =
                JsonSerializer.Deserialize<VersionDetails>(await File.ReadAllBytesAsync(detailsFile));

            if (details is null) {
                continue;
            }

            return details;
        }

        ConsoleCommandUtils.WriteLineError($"Could not download version details for `{version}`.\n");
        return null;
    }

    public static bool IsInstalled(Settings settings, string version) {
        return Directory.Exists(Path.Combine(settings.InstallDirectory, version));
    }

    public static bool Uninstall(Settings settings, string version) {
        string path = Path.Combine(settings.InstallDirectory, version);

        if (!Directory.Exists(path)) {
            ConsoleCommandUtils.WriteLineError($"Version `{version}` is not installed.");
            return false;
        }

        Console.WriteLine($"Uninstalling version `{version}`...");

        try {
            Directory.Delete(path, true);
        } catch (Exception e) {
            ConsoleCommandUtils.WriteLineError($"Could not uninstall version `{version}`: {e.Message}");
            return false;
        }

        Console.WriteLine($"Uninstalled version `{version}`.");
        return true;
    }

}