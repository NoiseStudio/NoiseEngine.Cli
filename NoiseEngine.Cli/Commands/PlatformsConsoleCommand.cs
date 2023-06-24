using System;

namespace NoiseEngine.Cli.Commands;

public class PlatformsConsoleCommand : IConsoleCommand {

    public string Name => "platforms";
    public string[] Aliases => Array.Empty<string>();
    public string Description => "Lists available platforms.";
    public string Usage => $"{ConsoleCommandUtils.ExeName} {Name}";
    public ConsoleCommandOption[] Options => Array.Empty<ConsoleCommandOption>();
    public string? LongDescription => null;

    public bool Execute(ReadOnlySpan<string> args) {
        if (args.Length > 0) {
            ConsoleCommandUtils.WriteLineError("Too many arguments.");
            return false;
        }

        Console.WriteLine("Available platforms:");

        foreach (Platform platform in Enum.GetValues<Platform>()) {
            Console.WriteLine(ConsoleCommandUtils.Indent(platform.ToString()));
        }

        return true;
    }

}
