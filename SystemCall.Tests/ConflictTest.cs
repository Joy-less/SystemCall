namespace SystemCall.Tests;

public class ConflictTest {
    private readonly Command[] Commands = [
        new("kill", "kill {name}"),
        new("kill_me", "kill me"),
    ];

    [Fact]
    public void Test1() {
        Assert.Equal("kill", CommandCallParser.ParseCall("kill \"me\"", Commands).Command.Name);
        Assert.Equal("kill_me", CommandCallParser.ParseCall("kill me", Commands).Command.Name);
    }
}