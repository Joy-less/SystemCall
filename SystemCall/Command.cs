namespace SystemCall;

public record Command(object Name, List<CommandComponent> Components) {
    public object Name = Name;
    public List<CommandComponent> Components = Components;

    public Command(object Name, string Format) : this(Name, CommandParser.ParseComponents(Format)) {
    }
}