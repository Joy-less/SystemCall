namespace SystemCall.Tests;

public class SyntaxTest {
    private readonly Command[] Commands = [
        new("kill_user", "kill {user} for {reason}"),
        new("explode", "[explode, blow up, self destruct]"),
        new("show_emoticon", "show (the) emoticon {name} (for {duration} seconds) in [color, colour] {color}"),
        new("test", "test [a, an] [b, c] {lol}"),
    ];

    [Fact]
    public void ParseCommandsTest() {
        Assert.NotNull(CommandCallParser.ParseCall("kill 'player' for 'no reason'", Commands));
        Assert.Equal(2, CommandCallParser.ParseCalls("kill 'player' for 'no reason';blow up", Commands).Count);
    }
    [Fact]
    public void InterpretTest() {
        Assert.Equal("Killed", string.Join("\n", CommandCallParser.Interpret("kill 'player' for 'no reason'", Commands, RunCommand)));
        Assert.Equal("Exploded\nExploded\nExploded", string.Join("\n", CommandCallParser.Interpret("explode\n    explode   ;explode", Commands, RunCommand)));
        Assert.Equal("Showing smile for 3.2s in Red", string.Join("\n", CommandCallParser.Interpret("show emoticon 'smile' for 3.2 seconds in colour 'Red'", Commands, RunCommand)));
        Assert.Equal("Showing smile for 0s in Red", string.Join("\n", CommandCallParser.Interpret("show emoticon 'smile' in colour 'Red'", Commands, RunCommand)));
    }
    [Fact]
    public void Json5Test() {
        Assert.Equal("Killed", string.Join("\n", CommandCallParser.Interpret("kill \"player\" for \"no reason\"", Commands, RunCommand)));
        Assert.Equal("Killed", string.Join("\n", CommandCallParser.Interpret("kill 0 for {reason: 'none \\{\\}'}", Commands, RunCommand)));
    }
    [Fact]
    public void WhitespaceTest() {
        Assert.Equal("Killed", string.Join("\n", CommandCallParser.Interpret("kill   \t  'player'for'no reason'", Commands, RunCommand)));
    }
    [Fact]
    public void ExcessTest() {
        Assert.Throws<CommandNotFoundException>(() => CommandCallParser.ParseCalls("kill 'player' for 'no reason' okay", Commands));
    }

    private string? RunCommand(CommandCall Call) {
        return Call.Command.Name switch {
            "kill_user" => "Killed",
            "explode" => "Exploded",
            "show_emoticon" => $"Showing {Call.GetArgument<string>("name")} for {Call.GetArgumentOrDefault<double>("duration")}s in {Enum.Parse<ConsoleColor>(Call.GetArgument<string>("color")!)}",
            _ => null,
        };
    }
}