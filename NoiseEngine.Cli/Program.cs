using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using NoiseEngine.Cli;
using NoiseEngine.Cli.Commands;
using NoiseEngine.Cli.Versions;

string exeName = ConsoleCommandUtils.ExeName;

Settings.Instance = GetSettings();

List<IConsoleCommand> commands = new List<IConsoleCommand> {
    new InstallConsoleCommand(),
    new UninstallConsoleCommand(),
    new PlatformsConsoleCommand(),
    new VersionsConsoleCommand(),
    new NewConsoleCommand()
};

// Help being in the collection passed as an argument to the constructor is intentional.
commands.Add(new HelpConsoleCommand(commands));

if (args.Length == 0) {
    ConsoleCommandUtils.WriteLineError("No command specified.");
    Console.WriteLine();
    Console.WriteLine($"Usage: `{exeName} <COMMAND>`");
    Console.WriteLine($"Use `{exeName} help` for a list of commands.");
    CheckCache();
    return -1;
}

string commandName = args[0];

IConsoleCommand? command = commands.FirstOrDefault(c => c.Name == commandName || c.Aliases.Contains(commandName));

if (command == null) {
    ConsoleCommandUtils.WriteLineError($"Command `{commandName}` not found.");
    Console.WriteLine($"Use `{exeName} help` for a list of commands.");
    CheckCache();
    return -1;
}

if (!command.Execute(args.AsSpan()[1..])) {
    CheckCache();
    return -1;
}

CheckCache();

return 0;

Settings GetSettings() {
    string settingsPath = ConsoleCommandUtils.MakeRootedWithExeAsBase("settings.json");
    if (!File.Exists(settingsPath)) {
        ConsoleCommandUtils.WriteLineWarning("Settings file not found; using default settings.");
        File.WriteAllBytes(
            settingsPath,
            JsonSerializer.SerializeToUtf8Bytes(new Settings(), ConsoleCommandUtils.JsonOptions));
    }

    Settings? result = JsonSerializer.Deserialize<Settings>(
        File.ReadAllText(settingsPath),
        ConsoleCommandUtils.JsonOptions);
    return result ?? throw new Exception("Could not deserialize settings.");
}

void CheckCache() {
    if (VersionUtils.CheckCacheShouldUpdate()) {
        new Process {
            StartInfo = new ProcessStartInfo(
                ConsoleCommandUtils.MakeRootedWithExeAsBase(ConsoleCommandUtils.ExeName),
                "versions available") {
                RedirectStandardOutput = true
            }
        }.Start();
    }
}
