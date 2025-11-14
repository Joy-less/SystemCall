using System.Diagnostics.CodeAnalysis;

namespace SystemCall;

/// <summary>
/// A command definition that may be called.
/// </summary>
public partial class Command {
    /// <summary>
    /// An identifier for the command (ideally a string or an enum).
    /// </summary>
    public object Name { get; set; }
    /// <summary>
    /// The components that make up the command.
    /// </summary>
    public List<CommandComponent> Components { get; set; }
    /// <summary>
    /// The function that asynchronously executes the action and returns a result.
    /// </summary>
    public Func<CommandCall, Task<object?>>? ExecuteAsync { get; set; }
    /// <summary>
    /// The function that synchronously executes the action and returns a result.
    /// </summary>
    /// <remarks>
    /// This is a wrapper for <see cref="ExecuteAsync"/>.
    /// </remarks>
    public Func<CommandCall, object?>? Execute {
        get => ConvertExecuteAsyncToExecute(ExecuteAsync);
        set => ConvertExecuteToExecuteAsync(value);
    }

    /// <summary>
    /// Constructs a <see cref="Command"/> from the components.
    /// </summary>
    public Command(object Name, List<CommandComponent> Components, Func<CommandCall, Task<object?>>? ExecuteAsync = null) {
        this.Name = Name;
        this.Components = Components;
        this.ExecuteAsync = ExecuteAsync;
    }
    /// <inheritdoc cref="Command(object, List{CommandComponent}, Func{CommandCall, Task{object?}})"/>
    public Command(object Name, List<CommandComponent> Components, Func<CommandCall, object?>? Execute)
        : this(Name, Components, ConvertExecuteToExecuteAsync(Execute)) {
    }
    /// <summary>
    /// Constructs a <see cref="Command"/> by parsing the components in the format string.
    /// </summary>
    public Command(object Name, string Format, Func<CommandCall, Task<object?>>? ExecuteAsync = null) {
        this.Name = Name;
        this.Components = ParseComponents(Format);
        this.ExecuteAsync = ExecuteAsync;
    }
    /// <inheritdoc cref="Command(object, string, Func{CommandCall, Task{object?}})"/>
    public Command(object Name, string Format, Func<CommandCall, object?>? Execute)
        : this(Name, Format, ConvertExecuteToExecuteAsync(Execute)) {
    }

    [return: NotNullIfNotNull(nameof(Execute))]
    private static Func<CommandCall, Task<object?>>? ConvertExecuteToExecuteAsync(Func<CommandCall, object?>? Execute) {
        if (Execute is null) {
            return null;
        }
        return (CommandCall Call) => {
            return Task.FromResult(Execute(Call));
        };
    }
    [return: NotNullIfNotNull(nameof(ExecuteAsync))]
    private static Func<CommandCall, object?>? ConvertExecuteAsyncToExecute(Func<CommandCall, Task<object?>>? ExecuteAsync) {
        if (ExecuteAsync is null) {
            return null;
        }
        return (CommandCall Call) => {
            return ExecuteAsync(Call).GetAwaiter().GetResult();
        };
    }
}