namespace NoiseEngine.Cli.Versions;

public record VersionDetails(
    int IndexApiVersion,
    string Version,
    bool PreRelease,
    string UniversalSha256,
    string[] UniversalUrls,
    string ExtensionWindowsAmd64Sha256,
    string[] ExtensionWindowsAmd64Urls,
    string ExtensionLinuxAmd64Sha256,
    string[] ExtensionLinuxAmd64Urls
);
