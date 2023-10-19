using System;
using NoiseEngine.Cli.Options;
using NoiseEngine.Cli.Versions;

namespace NoiseEngine.Cli.Commands;

public class UninstallConsoleCommand : IConsoleCommand {

    private readonly Settings settings;

    public string Name => "uninstall";
    public string[] Aliases => Array.Empty<string>();
    public string Description => "Uninstalls NoiseEngine versions.";
    public string Usage => $"{ConsoleCommandUtils.ExeName} {Name} <VERSION>";

    public CommandOption[] Options { get; } = Array.Empty<CommandOption>();

    public string LongDescription => $"Use `{ConsoleCommandUtils.ExeName} versions list` to list installed versions.";

    public UninstallConsoleCommand() {
        settings = Settings.Instance;
    }

    public bool Execute(ReadOnlySpan<string> args) {
        switch (args.Length) {
            case < 1:
                ConsoleCommandUtils.WriteInvalidUsage("Missing version argument.", Usage);
                return false;
            case > 2:
                ConsoleCommandUtils.WriteInvalidUsage("Too many arguments.", Usage);
                return false;
            default:
                return VersionUtils.Uninstall(settings, args[0]);
        }
    }

}
