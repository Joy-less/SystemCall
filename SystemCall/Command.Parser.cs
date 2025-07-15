using LinkDotNet.StringBuilder;

namespace SystemCall;

partial class Command {
    /// <summary>
    /// Parses the components of the command definition.
    /// </summary>
    /// <exception cref="CommandSyntaxException"></exception>
    public static List<CommandComponent> ParseComponents(string Format) {
        List<CommandComponent> Components = [];

        ValueStringBuilder LiteralBuilder = new(stackalloc char[64]);
        using ValueStringBuilder ReadOnlyLiteralBuilder = LiteralBuilder; // Can't pass using variables by-ref

        void SubmitLiteral(ref ValueStringBuilder LiteralBuilder) {
            // Take literal
            LiteralBuilder.Trim();
            string Literal = LiteralBuilder.ToString();
            LiteralBuilder.Clear();

            // Ensure literal present
            if (string.IsNullOrWhiteSpace(Literal)) {
                return;
            }

            // Add literal
            Components.Add(new CommandLiteralComponent(Literal));
        }

        for (int Index = 0; Index < Format.Length; Index++) {
            char Char = Format[Index];

            // Escaped character
            if (Char is '\\') {
                // Append escape
                LiteralBuilder.Append(Char);

                // Ensure not trailing escape
                if (Index + 1 >= Format.Length) {
                    throw new CallSyntaxException("Incomplete escape sequence: `\\`");
                }

                // Read escaped character
                Index++;
                char EscapedChar = Format[Index];

                // Append escaped character
                LiteralBuilder.Append(EscapedChar);
            }
            // Start optional tokens
            else if (Char is '(') {
                // Complete previous literal
                SubmitLiteral(ref LiteralBuilder);

                // Move past bracket
                Index++;

                // Find closing bracket
                int EndContentsSubIndex = Format.AsSpan(Index).FindClosingBracket('(', ')', '\\');
                if (EndContentsSubIndex < 0) {
                    throw new CommandSyntaxException("Unclosed bracket: '('");
                }
                int EndContentsIndex = Index + EndContentsSubIndex;

                // Get contents in brackets
                string Contents = Format[Index..EndContentsIndex];
                // Move past contents
                Index = EndContentsIndex;

                // Parse contents as components
                List<CommandComponent> ContentsComponents = ParseComponents(Contents);
                // Add optional
                Components.Add(new CommandOptionalComponent(ContentsComponents));
            }
            else if (Char is ')') {
                // Unexpected close bracket
                throw new CommandSyntaxException("Unexpected bracket: ')'");
            }
            // Start argument token
            else if (Char is '{') {
                // Complete previous literal
                SubmitLiteral(ref LiteralBuilder);

                // Move past bracket
                Index++;

                // Find closing bracket
                int EndContentsSubIndex = Format.AsSpan(Index).FindClosingBracket('{', '}', '\\');
                if (EndContentsSubIndex < 0) {
                    throw new CommandSyntaxException("Unclosed bracket: '{'");
                }
                int EndContentsIndex = Index + EndContentsSubIndex;

                // Get argument in brackets
                string Argument = Format[Index..EndContentsIndex];
                // Move past argument
                Index = EndContentsIndex;

                // Disallow recursion
                if (Argument.Contains('{')) {
                    throw new CommandSyntaxException("Invalid recursion: '{'");
                }
                // Add argument
                Components.Add(new CommandArgumentComponent(Argument));
            }
            else if (Char is '}') {
                // Unexpected close bracket
                throw new CommandSyntaxException("Unexpected bracket: '}'");
            }
            // Start choices
            else if (Char is '[') {
                // Complete previous literal
                SubmitLiteral(ref LiteralBuilder);

                // Move past bracket
                Index++;

                // Find closing bracket
                int EndContentsSubIndex = Format.AsSpan(Index).FindClosingBracket('[', ']', '\\');
                if (EndContentsSubIndex < 0) {
                    throw new CommandSyntaxException("Unclosed bracket: '['");
                }
                int EndContentsIndex = Index + EndContentsSubIndex;

                // Get contents in brackets
                ReadOnlySpan<char> Contents = Format.AsSpan(Index..EndContentsIndex);
                // Move past contents
                Index = EndContentsIndex;

                // Split choices by commas
                IEnumerable<string> Choices = Contents.SplitWithEscape(',', '\\', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                // Parse choices as components
                List<List<CommandComponent>> ChoiceComponents = [.. Choices.Select(ParseComponents)];
                // Add choices
                Components.Add(new CommandChoicesComponent(ChoiceComponents));
            }
            else if (Char is ']') {
                // Unexpected close bracket
                throw new CommandSyntaxException("Unexpected bracket: ']'");
            }
            // Unreserved character
            else {
                LiteralBuilder.Append(Char);
            }
        }

        // Complete final literal
        SubmitLiteral(ref LiteralBuilder);

        return Components;
    }
}