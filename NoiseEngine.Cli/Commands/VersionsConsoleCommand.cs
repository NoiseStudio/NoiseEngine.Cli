using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NoiseEngine.Cli.Options;
using NoiseEngine.Cli.Versions;

namespace NoiseEngine.Cli.Commands;

public class VersionsConsoleCommand : IConsoleCommand {

    private readonly Settings settings;

    public string Name => "versions";
    public string[] Aliases { get; } = { "ver" };
    public string Description => "Lists NoiseEngine versions.";

    public string Usage =>
        $"{ConsoleCommandUtils.ExeName} {Name} <list|available|l|a> | " +
        $"{ConsoleCommandUtils.ExeName} {Name} <details|d> <VERSION>";

    public CommandOption[] Options => Array.Empty<CommandOption>();

    public string LongDescription =>
        $"Use `{ConsoleCommandUtils.ExeName} versions list` to list installed versions.\n" +
        $"Use `{ConsoleCommandUtils.ExeName} versions available` to list available versions.\n" +
        $"Use `{ConsoleCommandUtils.ExeName} versions details <VERSION>` to list details about a version.";

    public VersionsConsoleCommand() {
        settings = Settings.Instance;
    }

    public bool Execute(ReadOnlySpan<string> args) {
        if (args.Length < 1) {
            ConsoleCommandUtils.WriteInvalidUsage("Missing subcommand.", Usage);
            return false;
        }

        string subcommand = args[0];

        if (subcommand is "list" or "l") {
            if (args.Length > 1) {
                ConsoleCommandUtils.WriteInvalidUsage("Too many arguments.", Usage);
                return false;
            }

            return ListVersions();
        }

        if (subcommand is "available" or "a") {
            if (args.Length > 1) {
                ConsoleCommandUtils.WriteInvalidUsage("Too many arguments.", Usage);
                return false;
            }

            return AvailableVersions().Result;
        }

        if (subcommand is "details" or "d") {
            if (args.Length < 2) {
                ConsoleCommandUtils.WriteInvalidUsage("Missing version argument.", Usage);
                return false;
            }

            if (args.Length > 2) {
                ConsoleCommandUtils.WriteInvalidUsage("Too many arguments.", Usage);
                return false;
            }

            return ListVersionDetails(args[1]).Result;
        }

        ConsoleCommandUtils.WriteInvalidUsage($"Unknown subcommand `{subcommand}`.", Usage);
        return false;
    }

    private bool ListVersions() {
        string root = ConsoleCommandUtils.MakeRootedWithExeAsBase(settings.InstallDirectory);

        foreach (object o in Enum.GetValuesAsUnderlyingType<Platform>()) {
            Platform platform = (Platform)o;
            string platformString = platform.ToString();
            string[] versions = Directory.GetDirectories(Path.Combine(root, platformString));

            if (versions.Length == 0) {
                continue;
            }

            Console.WriteLine($"{platformString}:");

            foreach (string version in versions) {
                Console.WriteLine(ConsoleCommandUtils.Indent(Path.GetFileName(version)));
            }

            Console.WriteLine();
        }

        return true;
    }

    private async Task<bool> AvailableVersions() {
        VersionIndex? index = await VersionUtils.DownloadIndex();

        if (index is null) {
            return false;
        }

        Console.WriteLine("Available versions:");
        bool latest = true;
        bool latestPreRelease = true;

        foreach (VersionInfo version in index.Versions) {
            IEnumerable<Platform> installed = Enum.GetValuesAsUnderlyingType<Platform>()
                .Cast<Platform>()
                .Where(platform => VersionUtils.IsInstalled(version.Version, platform));

            string installedString = string.Join(", ", installed.Select(x => x.ToString()));

            if (installedString.Length > 0) {
                installedString = $" (installed for {installedString})";
            }

            string releaseString;

            if (version.PreRelease) {
                if (latestPreRelease) {
                    releaseString = " (latest pre-release)";
                } else {
                    releaseString = " (pre-release)";
                }
            } else {
                if (latest) {
                    releaseString = " (latest)";
                } else {
                    releaseString = "";
                }
            }

            Console.WriteLine(ConsoleCommandUtils.Indent(
                version.Version + releaseString + installedString));

            if (!version.PreRelease) {
                latest = false;
            }

            latestPreRelease = false;
        }

        return true;
    }

    private async Task<bool> ListVersionDetails(string version) {
        VersionIndex? index = await VersionUtils.GetIndex();

        if (index is null) {
            return false;
        }

        VersionInfo? info = index.Versions.FirstOrDefault(v => v.Version == version);

        if (info is null) {
            ConsoleCommandUtils.WriteLineError(
                $"Version `{version}` not found. Try running `{ConsoleCommandUtils.ExeName} versions available` " +
                $"to refresh index and see available versions.");
            return false;
        }

        VersionDetails? details = await VersionUtils.DownloadDetails(version);

        if (details is null) {
            return false;
        }

        Console.WriteLine($"Version: {details.Version}");
        Console.WriteLine($"Pre-release: {details.PreRelease}");
        return true;
    }

}
