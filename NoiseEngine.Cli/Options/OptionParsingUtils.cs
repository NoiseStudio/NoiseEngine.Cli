using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NoiseEngine.Cli.Versions;

namespace NoiseEngine.Cli.Options;

public static class OptionParsingUtils {

    public static bool TryGetPairs(
        ReadOnlySpan<string> args,
        [NotNullWhen(true)] out CommandOptionValue[]? values,
        CommandOption[] allowedOptions
    ) {
        List<CommandOptionValue> result = new List<CommandOptionValue>();
        values = null;

        for (int i = 0; i < args.Length; i++) {
            if (!ValidateOption(args[i], out CommandOption? co)) {
                ConsoleCommandUtils.WriteLineError($"Invalid or duplicate option: {args[i]}");
                return false;
            }

            if (args.Length <= i + 1) {
                if (co.Trail is not null) {
                    ConsoleCommandUtils.WriteLineError($"Trailing option: {args[i]}");
                    return false;
                }

                result.Add(new CommandOptionValue(co, null));
                values = result.ToArray();
                return true;
            }

            if (co.Trail is not null) {
                result.Add(new CommandOptionValue(co, args[i + 1]));
                i++;
            } else {
                result.Add(new CommandOptionValue(co, null));
            }
        }

        values = result.ToArray();
        return true;

        bool ValidateOption(string option, [NotNullWhen(true)] out CommandOption? realOption) {
            foreach (CommandOption co in allowedOptions) {
                if (!co.Variants.Contains(option)) {
                    continue;
                }

                if (result.All(cov => co != cov.Option)) {
                    realOption = co;
                    return true;
                }

                ConsoleCommandUtils.WriteLineError($"Duplicate option: {option}");
                realOption = null;
                return false;
            }

            realOption = null;
            return false;
        }

    }

    public static bool GetFlag(IEnumerable<CommandOptionValue> values, CommandOption option) {
        foreach ((CommandOption co, string? _) in values) {
            if (co != option) {
                continue;
            }

            return true;
        }

        return false;
    }

    public static string? GetValue(IEnumerable<CommandOptionValue> values, CommandOption option) {
        foreach ((CommandOption co, string? value) in values) {
            if (co != option) {
                continue;
            }

            return value;
        }

        return null;
    }

    public static Platform? GetPlatformOrCurrent(IEnumerable<CommandOptionValue> values, CommandOption option) {
        foreach ((CommandOption co, string? value) in values) {
            if (co != option) {
                continue;
            }

            if (value is null) {
                ConsoleCommandUtils.WriteLineError("Missing value for --platform option.");
                return null;
            }

            if (Enum.TryParse(value, out Platform platform)) {
                return platform;
            }

            ConsoleCommandUtils.WriteLineError(
                $"Invalid platform: `{value}`. List with `{ConsoleCommandUtils.ExeName} platforms`.");
            return null;
        }

        if (OperatingSystem.IsWindows()) {
            return Platform.WindowsAmd64;
        }

        if (OperatingSystem.IsLinux()) {
            return Platform.LinuxAmd64;
        }

        ConsoleCommandUtils.WriteLineError("Could not determine OS. Try using `--platform` option.");
        return null;
    }

    public static string? GetVersionOrLatest(
        IEnumerable<CommandOptionValue> values,
        Platform platform,
        CommandOption option
    ) {
        foreach ((CommandOption co, string? value) in values) {
            if (co == option) {
                return value;
            }
        }

        string? version = VersionUtils.LatestInstalled(platform).Result;

        if (version is null) {
            ConsoleCommandUtils.WriteLineError(
                $"Could not determine latest installed version for platform {platform}.");
        }

        return version;
    }

}
