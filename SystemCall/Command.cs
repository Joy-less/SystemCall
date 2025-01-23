namespace SystemCall;

/// <summary>
/// A command definition that may be called.
/// </summary>
public record Command(object Name, List<CommandComponent> Components) {
    /// <summary>
    /// An identifier for the command (ideally a string or an enum).
    /// </summary>
    public object Name { get; set; } = Name;
    /// <summary>
    /// The components that make up the command.
    /// </summary>
    public List<CommandComponent> Components { get; set; } = Components;

    /// <summary>
    /// Constructs a <see cref="Command"/> by parsing the components in the format string.
    /// </summary>
    public Command(object Name, string Format)
        : this(Name, CommandParser.ParseComponents(Format)) {
    }
}