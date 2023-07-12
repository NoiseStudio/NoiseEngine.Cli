using System;
using System.Linq;
using NoiseEngine.Cli.Options;

namespace NoiseEngine.Cli.Commands;

public interface IConsoleCommand {

    public string Name { get; }
    public string[] Aliases { get; }
    public string Description { get; }
    public string Usage { get; }
    public CommandOption[] Options { get; }
    public string? LongDescription { get; }

    public string NameAliasList => string.Join(", ", new[] { Name }.Concat(Aliases));

    public bool Execute(ReadOnlySpan<string> args);

}
