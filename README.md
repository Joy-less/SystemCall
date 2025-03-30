# System Call

[![NuGet](https://img.shields.io/nuget/v/SystemCall.svg)](https://www.nuget.org/packages/SystemCall)
 
System Call is a command-parsing .NET library inspired by [Jinx](https://github.com/JamesBoer/Jinx) and named after [Sword Art Online](https://swordartonline.fandom.com/wiki/Sacred_Arts).

It uses natural language syntax and is intended for use in command line interfaces and magic systems.

```
COMMAND: heal [me, everyone, player {name}] by {amount} (points)
CALL: heal player "John Doe" by 10 points
```
```
COMMAND: enhance (my) armament(!)
CALL: Enhance Armament!
```

## Example

```cs
using SystemCall;

// Define commands
Command[] Commands = [
    new("enhance_weapon", "enhance my {weapon}(!)", Call => {
        return $"Weapon enhanced: {Call.GetArgument<string>("weapon")}";
    }),
];

// Call commands
string.Join("\n", CommandCall.Execute("Enhance my 'Sword'!", Commands)).ShouldBe("Weapon enhanced: Sword");
```

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

Calls can be parsed using an input string:
```cs
List<CommandCall> Calls = CommandCall.ParseAll("enhance my 'Sword'!", Commands);
```

Calls use the following syntax:
- Each call is separated by newlines or semicolons: `eat me; drink me`
- Any token can be escaped with a backslash: `not a bracket \(`

Alternatively, calls can be parsed and executed, returning a result:
```cs
List<object?> Outputs = CommandCall.Execute("Enhance my 'Sword'!", Commands);
```

Arguments are parsed as [JSONH](https://github.com/jsonh-org/Jsonh), which is a superset of JSON.

If a call is ambiguous between multiple commands, the first command is prioritized.

## Known Bugs/Limitations

### Optional arguments must have a leading/trailing token

In the following example, `object` is an optional argument:
```
eat ({object}) please
```
However, calling `eat please` will not match the command.
The reason is that `please` is parsed as the optional `object` argument rather than the token.
This is a current limitation with the System Call parser that may be fixed in the future.

The current workaround is to add another token within the brackets:
```
eat (the {object}) please
```