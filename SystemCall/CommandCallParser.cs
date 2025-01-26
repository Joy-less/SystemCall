using System.Text;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using HjsonSharp;
using LinkDotNet.StringBuilder;

namespace SystemCall;

/// <summary>
/// Contains methods for interpreting command calls.
/// </summary>
public static class CommandCallParser {
    /// <summary>
    /// Parses the input for a sequence of command calls, runs them by invoking the callback function and returns their outputs.
    /// </summary>
    /// <exception cref="CallSyntaxException"/>
    /// <exception cref="CommandNotFoundException"/>
    /// <exception cref="CallArgumentException"/>
    public static List<string?> Interpret(string Input, IEnumerable<Command> Commands, Func<CommandCall, string?> RunCommand) {
        // Run commands in input
        List<string?> Outputs = [];
        try {
            foreach (CommandCall Call in ParseCalls(Input, Commands)) {
                Outputs.Add(RunCommand(Call));
            }
        }
        // Command errored
        catch (SystemCallException Exception) {
            Outputs.Add(Exception.Message);
        }
        // Return success
        return Outputs;
    }
    /// <inheritdoc cref="Interpret(string, IEnumerable{Command}, Func{CommandCall, string?})"/>
    public static async Task<List<string?>> InterpretAsync(string Input, IEnumerable<Command> Commands, Func<CommandCall, Task<string?>> RunCommandAsync) {
        // Run commands in input
        List<string?> Outputs = [];
        try {
            foreach (CommandCall Call in ParseCalls(Input, Commands)) {
                Outputs.Add(await RunCommandAsync(Call));
            }
        }
        // Command errored
        catch (SystemCallException Exception) {
            Outputs.Add(Exception.Message);
        }
        // Return success
        return Outputs;
    }
    /// <summary>
    /// Parses the input for a sequence of command calls.
    /// </summary>
    /// <exception cref="CallSyntaxException"/>
    /// <exception cref="CommandNotFoundException"/>
    public static List<CommandCall> ParseCalls(string Input, IEnumerable<Command> Commands) {
        List<CommandCall> Calls = [];
        // Parse each command call from input tokens
        foreach (List<string> CommandTokens in TokenizeInputCalls(Input)) {
            if (TryParseCall(CollectionsMarshal.AsSpan(CommandTokens), Commands, out CommandCall? Call)) {
                Calls.Add(Call);
            }
            else {
                throw new CommandNotFoundException($"No matching command: `{string.Join(' ', CommandTokens)}`");
            }
        }
        return Calls;
    }
    /// <summary>
    /// Parses the input for exactly one command call.
    /// </summary>
    /// <exception cref="CallSyntaxException"/>
    /// <exception cref="CommandNotFoundException"/>
    public static CommandCall ParseCall(string Input, IEnumerable<Command> Commands) {
        return ParseCalls(Input, Commands).SingleOrDefault()
            ?? throw new CallSyntaxException($"Expected single command: `{Input}`");
    }
    /// <summary>
    /// Parses a list of tokens for each command call in the input.
    /// </summary>
    /// <exception cref="CallSyntaxException"/>
    public static List<List<string>> TokenizeInputCalls(string Input) {
        List<List<string>> TokensPerCall = [];
        List<string> Tokens = [];
        ValueStringBuilder Token = new(stackalloc char[32]);

        bool TrySubmitCall() {
            if (Tokens.Count == 0) {
                return false;
            }
            TokensPerCall.Add(Tokens);
            Tokens = [];
            return true;
        }
        bool TrySubmitToken(ref ValueStringBuilder Token) {
            if (Token.Length == 0) {
                return false;
            }
            Tokens.Add(Token.ToString());
            Token.Clear();
            return true;
        }

        int Index = 0;
        while (Index < Input.Length) {
            // Read rune
            Rune Rune = Rune.GetRuneAt(Input, Index);
            Index += Rune.Utf16SequenceLength;

            // Escape character
            if (Rune.Value is '\\') {
                // Append escape
                Token.Append(Rune);

                // Ensure not trailing escape
                if (Index >= Input.Length) {
                    throw new CallSyntaxException("Incomplete escape sequence: `\\`");
                }

                // Read escaped rune
                Rune EscapedRune = Rune.GetRuneAt(Input, Index);
                Index += EscapedRune.Utf16SequenceLength;

                // Append escaped rune
                Token.Append(EscapedRune);
            }
            // JSON5 character
            else if (Rune.Value is '"' or '\'' or '{' or '}' or '[' or ']' or ':' or '/' or '#') {
                // End previous token
                TrySubmitToken(ref Token);

                // Move to start of element
                Index -= Rune.Utf16SequenceLength;

                // Read JSON5 element
                int ElementLength;
                using (CustomJsonReader Reader = new(Input, Index, Input.Length - Index, CustomJsonReaderOptions.Json5)) {
                    ElementLength = (int)Reader.ReadElementLength().Value;
                }
                ReadOnlySpan<char> RawElement = Input.AsSpan(Index, ElementLength);

                // Move to end of element
                Index += ElementLength;

                // Submit element as token
                Token.Append(RawElement);
                TrySubmitToken(ref Token);
            }
            // End of call
            else if (Rune.Value is '\n' or '\r' or '\u2028' or '\u2029' or ';') {
                // End call
                TrySubmitToken(ref Token);
                TrySubmitCall();
            }
            // Whitespace
            else if (Rune.IsWhiteSpace(Rune)) {
                // End token
                TrySubmitToken(ref Token);
            }
            // Unreserved character
            else {
                Token.Append(Rune);
            }
        }

        // Complete final call
        TrySubmitToken(ref Token);
        TrySubmitCall();

        return TokensPerCall;
    }
    /// <summary>
    /// Parses a list of tokens for exactly one command call in the input.
    /// </summary>
    /// <exception cref="CallSyntaxException"/>
    public static List<string> TokenizeInputCall(string Input) {
        return TokenizeInputCalls(Input).SingleOrDefault()
            ?? throw new CallSyntaxException($"Expected single command: `{Input}`");
    }
    /// <summary>
    /// Parses the tokens as calls for any of the commands.
    /// </summary>
    public static List<CommandCall> ParseMatchingCalls(ReadOnlySpan<string> Tokens, IEnumerable<Command> Commands) {
        List<CommandCall> Calls = [];
        foreach (Command Command in Commands) {
            if (TryParseComponents(Tokens, Command.Components, out int TokenCount, out Dictionary<string, string>? Arguments)) {
                // Ensure all tokens used up
                if (TokenCount < Tokens.Length) {
                    continue;
                }
                // Call matches command
                Calls.Add(new CommandCall(Command, Arguments, TokenCount));
            }
        }
        return Calls;
    }
    /// <summary>
    /// Parses the tokens as a call for any of the commands, prioritizing the call with the most tokens.
    /// </summary>
    public static bool TryParseCall(ReadOnlySpan<string> Tokens, IEnumerable<Command> Commands, [NotNullWhen(true)] out CommandCall? Call) {
        // Match all possible calls
        List<CommandCall> Calls = ParseMatchingCalls(Tokens, Commands);
        // Return call with most tokens
        Call = Calls.MaxBy(Call => Call.TokenCount);
        return Call is not null;
    }
    /// <summary>
    /// Parses the tokens as the sequence of command components.
    /// </summary>
    public static bool TryParseComponents(ReadOnlySpan<string> Tokens, IEnumerable<CommandComponent> Components, out int TokenCount, [NotNullWhen(true)] out Dictionary<string, string>? Arguments) {
        TokenCount = 0;
        Arguments = [];

        // Match each component
        foreach (CommandComponent Component in Components) {
            // Component mismatched
            if (!TryParseComponent(Tokens[TokenCount..], Component, out int ComponentTokenCount, out Dictionary<string, string>? NewArguments)) {
                TokenCount = 0;
                Arguments = null;
                return false;
            }
            // Component matched
            TokenCount += ComponentTokenCount;
            // Pass arguments
            if (NewArguments is not null) {
                foreach ((string Name, string Value) in NewArguments) {
                    Arguments[Name] = Value;
                }
            }
        }
        // All components matched
        return true;
    }
    /// <summary>
    /// Parses the tokens as the command component.
    /// </summary>
    public static bool TryParseComponent(ReadOnlySpan<string> Tokens, CommandComponent Component, out int TokenCount, out Dictionary<string, string>? Arguments) {
        TokenCount = 0;
        Arguments = null;

        switch (Component) {
            // Optional
            case CommandOptionalComponent OptionalComponent: {
                // Match every optional component
                if (TryParseComponents(Tokens, OptionalComponent.Components, out TokenCount, out Arguments)) {
                    // Optional components matched
                    return true;
                }
                // Not matched (OK)
                return true;
            }
            // Argument
            case CommandArgumentComponent ArgumentComponent: {
                // Match a token as the argument
                if (!Tokens.IsEmpty) {
                    // Get argument value
                    string ArgumentValue = Tokens[TokenCount];
                    // Next token
                    TokenCount++;
                    // Argument matched
                    Arguments = new() {
                        [ArgumentComponent.ArgumentName] = ArgumentValue
                    };
                    return true;
                }
                // Not matched
                return false;
            }
            // Literal
            case CommandLiteralComponent LiteralComponent: {
                // Match literal
                if (LiteralComponent.TryMatch(Tokens, out TokenCount)) {
                    // Literal matched
                    return true;
                }
                // Not matched
                return false;
            }
            // Choices
            case CommandChoicesComponent ChoicesComponent: {
                // Match any of the choices
                foreach (List<CommandComponent> Choice in ChoicesComponent.Choices) {
                    if (TryParseComponents(Tokens, Choice, out TokenCount, out Arguments)) {
                        // Choice matched
                        return true;
                    }
                }
                // Not matched
                return false;
            }
            // Not implemented
            default: {
                throw new NotImplementedException($"Component not implemented: '{Component.GetType().Name}'");
            }
        }
    }
}