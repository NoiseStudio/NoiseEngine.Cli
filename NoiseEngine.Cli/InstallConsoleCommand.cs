using System;
using System.IO;
using System.Linq;

namespace NoiseEngine.Cli;

public class InstallConsoleCommand : IConsoleCommand {

    private readonly Settings settings;

    public string Name => "install";
    public string[] Aliases { get; } = Array.Empty<string>();
    public string Description => "Installing and displaying information about NoiseEngine versions.";

    public string Usage =>
        $"{ConsoleCommandUtils.ExeName} install <VERSION> [options] | " +
        $"{ConsoleCommandUtils.ExeName} install <list|available|l|a>";

    public ConsoleCommandOption[] Options { get; } = {
        new ConsoleCommandOption(
            new[] { "--force", "-f" },
            "Forces the installation of the specified version, even if it is already installed."),
    };

    public string LongDescription =>
        $"Use `{ConsoleCommandUtils.ExeName} install <VERSION>` to install a version of NoiseEngine.\n" +
        $"Use `{ConsoleCommandUtils.ExeName} install <list|l>` to list installed versions.\n" +
        $"Use `{ConsoleCommandUtils.ExeName} install <available|a>` to list available versions.\n";

    public InstallConsoleCommand(Settings settings) {
        this.settings = settings;
    }

    public void Execute(ReadOnlySpan<string> args) {
        if (args.Length is 0 or > 2) {
            ConsoleCommandUtils.WriteLineError("Invalid usage.");
            Console.WriteLine();
            Console.WriteLine($"Usage: `{Usage}`");
            return;
        }

        string version = args[0];

        switch (version) {
            case "list":
            case "l":
                if (args.Length > 1) {
                    ConsoleCommandUtils.WriteLineError("Too many arguments.");
                    Console.WriteLine();
                    Console.WriteLine($"Usage: `{Usage}`");
                    return;
                }

                ListInstalledVersions();
                return;
            case "available":
            case "a":
                if (args.Length > 1) {
                    ConsoleCommandUtils.WriteLineError("Too many arguments.");
                    Console.WriteLine();
                    Console.WriteLine($"Usage: `{Usage}`");
                    return;
                }

                ListAvailableVersions();
                return;
            default:
                InstallVersion(version, args.Contains("--force") || args.Contains("-f"));
                break;
        }
    }

    private void InstallVersion(string version, bool force) {
        throw new NotImplementedException();
    }

    private void ListAvailableVersions() {
        throw new NotImplementedException();
    }

    private void ListInstalledVersions() {
        string[] installedVersions = GetInstalledVersions();

        if (installedVersions.Length == 0) {
            Console.WriteLine("No versions installed.");
            return;
        }

        Console.WriteLine("Installed versions:");
        Console.WriteLine(ConsoleCommandUtils.Indent(string.Join('\n', GetInstalledVersions())));
    }

    private string[] GetInstalledVersions() {
        string path = settings.InstallDirectory;

        if (!Path.IsPathRooted(path)) {
            path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
        }

        if (!Directory.Exists(path))
            return Array.Empty<string>();

        return Directory.GetDirectories(path)
            .Select(Path.GetFileName)
            .ToArray()!;
    }

}
