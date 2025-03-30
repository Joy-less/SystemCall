namespace SystemCall.Tests;

public class OptionalTest {
    private readonly Command[] Commands = [
        new("eat", "eat ({object}) please"),
    ];

    [Fact(Skip = "This will fail due to a limitation with the current approach. See the README for an explanation.")]
    public void Test1() {
        CommandCall.ParseSingle("eat \"me\" please", Commands).Command.Name.ShouldBe("eat");
        CommandCall.ParseSingle("eat please", Commands).Command.Name.ShouldBe("eat");
    }
}