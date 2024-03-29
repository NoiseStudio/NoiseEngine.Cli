using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace NoiseEngine.Cli;

public static class ConsoleCommandUtils {

    public static JsonSerializerOptions JsonOptions { get; } = new JsonSerializerOptions {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

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

    public static void WriteInvalidUsage(string message, string usage) {
        WriteLineError(message);
        Console.WriteLine();
        Console.WriteLine($"Usage: `{usage}`");
    }

    public static bool PromptYesNo(string message) {
        while (true) {
            Console.Write($"{message} [y/n]: ");
            ConsoleKeyInfo key = Console.ReadKey();
            Console.WriteLine();

            switch (key.Key) {
                case ConsoleKey.Y:
                    return true;
                case ConsoleKey.N:
                    return false;
                default:
                    Console.WriteLine("Invalid input.");
                    break;
            }
        }
    }

    public static void UpdateProgressBar(double current, double total, int width = 20) {
        int progress = (int)Math.Round(current / total * width);
        Console.Write($"\r[{new string('#', progress).PadRight(width)}]");
    }

    public static string Indent(string s, int level = 1) {
        string indent = new string(' ', level * 2);
        return indent + s.Replace("\n", "\n" + indent);
    }

    public static string Align((string lhs, string rhs)[] pairs) {
        int maxLhsLength = pairs.Max(p => p.lhs.Length);
        return string.Join('\n', pairs.Select(p => p.lhs.PadRight(maxLhsLength) + p.rhs));
    }

     public static async Task<string?> TryDownloadFile(Uri uri) {
         Console.WriteLine($"Downloading file `{uri}`...");

         try {
             using HttpClient client = new HttpClient();
             using HttpResponseMessage response = await client.GetAsync(uri);

             if (response.StatusCode != HttpStatusCode.OK) {
                 WriteLineError(
                     $"Could not access file at `{uri}`. Status code: `{(int)response.StatusCode} {response.StatusCode}`.");
                 return null;
             }

             if (response.Content.Headers.ContentLength is null) {
                 WriteLineWarning("Could not determine file size. This is likely a bug on the server.");
             }

             string path = Path.GetTempFileName();

             await using FileStream file = File.Create(path);
             await using Stream stream = await response.Content.ReadAsStreamAsync();

             byte[] buffer = new byte[4096];
             int read;
             long? total = response.Content.Headers.ContentLength;
             long totalRead = 0;

             while ((read = await stream.ReadAsync(buffer)) > 0) {
                 await file.WriteAsync(buffer.AsMemory(0, read));
                 totalRead += read;

                 if (total is null)
                     continue;

                 if (total < 1024) {
                     UpdateProgressBar(totalRead, total.Value);
                     Console.Write($" {totalRead}/{total} bytes");
                 } else if (total < 1024 * 1024) {
                     UpdateProgressBar(totalRead, total.Value);
                     Console.Write($" {totalRead / 1024:F2}/{total / 1024:F2} KiB");
                 } else {
                     UpdateProgressBar(totalRead, total.Value);
                     Console.Write($" {totalRead / 1024 / 1024:F2}/{total / 1024 / 1024:F2} MiB");
                 }
             }

             if (total is not null) {
                 Console.WriteLine();
                 Console.WriteLine();
             }

             return path;
         } catch (Exception) {
             WriteLineWarning($"Could not download file from `{uri}`.");
             return null;
         }
     }

     public static string MakeRootedWithExeAsBase(string path) {
         if (Path.IsPathRooted(path)) {
             return path;
         }

         return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
     }

}
