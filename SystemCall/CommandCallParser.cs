using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace SystemCall;

/// <summary>
/// Contains methods for interpreting command calls.
/// </summary>
public static class CommandCallParser {
    /// <summary>
    /// Parses the input for a sequence of command calls, runs them by invoking the callback function and compiles their output to a string.
    /// </summary>
    /// <exception cref="CallSyntaxException"/>
    /// <exception cref="CommandNotFoundException"/>
    /// <exception cref="CallArgumentException"/>
    public static async Task<string> InterpretAsync(string Input, IEnumerable<Command> Commands, Func<CommandCall, Task<string?>> RunCommandAsync, string OutputSeparator = "\n") {
        // Run commands in input
        StringBuilder Output = new();
        try {
            List<CommandCall> Calls = ParseCalls(Input, Commands);
            foreach (CommandCall Call in Calls) {
                Output.AppendLine(await RunCommandAsync(Call) + OutputSeparator);
            }
        }
        // Command errored
        catch (SystemCallException Exception) {
            Output.AppendLine(Exception.Message + OutputSeparator);
        }
        // Return success
        return Output.ToString().Trim();
    }
    /// <inheritdoc cref="InterpretAsync(string, IEnumerable{Command}, Func{CommandCall, Task{string?}}, string)"/>
    public static string Interpret(string Input, IEnumerable<Command> Commands, Func<CommandCall, string?> RunCommand, string OutputSeparator = "\n") {
        // Run commands in input
        StringBuilder Output = new();
        try {
            List<CommandCall> Calls = ParseCalls(Input, Commands);
            foreach (CommandCall Call in Calls) {
                Output.Append(RunCommand(Call) + OutputSeparator);
            }
        }
        // Command errored
        catch (SystemCallException Exception) {
            Output.Append(Exception.Message + OutputSeparator);
        }
        // Return success
        return Output.ToString().Trim();
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
            if (TryParseCall(CommandTokens.ToArray(), Commands, out CommandCall? Call)) {
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
        StringBuilder Token = new();
        char? InQuote = null;
        bool Escaping = false;

        bool TryCommitTokens() {
            if (Tokens.Count == 0) {
                return false;
            }
            TokensPerCall.Add(Tokens);
            Tokens = [];
            return true;
        }
        bool TryAddToken() {
            if (Token.Length == 0) {
                return false;
            }
            Tokens.Add(Token.ToString());
            Token.Clear();
            return true;
        }

        for (int Index = 0; Index < Input.Length; Index++) {
            char Char = Input[Index];

            if (Escaping) {
                Escaping = false;
                // Escape sequence
                Token.Append(CommandUtilities.EscapeCharacter(Char));
            }
            else if (Char is '\\') {
                // Escaped backslash
                if (Escaping) {
                    Escaping = false;
                    Token.Append(Char);
                }
                // Start escaping character
                else {
                    Escaping = true;
                }
            }
            else if (Char is '"' or '\'') {
                // Quote inside different quotes
                if (InQuote is not null && InQuote != Char) {
                    Token.Append(Char);
                }
                // End quote
                else if (InQuote == Char) {
                    InQuote = null;
                    Token.Append(Char);

                    // Add token in quotes
                    TryAddToken();
                }
                // Start quote
                else {
                    // Add previous token
                    TryAddToken();

                    InQuote = Char;
                    Token.Append(Char);
                }
            }
            else if (Char is '\n' or '\r' or ';') {
                // Line break in quotes
                if (InQuote is not null) {
                    Token.Append(Char);
                }
                // End command
                else {
                    TryAddToken();
                    TryCommitTokens();
                }
            }
            else if (Char is '(' or '[' or '{') {
                // Add previous token
                TryAddToken();

                // Find closing bracket
                int EndContentsIndex = CommandUtilities.FindClosingBracket(Input, Index, Char, Char switch {
                    '(' => ')',
                    '[' => ']',
                    '{' => '}',
                    _ => throw new NotImplementedException()
                });
                if (EndContentsIndex < 0) {
                    throw new CommandSyntaxException("Unclosed bracket: '{'");
                }

                // Get contents in brackets
                string Contents = Input[(Index + 1)..EndContentsIndex];
                // Move past contents
                Index = EndContentsIndex;

                // Add Hjson as single token
                Token.Append(Contents);
            }
            else if (char.IsWhiteSpace(Char)) {
                // Whitespace in quotes
                if (InQuote is not null) {
                    Token.Append(Char);
                }
                // End token
                else {
                    TryAddToken();
                }
            }
            else {
                // Unreserved character
                Token.Append(Char);
            }
        }

        // Trailing quote
        if (InQuote is not null) {
            throw new CallSyntaxException($"Unclosed quotes: `{InQuote}`");
        }
        // Trailing escape
        if (Escaping) {
            throw new CallSyntaxException("Incomplete escape sequence: `\\`");
        }

        // Complete final call
        TryAddToken();
        TryCommitTokens();

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