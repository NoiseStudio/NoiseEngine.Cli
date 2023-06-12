using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using NoiseEngine.Cli;

string exeName = ConsoleCommandUtils.ExeName;

Settings settings = GetSettings();

List<IConsoleCommand> commands = new List<IConsoleCommand> {
    new InstallConsoleCommand(settings)
};

// Help being in the collection passed as an argument to the constructor is intentional.
commands.Add(new HelpConsoleCommand(commands));

if (args.Length == 0) {
    ConsoleCommandUtils.WriteLineError("No command specified.");
    Console.WriteLine();
    Console.WriteLine($"Usage: `{exeName} <COMMAND>`");
    Console.WriteLine($"Use `{exeName} help` for a list of commands.");
    return -1;
}

string commandName = args[0];

IConsoleCommand? command = commands.FirstOrDefault(c => c.Name == commandName || c.Aliases.Contains(commandName));

if (command == null) {
    ConsoleCommandUtils.WriteLineError($"Command `{commandName}` not found.");
    Console.WriteLine($"Use `{exeName} help` for a list of commands.");
    return -1;
}

if (!command.Execute(args.AsSpan()[1..])) {
    // return with exit code
    return -1;
}

return 0;

Settings GetSettings() {
    if (!File.Exists("./settings.json")) {
        ConsoleCommandUtils.WriteLineWarning("Settings file not found; using default settings.");
        JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllBytes("./settings.json", JsonSerializer.SerializeToUtf8Bytes(new Settings(), options));
    }

    Settings? result = JsonSerializer.Deserialize<Settings>(
        File.ReadAllBytes("./settings.json"),
        ConsoleCommandUtils.JsonOptions);
    return result ?? throw new Exception("Could not deserialize settings.");
}
