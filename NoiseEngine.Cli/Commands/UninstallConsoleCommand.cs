using System;
using NoiseEngine.Cli.Options;
using NoiseEngine.Cli.Versions;

namespace NoiseEngine.Cli.Commands;

public class UninstallConsoleCommand : IConsoleCommand {

    private readonly Settings settings;

    private static CommandOption PlatformOption { get; } = new CommandOption(
        new string[] { "--platform", "-p" },
        "PLATFORM",
        "The platform to uninstall the version from. " +
        "Not providing this option will uninstall the version from default platform.");

    public string Name => "uninstall";
    public string[] Aliases => Array.Empty<string>();
    public string Description => "Uninstalls NoiseEngine versions.";
    public string Usage => $"{ConsoleCommandUtils.ExeName} {Name} <VERSION> [OPTIONS]";

    public CommandOption[] Options { get; } = {
        PlatformOption
    };

    public string LongDescription => $"Use `{ConsoleCommandUtils.ExeName} versions list` to list installed versions.";

    public UninstallConsoleCommand() {
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

        return VersionUtils.Uninstall(settings, args[0], platform.Value);
    }

}
