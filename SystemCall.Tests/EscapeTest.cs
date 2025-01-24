namespace SystemCall.Tests;

public class EscapeTest {
    private readonly Command[] Commands = [
        new("print", "print {object}"),
        new("esc", @"esc\{"),
        new("choices", @"[a\,b, c]"),
    ];

    [Fact]
    public void Test1() {
        Assert.Equal("abc\nde\t", CommandCallParser.ParseCall(@"print 'abc\nde\t'", Commands).GetArgument<string>("object"));
    }
    [Fact]
    public void Test2() {
        Assert.Equal("speech marks\"\'\"\"", CommandCallParser.ParseCall(@"print 'speech marks""\'""""'", Commands).GetArgument<string>("object"));
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