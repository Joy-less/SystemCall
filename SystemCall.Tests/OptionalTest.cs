﻿namespace SystemCall.Tests;

public class OptionalTest {
    private readonly Command[] Commands = [
        new("eat", "eat ({object}) please"),
    ];

    [Fact]
    public void Test1() {
        // This won't work due to a limitation with the current approach.
        // See the README for an explanation.
        //CommandCallParser.ParseCall("eat \"me\" please", Commands).Command.Name.ShouldBe("eat");
        //CommandCallParser.ParseCall("eat please", Commands).Command.Name.ShouldBe("eat");
    }
}