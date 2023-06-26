namespace NoiseEngine.Cli;

public record Settings {

    public string[] InstallUrls { get; init; } = { "http://127.0.0.1:8080/" };
    public string InstallDirectory { get; init; } = "./versions";

}
