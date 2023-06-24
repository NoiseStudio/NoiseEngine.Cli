using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using NoiseEngine.Cli.Versions;

namespace NoiseEngine.Cli.Commands;

public class InstallConsoleCommand : IConsoleCommand {

    private readonly Settings settings;

    public string Name => "install";
    public string[] Aliases => Array.Empty<string>();
    public string Description => "Installs NoiseEngine versions.";
    public string Usage => $"{ConsoleCommandUtils.ExeName} {Name} <VERSION> [OPTIONS]";

    public ConsoleCommandOption[] Options { get; } = {
        new ConsoleCommandOption(
            new string[] { "--force", "-f" },
            "Forces the installation of the specified version, even if it is already installed."),
        new ConsoleCommandOption(
            new string[] { "--platform <PLATFORM>" },
            $"Forces installer to download engine for specified platform. List with `{ConsoleCommandUtils.ExeName} platforms`.")
    };

    public string LongDescription =>
        $"Use `{ConsoleCommandUtils.ExeName} versions list` to list installed versions " +
        $"and `{ConsoleCommandUtils.ExeName} versions available` to list available versions.";

    public InstallConsoleCommand(Settings settings) {
        this.settings = settings;
    }

    public bool Execute(ReadOnlySpan<string> args) {
        if (args.Length < 1) {
            ConsoleCommandUtils.WriteInvalidUsage("Missing version argument.", Usage);
            return false;
        }

        bool force = false;
        Platform? platform = null;

        for (int i = 1; i < args.Length; i++) {
            string arg = args[i];

            if (arg is "--force" or "-f") {
                if (force) {
                    ConsoleCommandUtils.WriteInvalidUsage("Multiple --force options.", Usage);
                    return false;
                }

                force = true;
            } else if (arg == "--platform") {
                if (platform is not null) {
                    ConsoleCommandUtils.WriteInvalidUsage("Multiple --platform options.", Usage);
                    return false;
                }

                if (args.Length <= i + 1) {
                    ConsoleCommandUtils.WriteInvalidUsage("Trailing --platform option.", Usage);
                    return false;
                }

                string platformString = args[i + 1];

                if (!Enum.TryParse(platformString, out Platform platformNotNullable)) {
                    ConsoleCommandUtils.WriteInvalidUsage(
                        $"Invalid platform: `{platformString}. List with `{ConsoleCommandUtils.ExeName} platforms`.",
                        Usage);
                    return false;
                }

                platform = platformNotNullable;
            }
        }

        if (platform is null) {
            if (OperatingSystem.IsWindows()) {
                platform = Platform.WindowsAmd64;
            } else if (OperatingSystem.IsLinux()) {
                platform = Platform.LinuxAmd64;
            } else {
                ConsoleCommandUtils.WriteLineError("Could not determine OS. Try using `--platform` option.");
                return false;
            }
        }

        return InstallVersion(args[0], force, platform.Value).Result;
    }


    private async Task<bool> InstallVersion(string version, bool force, Platform platform) {
        VersionIndex? index = await VersionUtils.DownloadIndex(settings);

        if (index is null) {
            return false;
        }

        VersionInfo? vi = index.Versions.FirstOrDefault(x => x.Version == version);

        if (vi is null) {
            ConsoleCommandUtils.WriteLineError($"Version `{version}` not found.");
            return false;
        }

        if (VersionUtils.IsInstalled(settings, vi.Version, platform)) {
            if (force) {
                VersionUtils.Uninstall(settings, vi.Version, platform);
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

        string? shared = await TryDownloadMultiple(
            details.SharedUrls.Select(x => new Uri(x)),
            Convert.FromHexString(details.SharedSha256));


        if (shared is null) {
            ConsoleCommandUtils.WriteLineError($"Failed to download version `{vi.Version}`.");
            return false;
        }

        IEnumerable<Uri> extensionUris = platform switch {
            Platform.WindowsAmd64 => details.ExtensionWindowsAmd64Urls.Select(x => new Uri(x)),
            Platform.LinuxAmd64 => details.ExtensionLinuxAmd64Urls.Select(x => new Uri(x)),
            _ => throw new ArgumentOutOfRangeException(nameof(platform), platform, null)
        };

        string extensionSha256String = platform switch {
            Platform.WindowsAmd64 => details.ExtensionWindowsAmd64Sha256,
            Platform.LinuxAmd64 => details.ExtensionLinuxAmd64Sha256,
            _ => throw new ArgumentOutOfRangeException(nameof(platform), platform, null)
        };

        string? extension = await TryDownloadMultiple(
            extensionUris,
            Convert.FromHexString(extensionSha256String));

        if (extension is null) {
            ConsoleCommandUtils.WriteLineError($"Failed to download version `{vi.Version}`.");
            return false;
        }

        string root = ConsoleCommandUtils.MakeRootedWithExeAsBase(settings.InstallDirectory);
        string installDir = Path.Combine(root, platform.ToString(), version);

        Directory.CreateDirectory(installDir);

        ZipFile.ExtractToDirectory(shared, installDir);
        ZipFile.ExtractToDirectory(extension, installDir);

        File.Delete(shared);
        File.Delete(extension);

        Console.WriteLine($"Installed version `{vi.Version}`.");
        return true;
    }

    private static async Task<string?> TryDownloadMultiple(IEnumerable<Uri> uris, byte[] sha256) {
        string? result = null;

        foreach (Uri uri in uris) {
            result = await ConsoleCommandUtils.TryDownloadFile(uri);

            if (result is null) {
                continue;
            }

            await using FileStream fs = File.OpenRead(result);
            byte[] hash = await SHA256.HashDataAsync(fs);

            if (!sha256.SequenceEqual(hash)) {
                ConsoleCommandUtils.WriteLineWarning(
                    $"Hash mismatch in file from mirror `{uri}`.\n" +
                    $"Expected: {Convert.ToHexString(sha256)}\n" +
                    $"Actual:   {Convert.ToHexString(hash)}");
                result = null;
                continue;
            }

            break;
        }

        return result;
    }

}
