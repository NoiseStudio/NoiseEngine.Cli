using System;
using System.IO;
using System.Linq;
using System.Text;
using NoiseEngine.Cli.Versions;

namespace NoiseEngine.Cli.Commands;

public class NewConsoleCommand : IConsoleCommand {

    public string Name => "new";
    public string[] Aliases => Array.Empty<string>();

    public string Description => "Creates a new NoiseEngine project.";

    public string Usage =>
        $"{ConsoleCommandUtils.ExeName} {Name} <TEMPLATE|list> [OPTIONS]";

    public ConsoleCommandOption[] Options => new[] {
        new ConsoleCommandOption(
            new[] { "--name <NAME>", "-n <NAME>" },
            "The name of the project. Defaults to the current directory name if it's empty, makes a new one if specified."),
        new ConsoleCommandOption(
            new[] { "--platform <PLATFORM>", "-p <PLATFORM>" },
            "The platform to target. Defaults to the current platform."),
        new ConsoleCommandOption(
            new [] { "--version <VERSION>", "-v <VERSION>" },
    "The version of NoiseEngine to use. Defaults to the latest version.")
    };

    public string LongDescription => $"Use `{ConsoleCommandUtils.ExeName} new list` for list of templates.";

    private static bool ListTemplates(Platform platform, string version) {
        string root = ConsoleCommandUtils.MakeRootedWithExeAsBase(Settings.Instance.InstallDirectory);
        root = Path.Combine(root, platform.ToString(), version, "templates");

        if (!Directory.Exists(root)) {
            ConsoleCommandUtils.WriteLineError($"No templates found for {platform} {version}.");
            return false;
        }

        string[] templates = Directory.GetDirectories(root);

        Console.WriteLine($"Templates for {platform} {version}:");

        foreach (string template in templates) {
            string descriptionPath = Path.Combine(template, "description.txt");

            if (File.Exists(descriptionPath)) {
                Console.WriteLine(
                    $"{Path.GetFileName(template)} - {File.ReadAllText(descriptionPath).Trim()}");
            } else {
                Console.WriteLine(
                    $"{Path.GetFileName(template)} - No description provided.");
            }
        }

        return true;
    }

    private static bool CreateProject(string template, string? name, Platform platform, string version) {
        string root = ConsoleCommandUtils.MakeRootedWithExeAsBase(Settings.Instance.InstallDirectory);
        root = Path.Combine(root, platform.ToString(), version, "templates", template);

        if (!VersionUtils.IsInstalled(version, platform)) {
            ConsoleCommandUtils.WriteLineError($"Version {version} is not installed.");
            return false;
        }

        if (!Directory.Exists(root)) {
            ConsoleCommandUtils.WriteLineError($"No template found for {platform} {version} named {template}.");
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

        foreach (string file in files) {
            string relativePath = Path.GetRelativePath(root, file);

            if (relativePath == "description.txt") {
                continue;
            }

            string destination = Path.Combine(fullPath, relativePath);
            string directory = Path.GetDirectoryName(destination)!;

            Directory.CreateDirectory(directory);

            byte[] data = File.ReadAllBytes(file);

            string dataText = Encoding.UTF8.GetString(data);
            string replacedText = dataText.Replace("{{ProjectName}}", name);
            replacedText = replacedText.Replace("{{Version}}", version);

            if (dataText != replacedText) {
                data = Encoding.UTF8.GetBytes(replacedText);
            }

            File.WriteAllBytes(destination, data);
        }

        return true;
    }

    public bool Execute(ReadOnlySpan<string> args) {
        if (args.Length < 1) {
            ConsoleCommandUtils.WriteInvalidUsage("Missing template argument.", Usage);
            return false;
        }

        string template = args[0];

        Platform? platform = null;
        string? name = null;
        string? version = null;

        for (int i = 1; i < args.Length; i++) {
            string arg = args[i];

            if (arg is "--platform") {
                if (platform is not null) {
                    ConsoleCommandUtils.WriteInvalidUsage("Multiple --platform options.", Usage);
                    return false;
                }

                if (args.Length <= i + 1) {
                    ConsoleCommandUtils.WriteInvalidUsage("Trailing --platform option.", Usage);
                    return false;
                }

                string platformString = args[i + 1];

                if (!Enum.TryParse(platformString, out Platform platformNotNullable)) {
                    ConsoleCommandUtils.WriteInvalidUsage(
                        $"Invalid platform: `{platformString}. List with `{ConsoleCommandUtils.ExeName} platforms`.",
                        Usage);
                    return false;
                }

                platform = platformNotNullable;
                i++;
            } else if (arg is "--name" or "-n") {
                if (name is not null) {
                    ConsoleCommandUtils.WriteInvalidUsage($"Multiple {arg} options.", Usage);
                    return false;
                }

                if (args.Length <= i + 1) {
                    ConsoleCommandUtils.WriteInvalidUsage($"Trailing {arg} option.", Usage);
                    return false;
                }

                name = args[i + 1];
                i++;
            } else if (arg is "--version" or "-v") {
                if (version is not null) {
                    ConsoleCommandUtils.WriteInvalidUsage($"Multiple {arg} options.", Usage);
                    return false;
                }

                if (args.Length <= i + 1) {
                    ConsoleCommandUtils.WriteInvalidUsage($"Trailing {arg} option.", Usage);
                    return false;
                }

                version = args[i + 1];
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

        version ??= VersionUtils.LatestInstalled(platform.Value);

        if (template == "list") {
            return ListTemplates(platform.Value, version!);
        }


        return CreateProject(template, name, platform.Value, version!);
    }

}
