using System;
using System.Collections.Generic;
using System.Linq;

namespace NoiseEngine.Cli.Commands;

public class HelpConsoleCommand : IConsoleCommand {

    private readonly IEnumerable<IConsoleCommand> consoleCommands;

    public HelpConsoleCommand(IEnumerable<IConsoleCommand> consoleCommands) {
        this.consoleCommands = consoleCommands;
    }

    public string Name => "help";
    public string[] Aliases { get; } = Array.Empty<string>();
    public string Description => "Displays help information about the console commands.";
    public string Usage => $"{ConsoleCommandUtils.ExeName} {Name} [COMMAND]";
    public ConsoleCommandOption[] Options => Array.Empty<ConsoleCommandOption>();
    public string? LongDescription => null;

    public bool Execute(ReadOnlySpan<string> args) {
        switch (args.Length) {
            case > 1:
                ConsoleCommandUtils.WriteLineError("Too many arguments.");
                Console.WriteLine();
                Console.WriteLine($"Usage: `{Usage}`");
                return false;
            case 1:
                string commandName = args[0];
                IConsoleCommand? command =
                    consoleCommands.FirstOrDefault(c => c.Name == commandName || c.Aliases.Contains(commandName));

                if (command == null) {
                    ConsoleCommandUtils.WriteLineError($"Command `{commandName}` not found.");
                    return false;
                }

                Console.WriteLine($"{command.NameAliasList} - {command.Description}");
                Console.WriteLine();
                Console.WriteLine($"Usage: `{command.Usage}`");

                if (command.LongDescription is not null) {
                    Console.WriteLine();
                    Console.WriteLine("Description:");
                    Console.WriteLine(ConsoleCommandUtils.Indent(command.LongDescription));
                }

                if (command.Options.Length <= 0)
                    return true;

                Console.WriteLine();
                Console.WriteLine("Options:");

                (string, string)[] optionDescriptionPairs = command.Options
                    .Select(o => (string.Join(", ", o.Names), " - " + o.Description))
                    .ToArray();

                Console.WriteLine(ConsoleCommandUtils.Indent(ConsoleCommandUtils.Align(optionDescriptionPairs)));
                return true;
        }

        Console.WriteLine("List of commands:");

        (string, string)[] aliasDescriptionPairs = consoleCommands
            .OrderBy(c => c.Name)
            .Select(
                command => (command.NameAliasList, " - " + command.Description))
            .ToArray();

        string list = ConsoleCommandUtils.Align(aliasDescriptionPairs);
        Console.WriteLine(ConsoleCommandUtils.Indent(list));
        return true;
    }

}