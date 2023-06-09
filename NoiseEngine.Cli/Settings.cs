namespace NoiseEngine.Cli;

public record Settings(
    string InstallUrl = "http://localhost:8080/",
    string InstallDirectory = "./versions"
);
