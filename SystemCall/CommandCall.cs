using System.Text.Json;
using JsonhCs;

namespace SystemCall;

/// <summary>
/// Contains information about a call of a command including passed arguments.
/// </summary>
public partial class CommandCall {
    /// <summary>
    /// The command that has been called.
    /// </summary>
    public Command Command { get; }
    /// <summary>
    /// The arguments passed to the command.
    /// </summary>
    public IDictionary<string, string> Arguments { get; }
    /// <summary>
    /// The number of tokens used to call the command.
    /// </summary>
    public int TokenCount { get; }

    /// <summary>
    /// Constructs a <see cref="CommandCall"/> from the command and arguments.
    /// </summary>
    public CommandCall(Command Command, IDictionary<string, string> Arguments, int TokenCount) {
        this.Command = Command;
        this.Arguments = Arguments;
        this.TokenCount = TokenCount;
    }
    /// <summary>
    /// Deserializes the passed JSONH argument.
    /// </summary>
    public bool TryGetArgument(string Name, out JsonElement Argument) {
        if (Arguments.TryGetValue(Name, out string? ArgumentJsonh)) {
            return JsonhReader.ParseElement(ArgumentJsonh).TryGetValue(out Argument);
        }
        else {
            Argument = default;
            return false;
        }
    }
    /// <summary>
    /// Deserializes the passed JSONH argument as the given type.
    /// </summary>
    public bool TryGetArgument<T>(string Name, out T? Argument) {
        if (TryGetArgument(Name, out JsonElement ArgumentElement)) {
            Argument = ArgumentElement.Deserialize<T>(JsonhReader.MiniJson);
            return true;
        }
        else {
            Argument = default;
            return false;
        }
    }
    /// <summary>
    /// Deserializes the passed JSONH argument.
    /// </summary>
    public JsonElement GetArgument(string Name) {
        if (TryGetArgument(Name, out JsonElement Argument)) {
            return Argument;
        }
        throw new CallArgumentException($"Invalid argument: '{Name}'");
    }
    /// <summary>
    /// Deserializes the passed JSONH argument as the given type.
    /// </summary>
    public T? GetArgument<T>(string Name) {
        if (TryGetArgument(Name, out T? Argument)) {
            return Argument;
        }
        throw new CallArgumentException($"Invalid argument: '{Name}' ({typeof(T).Name})");
    }
    /// <summary>
    /// Deserializes the passed JSONH argument or returns the default.
    /// </summary>
    public JsonElement GetArgument(string Name, JsonElement Default = default) {
        if (TryGetArgument(Name, out JsonElement Argument)) {
            return Argument;
        }
        return Default;
    }
    /// <summary>
    /// Deserializes the passed JSONH argument as the given type or returns the default.
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