namespace NoiseEngine.Cli;

public record Settings {

    public string[] InstallUrls { get; init; } = { "http://localhost:8080/" };
    public string InstallDirectory { get; init; } = "./versions";

}
