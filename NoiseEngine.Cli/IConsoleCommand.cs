using System;
using System.Collections.Generic;
using System.Linq;

namespace NoiseEngine.Cli;

public interface IConsoleCommand {

    string Name { get; }
    string[] Aliases { get; }
    string Description { get; }
    string Usage { get; }
    ConsoleCommandOption[] Options { get; }
    string? LongDescription { get; }

    string NameAliasList => string.Join(", ", new[] { Name }.Concat(Aliases));

    bool Execute(ReadOnlySpan<string> args);

}
