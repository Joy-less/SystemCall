namespace SystemCall.Tests;

public class EscapeTest {
    private readonly Command[] Commands = [
        new("print", "print {object}"),
        new("esc", @"esc\{"),
        new("choices", @"[a\,b, c]"),
    ];

    [Fact]
    public void Test1() {
        CommandCallParser.ParseCall(@"print 'abc\nde\t'", Commands).GetArgument<string>("object").ShouldBe("abc\nde\t");
    }
    [Fact]
    public void Test2() {
        CommandCallParser.ParseCall(@"print 'speech marks""\'""""'", Commands).GetArgument<string>("object").ShouldBe("speech marks\"\'\"\"");
    }
    [Fact]
    public void Test3() {
        CommandCallParser.ParseCall(@"esc\{", Commands);
    }
    [Fact]
    public void Test4() {
        CommandCallParser.ParseCall(@"a,b", Commands);
        CommandCallParser.ParseCall(@"c", Commands);
    }
}