namespace SystemCall.Tests;

[TestClass]
public class ReadMeTest {
    [TestMethod]
    public void Test1() {
        // Define commands
        Command[] Commands = [
            new("enhance_weapon", "enhance my {weapon}(!)"),
        ];

        // Run commands
        string? RunCommand(CommandCall Call) {
            switch (Call.Command.Name) {
                case "enhance_weapon":
                    return $"Weapon enhanced: {Call.GetArgument<string>("weapon")}";
                default:
                    return null;
            }
        }

        // Call commands
        Assert.AreEqual("Weapon enhanced: Sword", CommandCallParser.Interpret("Enhance my 'Sword'!", Commands, RunCommand));
    }
    [TestMethod]
    public void Test2() {
        // Define commands
        Command[] Commands = [
            new("enhance_weapon", [
                new CommandLiteralComponent("enhance my"),
                new CommandArgumentComponent("weapon"),
                new CommandOptionalComponent([
                    new CommandLiteralComponent("!"),
                ]),
            ]),
        ];

        // Run commands
        string? RunCommand(CommandCall Call) {
            switch (Call.Command.Name) {
                case "enhance_weapon":
                    return $"Weapon enhanced: {Call.GetArgument<string>("weapon")}";
                default:
                    return null;
            }
        }

        // Call commands
        Assert.AreEqual("Weapon enhanced: Sword", CommandCallParser.Interpret("Enhance my 'Sword'!", Commands, RunCommand));
    }
}