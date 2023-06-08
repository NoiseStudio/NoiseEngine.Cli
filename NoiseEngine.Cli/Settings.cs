namespace NoiseEngine.Cli;

public record Settings(
    string InstallUrl = "127.0.0.1",
    string InstallDirectory = "./versions"
);
