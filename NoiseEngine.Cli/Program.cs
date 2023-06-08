using System;
using System.Collections.Generic;
using System.Linq;
using NoiseEngine.Cli;

string exeName = ConsoleCommandUtils.ExeName;

List<IConsoleCommand> commands = new List<IConsoleCommand>();

// Help being in the collection passed as an argument to the constructor is intentional.
commands.Add(new HelpConsoleCommand(commands));

if (args.Length == 0) {
    ConsoleCommandUtils.WriteLineError("No command specified.");
    Console.WriteLine();
    Console.WriteLine($"Usage: `{exeName} <command>`");
    Console.WriteLine($"Use `{exeName} help` for a list of commands.");
    return;
}

string commandName = args[0];

if (commandName is "--help" or "-help" or "?")
    commandName = "help";

IConsoleCommand? command = commands.FirstOrDefault(c => c.Name == commandName || c.Aliases.Contains(commandName));

if (command == null) {
    ConsoleCommandUtils.WriteLineError($"Command `{commandName}` not found.");
    Console.WriteLine($"Use `{exeName} help` for a list of commands.");
    return;
}

command.Execute(args.AsSpan()[1..]);
