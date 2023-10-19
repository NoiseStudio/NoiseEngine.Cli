using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NoiseEngine.Cli.Options;
using NoiseEngine.Cli.Versions;

namespace NoiseEngine.Cli.Commands;

public class NewConsoleCommand : IConsoleCommand {

    private static CommandOption NameOption { get; } = new CommandOption(
        new[] { "--name", "-n" },
        "NAME",
        "The name of the project. Defaults to the current directory name if it's empty, makes a new one if specified.");

    private static CommandOption VersionOption { get; } = new CommandOption(
        new[] { "--version", "-v" },
        "VERSION",
        "The version of NoiseEngine to use. Defaults to the latest version.");

    private static CommandOption NoUpdateOption { get; } = new CommandOption(
        new[] { "--noupdate" },
        null,
        "Disables checking for new NoiseEngine versions.");

    public string Name => "new";
    public string[] Aliases => Array.Empty<string>();

    public string Description => "Creates a new NoiseEngine project.";

    public string Usage =>
        $"{ConsoleCommandUtils.ExeName} {Name} <TEMPLATE|list> [OPTIONS]";

    public CommandOption[] Options => new[] {
        NameOption,
        VersionOption,
        NoUpdateOption
    };

    public string LongDescription => $"Use `{ConsoleCommandUtils.ExeName} new list` for list of templates.";

    private static bool ListTemplates(string version) {
        string root = ConsoleCommandUtils.MakeRootedWithExeAsBase(Settings.Instance.InstallDirectory);
        root = Path.Combine(root, version, "shared", "templates");

        if (!Directory.Exists(root)) {
            ConsoleCommandUtils.WriteLineError($"No templates found for {version}.");
            return false;
        }

        string[] templates = Directory.GetDirectories(root);

        Console.WriteLine($"Templates for {version}:");

        List<(string lhs, string rhs)> pairs = new List<(string lhs, string rhs)>();

        foreach (string template in templates) {
            string descriptionPath = Path.Combine(template, "description.txt");

            if (File.Exists(descriptionPath)) {
                pairs.Add((Path.GetFileName(template), $" - {File.ReadAllText(descriptionPath).Trim()}"));
            } else {
                pairs.Add((Path.GetFileName(template), " - No description provided."));
            }
        }

        Console.WriteLine(ConsoleCommandUtils.Align(pairs.ToArray()));

        return true;
    }

    private static bool CreateProject(string template, string? name, string version) {
        string root = ConsoleCommandUtils.MakeRootedWithExeAsBase(Settings.Instance.InstallDirectory);
        root = Path.Combine(root, version, "shared", "templates", template);

        if (!VersionUtils.IsInstalledShared(version)) {
            ConsoleCommandUtils.WriteLineError($"Version {version} is not installed.");
            return false;
        }

        if (!Directory.Exists(root)) {
            ConsoleCommandUtils.WriteLineError($"No template found for {version} named {template}.");
            return false;
        }

        string fullPath;

        if (name is null) {
            fullPath = Directory.GetCurrentDirectory();
            name = Path.GetFileName(fullPath);
        } else {
            Directory.CreateDirectory(name);
            fullPath = Path.Combine(Directory.GetCurrentDirectory(), name);
        }

        if (Directory.EnumerateFileSystemEntries(fullPath).Any()) {
            ConsoleCommandUtils.WriteLineError($"Directory {fullPath} is not empty.");
            return false;
        }

        Console.WriteLine($"Creating project {name} from template {template}...");

        string[] files = Directory.GetFiles(root, "*", SearchOption.AllDirectories);

        Parallel.ForEach(files, file => {
            string relativePath = Path.GetRelativePath(root, file);

            if (relativePath == "description.txt") {
                return;
            }

            relativePath = relativePath
                .Replace("{{ProjectName}}", name)
                .Replace("{{Version}}", version);

            string destination = Path.Combine(fullPath, relativePath);
            string directory = Path.GetDirectoryName(destination)!;

            Directory.CreateDirectory(directory);

            byte[] data = File.ReadAllBytes(file);

            try {
                string dataText = Encoding.UTF8.GetString(data);
                string replacedText = dataText.Replace("{{ProjectName}}", name);
                replacedText = replacedText.Replace("{{Version}}", version);

                if (dataText != replacedText) {
                    data = Encoding.UTF8.GetBytes(replacedText);
                }
            } catch (Exception) {
                // Not a text file
            }

            File.WriteAllBytes(destination, data);
        });

        return true;
    }

    public bool Execute(ReadOnlySpan<string> args) {
        if (args.Length < 1) {
            ConsoleCommandUtils.WriteInvalidUsage("Missing template argument.", Usage);
            return false;
        }

        string template = args[0];

        if (
            !OptionParsingUtils.TryGetPairs(
                args.Length > 1 ? args[1..] : ReadOnlySpan<string>.Empty,
                out CommandOptionValue[]? optionValues,
                Options)
        ) {
            return false;
        }

        string? version = OptionParsingUtils.GetVersionOrLatest(optionValues, VersionOption);

        if (version is null) {
            return false;
        }

        string? name = OptionParsingUtils.GetValue(optionValues, NameOption);

        bool noUpdate = OptionParsingUtils.GetFlag(optionValues, NoUpdateOption);

        if (template == "list") {
            return ListTemplates(version);
        }

        if (VersionUtils.CheckCacheShouldUpdate()) {
            _ = VersionUtils.DownloadIndex().Result;
        }

        string? latestVersion = VersionUtils.LatestAvailable().Result;

        if (latestVersion is not null && version != latestVersion && !noUpdate) {
            ConsoleCommandUtils.WriteLineWarning(
                $"Version {version} is not the latest version. Latest version is {latestVersion}.");
            bool response = ConsoleCommandUtils.PromptYesNo("Do you want to update?");

            if (response) {
                version = latestVersion;

                if (!VersionUtils.IsInstalledShared(version) &&
                    !new InstallConsoleCommand().Execute(new string[] { latestVersion })) {
                    return false;
                }
            }
        }

        return CreateProject(template, name, version);
    }

}
