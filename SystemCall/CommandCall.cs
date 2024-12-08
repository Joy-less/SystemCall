using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Hjson;

namespace SystemCall;

/// <summary>
/// Contains information about a call of a command including passed arguments.
/// </summary>
public record CommandCall(Command Command, Dictionary<string, string> Arguments, int TokenCount) {
    /// <summary>
    /// The command that has been called.
    /// </summary>
    public readonly Command Command = Command;
    /// <summary>
    /// The arguments passed to the command.
    /// </summary>
    public readonly Dictionary<string, string> Arguments = Arguments;
    /// <summary>
    /// The number of tokens used to call the command.
    /// </summary>
    public readonly int TokenCount = TokenCount;

    /// <summary>
    /// Deserializes the passed Hjson argument.
    /// </summary>
    public bool TryGetArgument(string Name, [NotNullWhen(true)] out JsonValue? Argument) {
        try {
            // Convert Hjson to object
            Argument = HjsonValue.Parse(Arguments[Name]);
            return true;
        }
        catch (Exception) {
            Argument = default;
            return false;
        }
    }
    /// <summary>
    /// Deserializes the passed Hjson argument as the given type.
    /// </summary>
    public bool TryGetArgument<T>(string Name, [NotNullWhen(true)] out T? Argument) {
        try {
            // Convert Hjson to JSON
            string ArgumentJson = HjsonValue.Parse(Arguments[Name]).ToString();
            // Convert JSON to object
            Argument = JsonSerializer.Deserialize<T>(ArgumentJson)!;
            return true;
        }
        catch (Exception) {
            Argument = default;
            return false;
        }
    }
    /// <summary>
    /// Deserializes the passed Hjson argument.
    /// </summary>
    public JsonValue GetArgument(string Name) {
        if (TryGetArgument(Name, out JsonValue? Argument)) {
            return Argument;
        }
        throw new CallArgumentException($"Invalid argument: '{Name}'");
    }
    /// <summary>
    /// Deserializes the passed Hjson argument as the given type.
    /// </summary>
    public T GetArgument<T>(string Name) {
        if (TryGetArgument(Name, out T? Argument)) {
            return Argument;
        }
        throw new CallArgumentException($"Invalid argument: '{Name}' ({typeof(T).Name})");
    }
    /// <summary>
    /// Deserializes the passed Hjson argument or returns the default.
    /// </summary>
    public JsonValue? GetArgument(string Name, JsonValue? Default = default) {
        if (TryGetArgument(Name, out JsonValue? Argument)) {
            return Argument;
        }
        return Default;
    }
    /// <summary>
    /// Deserializes the passed Hjson argument as the given type or returns the default.
    /// </summary>
    public T? GetArgumentOrDefault<T>(string Name, T? Default = default) {
        if (TryGetArgument(Name, out T? Argument)) {
            return Argument;
        }
        return Default;
    }
    /// <summary>
    /// Returns true if the argument was passed to the call.
    /// </summary>
    public bool HasArgument(string Name) {
        return Arguments.ContainsKey(Name);
    }
}