using System;
using System.Linq;

namespace NoiseEngine.Cli;

public static class ConsoleCommandUtils {

    public static string ExeName { get; } = System.Diagnostics.Process.GetCurrentProcess().ProcessName;

    public static void WriteLineWarning(string message) {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("Warning: ");
        Console.ResetColor();
        Console.WriteLine(message);
    }

    public static void WriteLineError(string message) {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write("Error: ");
        Console.ResetColor();
        Console.WriteLine(message);
    }

    public static void UpdateProgressBar(long current, long total, int width = 20) {
        int progress = (int)Math.Round((double)current / total * width);
        Console.Write($"\r[{new string('#', progress).PadRight(width)}] {current}/{total}");
    }

    public static string Indent(string s, int level = 1) {
        string indent = new string(' ', level * 2);
        return indent + s.Replace("\n", "\n" + indent);
    }

    public static string Align((string lhs, string rhs)[] pairs) {
        int maxLhsLength = pairs.Max(p => p.lhs.Length);
        return string.Join('\n', pairs.Select(p => p.lhs.PadRight(maxLhsLength) + p.rhs));
    }

}
