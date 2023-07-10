using System;

namespace NoiseEngine.Cli;

public record Settings {

    private static Settings? instance;

    public static Settings Instance {
        get => instance ?? throw new InvalidOperationException();
        set {
            if (instance is not null) {
                throw new InvalidOperationException();
            }

            instance = value;
        }
    }

    public string[] InstallUrls { get; init; } = { "http://127.0.0.1:8080/" };
    public string InstallDirectory { get; init; } = "./versions";
    public bool AutoDownloadIndex { get; init; } = true;
    public TimeSpan AutoDownloadIndexInterval { get; init; } = TimeSpan.FromHours(12);

}
