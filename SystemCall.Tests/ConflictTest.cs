namespace SystemCall.Tests;

public class ConflictTest {
    private readonly Command[] Commands = [
        new("kill_me", "kill me"),
        new("kill", "kill {name}"),
    ];

    [Fact]
    public void Test1() {
        CommandCallParser.ParseCall("kill \"me\"", Commands).Command.Name.ShouldBe("kill");
        CommandCallParser.ParseCall("kill me", Commands).Command.Name.ShouldBe("kill_me");
    }
}