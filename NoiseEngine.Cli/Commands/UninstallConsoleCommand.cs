using System;
using NoiseEngine.Cli.Versions;

namespace NoiseEngine.Cli.Commands;

public class UninstallConsoleCommand : IConsoleCommand {

    private readonly Settings settings;

    public string Name => "uninstall";
    public string[] Aliases => Array.Empty<string>();
    public string Description => "Uninstalls NoiseEngine versions.";
    public string Usage => $"{ConsoleCommandUtils.ExeName} {Name} <VERSION> [OPTIONS]";

    public ConsoleCommandOption[] Options { get; } = {
        new ConsoleCommandOption(
            new[] { "--platform <PLATFORM>" },
            "The platform to uninstall the version from. " +
            "Not providing this option will uninstall the version from default platform.")
    };

    public string LongDescription => $"Use `{ConsoleCommandUtils.ExeName} versions list` to list installed versions.";

    public UninstallConsoleCommand(Settings settings) {
        this.settings = settings;
    }

    public bool Execute(ReadOnlySpan<string> args) {
        if (args.Length < 1) {
            ConsoleCommandUtils.WriteInvalidUsage("Missing version argument.", Usage);
            return false;
        }

        Platform? platform = null;

        for (int i = 1; i < args.Length; i++) {
            string arg = args[i];

            if (arg == "--platform") {
                if (platform is not null) {
                    ConsoleCommandUtils.WriteInvalidUsage("Multiple --platform options.", Usage);
                    return false;
                }

                if (args.Length <= i + 1) {
                    ConsoleCommandUtils.WriteInvalidUsage("Trailing --platform option.", Usage);
                    return false;
                }

                if (!Enum.TryParse(args[i + 1], true, out Platform parsedPlatform)) {
                    ConsoleCommandUtils.WriteInvalidUsage($"Unknown platform `{args[i + 1]}`.", Usage);
                    return false;
                }

                platform = parsedPlatform;
                i++;
            } else {
                ConsoleCommandUtils.WriteInvalidUsage($"Unknown option `{arg}`.", Usage);
                return false;
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

        return VersionUtils.Uninstall(settings, args[0], platform.Value);
    }

}
