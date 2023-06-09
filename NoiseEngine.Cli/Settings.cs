namespace NoiseEngine.Cli;

public record Settings(
    string[] InstallUrls = new string[] {"http://localhost:8080/"},
    string InstallDirectory = "./versions"
);
