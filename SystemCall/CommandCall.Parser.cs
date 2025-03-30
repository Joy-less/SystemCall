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
    public static List<object?> Execute(string Input, IEnumerable<Command> Commands) {
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
    /// <inheritdoc cref="Execute(string, IEnumerable{Command})"/>
    public static async Task<List<object?>> ExecuteAsync(string Input, IEnumerable<Command> Commands) {
        // Run commands in input
        List<object?> Results = [];
        try {
            foreach (CommandCall Call in ParseAll(Input, Commands)) {
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
    public static List<CommandCall> ParseAll(string Input, IEnumerable<Command> Commands) {
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
    public static CommandCall ParseSingle(string Input, IEnumerable<Command> Commands) {
        return ParseAll(Input, Commands).SingleOrDefault()
            ?? throw new CallSyntaxException($"Expected single command: `{Input}`");
    }
    /// <summary>
    /// Parses a list of tokens for each command call in the input.
    /// </summary>
    /// <exception cref="CallSyntaxException"/>
    public static List<List<string>> TokenizeAll(string Input) {
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
            if (TokenBuilder.Length == 0) {
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
                using (JsonhReader Reader = new(Input[Index..])) {
                    Reader.ParseElement().ThrowIfError();
                    RawElementLength = (int)Reader.CharCounter;
                }
                ReadOnlySpan<char> RawElement = Input.AsSpan(Index, RawElementLength);

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
    public static List<string> TokenizeSingle(string Input) {
        return TokenizeAll(Input).SingleOrDefault()
            ?? throw new CallSyntaxException($"Expected single command: `{Input}`");
    }
    /// <summary>
    /// Parses the tokens as possible calls for any of the commands.
    /// </summary>
    public static List<CommandCall> FindMatches(ReadOnlySpan<string> Tokens, IEnumerable<Command> Commands) {
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
    /// Parses the tokens as a call for any of the commands, prioritizing the call with the most tokens, otherwise the first command.
    /// </summary>
    public static bool TryParseFromTokens(ReadOnlySpan<string> Tokens, IEnumerable<Command> Commands, [NotNullWhen(true)] out CommandCall? Call) {
        // Match all possible calls
        List<CommandCall> Calls = FindMatches(Tokens, Commands);
        // Return call with most tokens (choosing first call if ambiguous)
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