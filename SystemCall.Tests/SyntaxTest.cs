namespace SystemCall.Tests;

public class SyntaxTest {
    private readonly Command[] Commands = [
        new("kill_user", "kill {user} for {reason}", Call => "Killed"),
        new("explode", "[explode, blow up, self destruct]", Call => "Exploded"),
        new("show_emoticon", "show (the) emoticon {name} (for {duration} seconds) in [color, colour] {color}", Call => $"Showing {Call.GetArgument<string>("name")} for {Call.GetArgumentOrDefault<double>("duration")}s in {Enum.Parse<ConsoleColor>(Call.GetArgument<string>("color")!)}"),
        new("test", "test [a, an] [b, c] {lol}"),
    ];

    [Fact]
    public void ParseCommandsTest() {
        CommandCall.ParseSingle("kill 'player' for 'no reason'", Commands).ShouldNotBeNull();
        CommandCall.ParseAll("kill 'player' for 'no reason';blow up", Commands).Count.ShouldBe(2);
    }
    [Fact]
    public void InterpretTest() {
        string.Join("\n", CommandCall.Execute("kill 'player' for 'no reason'", Commands)).ShouldBe("Killed");
        string.Join("\n", CommandCall.Execute("explode\n    explode   ;explode", Commands)).ShouldBe("Exploded\nExploded\nExploded");
        string.Join("\n", CommandCall.Execute("show emoticon 'smile' for 3.2 seconds in colour 'Red'", Commands)).ShouldBe("Showing smile for 3.2s in Red");
        string.Join("\n", CommandCall.Execute("show emoticon 'smile' in colour 'Red'", Commands)).ShouldBe("Showing smile for 0s in Red");
    }
    [Fact]
    public void JsonhTest() {
        string.Join("\n", CommandCall.Execute("kill \"player\" for \"no reason\"", Commands)).ShouldBe("Killed");
        string.Join("\n", CommandCall.Execute("kill 0 for {reason: 'none \\{\\}'}", Commands)).ShouldBe("Killed");
    }
    [Fact]
    public void WhitespaceTest() {
        string.Join("\n", CommandCall.Execute("kill   \t  'player'for'no reason'", Commands)).ShouldBe("Killed");
    }
    [Fact]
    public void ExcessTest() {
        Should.Throw<CommandNotFoundException>(() => CommandCall.ParseAll("kill 'player' for 'no reason' okay", Commands));
    }
}