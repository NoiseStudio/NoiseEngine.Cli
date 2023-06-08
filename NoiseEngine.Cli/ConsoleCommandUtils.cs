using System;
using System.Linq;

namespace NoiseEngine.Cli;

public static class ConsoleCommandUtils {

    public static string ExeName { get; } = System.Diagnostics.Process.GetCurrentProcess().ProcessName;

    public static void WriteLineError(string message) {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write("Error: ");
        Console.ResetColor();
        Console.WriteLine(message);
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
