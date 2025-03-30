namespace SystemCall.Tests;

public class ReadMeTest {
    [Fact]
    public void Test1() {
        // Define commands
        Command[] Commands = [
            new("enhance_weapon", "enhance my {weapon}(!)", Call => {
                return $"Weapon enhanced: {Call.GetArgument<string>("weapon")}";
            }),
        ];

        // Call commands
        string.Join("\n", CommandCall.Execute("Enhance my 'Sword'!", Commands)).ShouldBe("Weapon enhanced: Sword");
    }
    [Fact]
    public void Test2() {
        // Define commands
        Command[] Commands = [
            new("enhance_weapon", [
                new CommandLiteralComponent("enhance my"),
                new CommandArgumentComponent("weapon"),
                new CommandOptionalComponent([
                    new CommandLiteralComponent("!"),
                ]),
            ], Call => {
                return $"Weapon enhanced: {Call.GetArgument<string>("weapon")}";
            }),
        ];

        // Call commands
        string.Join("\n", CommandCall.Execute("Enhance my 'Sword'!", Commands)).ShouldBe("Weapon enhanced: Sword");
    }
}