namespace NoiseEngine.Cli.Versions;

public record VersionIndex(
    int IndexApiVersion,
    VersionInfo[] Versions
);
