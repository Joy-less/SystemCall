namespace SystemCall.Tests;

public class ConflictTest {
    private readonly Command[] Commands = [
        new("kill_me", "kill me"),
        new("kill", "kill {name}"),
    ];

    [Fact]
    public void Test1() {
        Assert.Equal("kill", CommandCallParser.ParseCall("kill \"me\"", Commands).Command.Name);
        Assert.Equal("kill_me", CommandCallParser.ParseCall("kill me", Commands).Command.Name);
    }
}