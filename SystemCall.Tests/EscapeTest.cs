namespace SystemCall.Tests;

public class EscapeTest {
    private readonly Command[] Commands = [
        new("print", "print {object}"),
        new("esc", @"esc\{"),
        new("choices", @"[a\,b, c]"),
    ];

    [Fact]
    public void Test1() {
        CommandCall.ParseSingle(@"print 'abc\nde\t'", Commands).GetArgument<string>("object").ShouldBe("abc\nde\t");
    }
    [Fact]
    public void Test2() {
        CommandCall.ParseSingle(@"print 'speech marks""\'""""'", Commands).GetArgument<string>("object").ShouldBe("speech marks\"\'\"\"");
    }
    [Fact]
    public void Test3() {
        CommandCall.ParseSingle(@"esc\{", Commands);
    }
    [Fact]
    public void Test4() {
        CommandCall.ParseSingle(@"a,b", Commands);
        CommandCall.ParseSingle(@"c", Commands);
    }
}