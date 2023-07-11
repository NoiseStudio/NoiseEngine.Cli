using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using NoiseEngine.Cli.Options;
using NoiseEngine.Cli.Versions;

namespace NoiseEngine.Cli.Commands;

public class InstallConsoleCommand : IConsoleCommand {

    private readonly Settings settings;

    private static CommandOption ForceOption { get; } = new CommandOption(
        new string[] { "--force", "-f" },
        null,
        "Forces the installation of the specified version, even if it is already installed.");

    private static CommandOption PlatformOption { get; } = new CommandOption(
        new string[] { "--platform" },
        "PLATFORM",
        $"Forces installer to download engine for specified platform. List with `{ConsoleCommandUtils.ExeName} platforms`.");

    public string Name => "install";
    public string[] Aliases => Array.Empty<string>();
    public string Description => "Installs NoiseEngine versions.";
    public string Usage => $"{ConsoleCommandUtils.ExeName} {Name} <VERSION|latest|latest-pre> [OPTIONS]";

    public CommandOption[] Options { get; } = {
        ForceOption,
        PlatformOption
    };

    public string LongDescription =>
        "Version `latest` installs latest stable version and version `latest-pre` installs latest version even if it's not stable.\n" +
        $"Use `{ConsoleCommandUtils.ExeName} versions list` to list installed versions " +
        $"and `{ConsoleCommandUtils.ExeName} versions available` to list available versions.";

    public InstallConsoleCommand() {
        settings = Settings.Instance;
    }

    public bool Execute(ReadOnlySpan<string> args) {
        if (args.Length < 1) {
            ConsoleCommandUtils.WriteInvalidUsage("Missing version argument.", Usage);
            return false;
        }

        if (
            !OptionParsingUtils.TryGetPairs(
                args.Length > 1 ? args[1..] : ReadOnlySpan<string>.Empty,
                out CommandOptionValue[]? optionValues,
                Options)
        ) {
            return false;
        }

        Platform? platform = OptionParsingUtils.GetPlatformOrCurrent(optionValues, PlatformOption);

        if (platform is null) {
            return false;
        }

        bool force = OptionParsingUtils.GetFlag(optionValues, ForceOption);

        return InstallVersion(args[0], force, platform.Value).Result;
    }


    private async Task<bool> InstallVersion(string version, bool force, Platform platform) {
        VersionIndex? index = await VersionUtils.GetIndex();

        if (index is null) {
            ConsoleCommandUtils.WriteLineError("Could not download version index.");
            return false;
        }

        VersionInfo? vi;

        switch (version) {
            case "latest":
                vi = index.Versions.FirstOrDefault(x => !x.PreRelease);
                version = vi?.Version ?? version;
                break;
            case "latest-pre":
                vi = index.Versions.FirstOrDefault();
                version = vi?.Version ?? version;
                break;
            default:
                vi = index.Versions.FirstOrDefault(x => x.Version == version);
                break;
        }

        if (vi is null) {
            ConsoleCommandUtils.WriteLineError(
                $"Version `{version}` not found. Try running `{ConsoleCommandUtils.ExeName} versions available` " +
                $"to refresh index and see available versions.");
            return false;
        }

        if (VersionUtils.IsInstalled(vi.Version, platform)) {
            if (force) {
                VersionUtils.Uninstall(settings, vi.Version, platform);
            } else {
                ConsoleCommandUtils.WriteLineError($"Version `{version}` is already installed.");
                return false;
            }
        }

        VersionDetails? details = await VersionUtils.DownloadDetails(vi.Version);

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
