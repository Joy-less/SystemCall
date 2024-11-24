namespace SystemCall.Tests;

public class EscapeTest {
    private static readonly Command[] Commands = [
        new("print", "print {object}"),
        new("esc", @"esc\{"),
    ];

    [Fact]
    public void Test1() {
        Assert.Equal("abc\nde\t", CommandCallParser.ParseCall(@"print 'abc\nde\t'", Commands).GetArgument("object"));
    }
    [Fact]
    public void Test2() {
        Assert.Equal("speech marks\"\'\"\"", CommandCallParser.ParseCall(@"print 'speech marks""\'""""'", Commands).GetArgument("object"));
    }
    [Fact]
    public void Test3() {
        CommandCallParser.ParseCall(@"esc\{", Commands);
    }
}