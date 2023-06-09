namespace NoiseEngine.Cli.Versions;

public record VersionIndex(
    int IndexApiVersion,
    VersionIndexVersion[] Versions
    );
