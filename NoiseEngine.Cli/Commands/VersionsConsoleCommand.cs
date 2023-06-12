using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

    public ConsoleCommandOption[] Options => Array.Empty<ConsoleCommandOption>();

    public string LongDescription =>
        $"Use `{ConsoleCommandUtils.ExeName} versions list` to list installed versions.\n" +
        $"Use `{ConsoleCommandUtils.ExeName} versions available` to list available versions.\n" +
        $"Use `{ConsoleCommandUtils.ExeName} versions details <VERSION>` to list details about a version.";

    public VersionsConsoleCommand(Settings settings) {
        this.settings = settings;
    }

    public bool Execute(ReadOnlySpan<string> args) {
        if (args.Length < 1) {
            ConsoleCommandUtils.WriteInvalidUsage("Missing subcommand.", Usage);
            return false;
        }

        string subcommand = args[0];

        if (subcommand is "list" or "l") {
            return ListVersions();
        }

        if (subcommand is "available" or "a") {
            return AvailableVersions().Result;
        }

        if (subcommand is "details" or "d") {
            if (args.Length < 2) {
                ConsoleCommandUtils.WriteInvalidUsage("Missing version argument.", Usage);
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
        VersionIndex? index = await VersionUtils.DownloadIndex(settings);

        if (index is null) {
            ConsoleCommandUtils.WriteLineError("Failed to download version index.");
            return false;
        }

        Console.WriteLine("Available versions:");

        foreach (VersionInfo version in index.Versions) {
            string installed = string.Empty;

            foreach (object o in Enum.GetValuesAsUnderlyingType<Platform>()) {
                Platform platform = (Platform)o;

                if (VersionUtils.IsInstalled(settings, version.Version, platform)) {
                    installed += $"{platform} ";
                }
            }

            if (installed.Length > 0) {
                installed = $" (installed for {installed.Trim().Replace(" ", ", ")})";
            }

            Console.WriteLine(ConsoleCommandUtils.Indent(
                version.Version + (version.PreRelease ? $" (pre-release){installed}" : $"{installed}")));
        }

        return true;
    }

    private async Task<bool> ListVersionDetails(string version) {
        VersionIndex? index = await VersionUtils.DownloadIndex(settings);

        if (index is null) {
            ConsoleCommandUtils.WriteLineError("Failed to download version index.");
            return false;
        }

        VersionInfo? info = index.Versions.FirstOrDefault(v => v.Version == version);

        if (info is null) {
            ConsoleCommandUtils.WriteLineError($"Version `{version}` not found.");
            return false;
        }

        VersionDetails? details = await VersionUtils.DownloadDetails(settings, version);

        if (details is null) {
            ConsoleCommandUtils.WriteLineError($"Failed to download version details for `{version}`.");
            return false;
        }

        Console.WriteLine($"Version: {details.Version}");
        Console.WriteLine($"Pre-release: {details.PreRelease}");
        return true;
    }

}
