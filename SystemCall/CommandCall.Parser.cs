using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using JsonhCs;
using LinkDotNet.StringBuilder;

namespace SystemCall;

partial class CommandCall {
    /// <summary>
    /// Parses the input for a sequence of command calls, executes them and returns their results.
    /// </summary>
    /// <exception cref="CallSyntaxException"/>
    /// <exception cref="CommandNotFoundException"/>
    /// <exception cref="CallArgumentException"/>
    public static List<object?> Execute(scoped ReadOnlySpan<char> Input, scoped ReadOnlySpan<Command> Commands) {
        // Run commands in input
        List<object?> Results = [];
        try {
            foreach (CommandCall Call in ParseAll(Input, Commands)) {
                if (Call.Command.Execute is null) {
                    Results.Add(null);
                    continue;
                }
                Results.Add(Call.Command.Execute.Invoke(Call));
            }
        }
        // Command errored
        catch (SystemCallException Exception) {
            Results.Add(Exception.Message);
        }
        // Return success
        return Results;
    }
    /// <inheritdoc cref="Execute(ReadOnlySpan{char}, ReadOnlySpan{Command})"/>
    public static async Task<List<object?>> ExecuteAsync(ReadOnlyMemory<char> Input, ReadOnlyMemory<Command> Commands) {
        // Run commands in input
        List<object?> Results = [];
        try {
            foreach (CommandCall Call in ParseAll(Input.Span, Commands.Span)) {
                if (Call.Command.ExecuteAsync is null) {
                    Results.Add(null);
                    continue;
                }
                Results.Add(await Call.Command.ExecuteAsync.Invoke(Call));
            }
        }
        // Command errored
        catch (SystemCallException Exception) {
            Results.Add(Exception.Message);
        }
        // Return success
        return Results;
    }
    /// <summary>
    /// Parses the input for a sequence of command calls.
    /// </summary>
    /// <exception cref="CallSyntaxException"/>
    /// <exception cref="CommandNotFoundException"/>
    public static List<CommandCall> ParseAll(scoped ReadOnlySpan<char> Input, scoped ReadOnlySpan<Command> Commands) {
        List<CommandCall> Calls = [];
        // Parse each command call from input tokens
        foreach (List<string> CommandTokens in TokenizeAll(Input)) {
            if (TryParseFromTokens(CollectionsMarshal.AsSpan(CommandTokens), Commands, out CommandCall? Call)) {
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
    public static CommandCall ParseSingle(scoped ReadOnlySpan<char> Input, scoped ReadOnlySpan<Command> Commands) {
        return ParseAll(Input, Commands).SingleOrDefault()
            ?? throw new CallSyntaxException($"Expected single command: `{Input}`");
    }
    /// <summary>
    /// Parses a list of tokens for each command call in the input.
    /// </summary>
    /// <exception cref="CallSyntaxException"/>
    public static List<List<string>> TokenizeAll(scoped ReadOnlySpan<char> Input) {
        List<List<string>> TokensForCalls = [];
        List<string> Tokens = [];

        ValueStringBuilder TokenBuilder = new(stackalloc char[64]);
        using ValueStringBuilder ReadOnlyTokenBuilder = TokenBuilder; // Can't pass using variables by-ref

        bool TrySubmitCall() {
            if (Tokens.Count == 0) {
                return false;
            }
            TokensForCalls.Add(Tokens);
            Tokens = [];
            return true;
        }
        bool TrySubmitToken(ref ValueStringBuilder TokenBuilder) {
            if (TokenBuilder.IsEmpty) {
                return false;
            }
            Tokens.Add(TokenBuilder.ToString());
            TokenBuilder.Clear();
            return true;
        }

        for (int Index = 0; Index < Input.Length; Index++) {
            char Char = Input[Index];

            // Escaped character
            if (Char is '\\') {
                // Append escape
                TokenBuilder.Append(Char);

                // Ensure not trailing escape
                if (Index + 1 >= Input.Length) {
                    throw new CallSyntaxException("Incomplete escape sequence: `\\`");
                }

                // Read escaped character
                Index++;
                char EscapedChar = Input[Index];

                // Append escaped character
                TokenBuilder.Append(EscapedChar);
            }
            // JSONH character
            else if (Char is '"' or '\'' or '{' or '}' or '[' or ']' or ':' or '/' or '#') {
                // End previous token
                TrySubmitToken(ref TokenBuilder);

                // Read JSONH element
                int RawElementLength;
                using (JsonhReader Reader = new(Input[Index..].ToString())) {
                    Reader.ParseNode().ThrowIfError();
                    RawElementLength = (int)Reader.CharCounter;
                }
                ReadOnlySpan<char> RawElement = Input.Slice(Index, RawElementLength);

                // Move to end of element
                Index += RawElementLength - 1;

                // Submit element as token
                TokenBuilder.Append(RawElement);
                TrySubmitToken(ref TokenBuilder);
            }
            // End of call
            else if (Char is '\n' or '\r' or '\u2028' or '\u2029' or ';') {
                // End call
                TrySubmitToken(ref TokenBuilder);
                TrySubmitCall();
            }
            // Whitespace
            else if (char.IsWhiteSpace(Char)) {
                // End token
                TrySubmitToken(ref TokenBuilder);
            }
            // Unreserved character
            else {
                TokenBuilder.Append(Char);
            }
        }

        // Complete final call
        TrySubmitToken(ref TokenBuilder);
        TrySubmitCall();

        return TokensForCalls;
    }
    /// <summary>
    /// Parses a list of tokens for exactly one command call in the input.
    /// </summary>
    /// <exception cref="CallSyntaxException"/>
    public static List<string> TokenizeSingle(scoped ReadOnlySpan<char> Input) {
        return TokenizeAll(Input).SingleOrDefault()
            ?? throw new CallSyntaxException($"Expected single command: `{Input}`");
    }
    /// <summary>
    /// Parses the tokens as possible calls for any of the commands.
    /// </summary>
    public static List<CommandCall> FindMatches(scoped ReadOnlySpan<string> Tokens, scoped ReadOnlySpan<Command> Commands) {
        List<CommandCall> Calls = [];
        foreach (Command Command in Commands) {
            if (TryParseComponents(Tokens, CollectionsMarshal.AsSpan(Command.Components), out int TokenCount, out Dictionary<string, string>? Arguments)) {
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
    /// Parses the tokens as a call for any of the commands, prioritizing the call with the most tokens, otherwise the first command.
    /// </summary>
    public static bool TryParseFromTokens(scoped ReadOnlySpan<string> Tokens, scoped ReadOnlySpan<Command> Commands, [NotNullWhen(true)] out CommandCall? Call) {
        // Match all possible calls
        List<CommandCall> Calls = FindMatches(Tokens, Commands);

        // Return best call (prioritized by most tokens then first command)
        int MaxCallTokens = -1;
        Call = null;
        foreach (CommandCall PossibleCall in Calls) {
            if (PossibleCall.TokenCount > MaxCallTokens) {
                MaxCallTokens = PossibleCall.TokenCount;
                Call = PossibleCall;
            }
        }
        return Call is not null;
    }
    /// <summary>
    /// Parses the tokens as the sequence of command components.
    /// </summary>
    public static bool TryParseComponents(scoped ReadOnlySpan<string> Tokens, scoped ReadOnlySpan<CommandComponent> Components, out int TokenCount, [NotNullWhen(true)] out Dictionary<string, string>? Arguments) {
        TokenCount = 0;
        Arguments = [];

        // Match each component
        for (int Index = 0; Index < Components.Length; Index++) {
            CommandComponent Component = Components[Index];
            ReadOnlySpan<CommandComponent> RemainingComponents = Components[(Index + 1)..];

            // Component mismatched
            if (!TryParseComponent(Tokens[TokenCount..], Component, RemainingComponents, out int ComponentTokenCount, out Dictionary<string, string>? NewArguments)) {
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
    public static bool TryParseComponent(scoped ReadOnlySpan<string> Tokens, CommandComponent Component, scoped ReadOnlySpan<CommandComponent> RemainingComponents, out int TokenCount, out Dictionary<string, string>? Arguments) {
        TokenCount = 0;
        Arguments = null;

        switch (Component) {
            // Optional
            case CommandOptionalComponent OptionalComponent: {
                // Try to match optional component
                if (TryParseComponents(Tokens, CollectionsMarshal.AsSpan(OptionalComponent.Components), out int OptionalTokenCount, out Dictionary<string, string>? OptionalArguments)) {
                    // Check if remaining components can still be parsed after consuming OptionalTokenCount
                    if (TryParseComponents(Tokens[OptionalTokenCount..], RemainingComponents, out _, out _)) {
                        // Remaining components match - consume the optional tokens
                        TokenCount = OptionalTokenCount;
                        Arguments = OptionalArguments;
                        return true;
                    }
                    // If we're at the end and optional matched, that's OK too
                    if (RemainingComponents.IsEmpty) {
                        TokenCount = OptionalTokenCount;
                        Arguments = OptionalArguments;
                        return true;
                    }
                    // Remaining components don't match - skip the optional
                }
                // Not matched (OK for optional)
                return true;
            }
            // Argument
            case CommandArgumentComponent ArgumentComponent: {
                // Match a token as the argument
                if (!Tokens.IsEmpty) {
                    // Get argument JSONH
                    string ArgumentJsonh = Tokens[TokenCount];
                    // Next token
                    TokenCount++;
                    // Argument matched
                    Arguments = new() {
                        [ArgumentComponent.ArgumentName] = ArgumentJsonh
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
                    if (TryParseComponents(Tokens, CollectionsMarshal.AsSpan(Choice), out TokenCount, out Arguments)) {
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
