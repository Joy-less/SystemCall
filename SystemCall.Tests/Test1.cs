namespace SystemCall.Tests;

[TestClass]
public class FormatTest {
    private readonly Command[] Commands = [
        new("kill_user", "kill {user} for {reason}"),
        new("explode", "[explode, blow up, self destruct]"),
        new("show_emoticon", "show (the) emoticon {name} (for {duration} seconds) in [color, colour] {color}"),
        new("test", "test [a, an] [b, c] {lol}"),
    ];

    [TestMethod]
    public void ParseCommandsTest() {
        Assert.IsNotNull(CommandCallParser.ParseCall("kill 'player' for 'no reason'", Commands));
        Assert.AreEqual(2, CommandCallParser.ParseCalls("kill 'player' for 'no reason';blow up", Commands).Count);
    }
    [TestMethod]
    public void InterpretTest() {
        Assert.AreEqual("Killed", CommandCallParser.Interpret("kill 'player' for 'no reason'", Commands, RunCommand));
        Assert.AreEqual("Exploded\nExploded\nExploded", CommandCallParser.Interpret("explode\n    explode   ;explode", Commands, RunCommand));
        Assert.AreEqual("Showing smile for 3.2s in Red", CommandCallParser.Interpret("show emoticon 'smile' for 3.2 seconds in colour 'Red'", Commands, RunCommand));
        Assert.AreEqual("Showing smile for 0s in Red", CommandCallParser.Interpret("show emoticon 'smile' in colour 'Red'", Commands, RunCommand));
    }
    [TestMethod]
    public void HjsonTest() {
        Assert.AreEqual("Killed", CommandCallParser.Interpret("kill \"player\" for \"no reason\"", Commands, RunCommand));
        Assert.AreEqual("Killed", CommandCallParser.Interpret("kill 0 for {reason: 'none \\{\\}'}", Commands, RunCommand));
    }
    [TestMethod]
    public void WhitespaceTest() {
        Assert.AreEqual("Killed", CommandCallParser.Interpret("kill   \t  'player'for'no reason'", Commands, RunCommand));
    }
    [TestMethod]
    public void ExcessTest() {
        Assert.ThrowsException<CommandNotFoundException>(() => CommandCallParser.ParseCalls("kill 'player' for 'no reason' okay", Commands));
    }

    private string? RunCommand(CommandCall Call) {
        return Call.Command.Name switch {
            "kill_user" => "Killed",
            "explode" => "Exploded",
            "show_emoticon" => $"Showing {Call.GetArgument<string>("name")} for {Call.GetArgumentOrDefault<double>("duration")}s in {Enum.Parse<ConsoleColor>(Call.GetArgument<string>("color"))}",
            _ => null,
        };
    }
}