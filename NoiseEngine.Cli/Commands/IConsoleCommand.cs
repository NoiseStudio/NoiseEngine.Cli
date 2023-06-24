using System;
using System.Linq;

namespace NoiseEngine.Cli.Commands;

public interface IConsoleCommand {

    public string Name { get; }
    public string[] Aliases { get; }
    public string Description { get; }
    public string Usage { get; }
    public ConsoleCommandOption[] Options { get; }
    public string? LongDescription { get; }

    public string NameAliasList => string.Join(", ", new[] { Name }.Concat(Aliases));

    public bool Execute(ReadOnlySpan<string> args);

}
