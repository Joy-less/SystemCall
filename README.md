# System Call

[![NuGet](https://img.shields.io/nuget/v/SystemCall.svg)](https://www.nuget.org/packages/SystemCall)
 
System Call is a command-parsing .NET library inspired by [Jinx](https://github.com/JamesBoer/Jinx) and named after [Sword Art Online](https://swordartonline.fandom.com/wiki/Sacred_Arts).

It uses natural language syntax and is intended for use in command line interfaces and magic systems.
```cs
using SystemCall;

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
CommandCallParser.Interpret("Enhance my 'Sword'!", Commands, RunCommand);
```

Arguments are parsed as [JSON5](https://github.com/Joy-less/HjsonSharp), which is a superset of JSON.

## Defining Commands

Commands can be defined using a format string:
```cs
Command[] Commands = [
    new("enhance_weapon", "enhance my {weapon}(!)"),
];
```

Format strings use the following syntax:
- A sequence of tokens in brackets may be entirely omitted: `(optional tokens)`
- A token in curly brackets is the name of an argument: `{username}`
- A list of sequences of tokens in square brackets is a list of choices: `[vanilla, chocolate, strawberry and cream]`
- Any token can be escaped with a backslash: `not a bracket \(`

Alternatively, commands can be manually constructed:
```cs
Command[] Commands = [
    new("enhance_weapon", [
        new CommandLiteralComponent("enhance my"),
        new CommandArgumentComponent("weapon"),
        new CommandOptionalComponent([
            new CommandLiteralComponent("!"),
        ]),
    ]),
];
```

## Calling Commands

Commands can be parsed into a list of calls:
```cs
List<CommandCall> Calls = CommandCallParser.ParseCalls("enhance my 'Sword'!", Commands);
```

Alternatively, commands can be parsed and interpreted, returning the output:
```cs
string? RunCommand(CommandCall Call) {
    switch (Call.Command.Name) {
        case "enhance_weapon":
            return $"Weapon enhanced: {Call.GetArgument<string>("weapon")}";
        default:
            return null;
    }
}

List<string?> Outputs = CommandCallParser.Interpret("Enhance my 'Sword'!", Commands, RunCommand);
```

## Note

System Call will migrate from JSON5 to JSONH when it is complete. See [https://github.com/jsonh-org/Jsonh](jsonh-org/Jsonh).