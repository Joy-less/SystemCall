namespace SystemCall;

/// <summary>
/// An exception thrown by System Call.
/// </summary>
public abstract class SystemCallException(string? Message = null) : Exception(Message);
/// <summary>
/// An exception thrown when the syntax of a command definition is invalid.
/// </summary>
public class CommandSyntaxException(string? Message = null) : SystemCallException(Message);
/// <summary>
/// An exception thrown when the syntax of a command call is invalid.
/// </summary>
public class CallSyntaxException(string? Message = null) : SystemCallException(Message);
/// <summary>
/// An exception thrown when a command call is not matched to an existing command.
/// </summary>
public class CommandNotFoundException(string? Message = null) : SystemCallException(Message);
/// <summary>
/// An exception thrown when a command call argument is the wrong type for a command.
/// </summary>
public class CallArgumentException(string? Message = null) : SystemCallException(Message);