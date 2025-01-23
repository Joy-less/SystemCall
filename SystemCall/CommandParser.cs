using System.Text;

namespace SystemCall;

/// <summary>
/// Contains methods for interpreting command definitions.
/// </summary>
public static class CommandParser {
    /// <summary>
    /// Parses the components of the command definition.
    /// </summary>
    /// <exception cref="CommandSyntaxException"></exception>
    public static List<CommandComponent> ParseComponents(string Format) {
        List<CommandComponent> Components = [];
        StringBuilder LiteralBuilder = new();

        void CompleteLiteral() {
            // Take literal
            string Literal = LiteralBuilder.ToString().Trim();
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

            if (Char is '\\') {
                // Trailing escape
                if (Index + 1 >= Format.Length) {
                    throw new CallSyntaxException("Incomplete escape sequence: `\\`");
                }

                // Append escape
                LiteralBuilder.Append(Char);

                // Append escaped character
                Index++;
                char EscapedChar = Format[Index];
                LiteralBuilder.Append(EscapedChar);

                // Append surrogate pair
                if (char.IsHighSurrogate(EscapedChar)) {
                    Index++;
                    char EscapedCharLow = Format[Index];
                    LiteralBuilder.Append(EscapedCharLow);
                }
            }
            else if (Char is '(') {
                // Complete previous literal
                CompleteLiteral();

                // Find closing bracket
                int EndContentsIndex = CommandUtilities.FindClosingBracket(Format, Index, '(', ')');
                if (EndContentsIndex < 0) {
                    throw new CommandSyntaxException("Unclosed bracket: '('");
                }

                // Get contents in brackets
                string Contents = Format[(Index + 1)..EndContentsIndex];
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
            else if (Char is '{') {
                // Complete previous literal
                CompleteLiteral();

                // Find closing bracket
                int EndContentsIndex = CommandUtilities.FindClosingBracket(Format, Index, '{', '}');
                if (EndContentsIndex < 0) {
                    throw new CommandSyntaxException("Unclosed bracket: '{'");
                }

                // Get argument in brackets
                string Argument = Format[(Index + 1)..EndContentsIndex];
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
            else if (Char is '[') {
                // Complete previous literal
                CompleteLiteral();

                // Find closing bracket
                int EndContentsIndex = CommandUtilities.FindClosingBracket(Format, Index, '[', ']');
                if (EndContentsIndex < 0) {
                    throw new CommandSyntaxException("Unclosed bracket: '['");
                }

                // Get contents in brackets
                string Contents = Format[(Index + 1)..EndContentsIndex];
                // Move past contents
                Index = EndContentsIndex;
                // Split choices by commas
                string[] Choices = Contents.Split(',', StringSplitOptions.TrimEntries);
                // Parse choices as components
                List<List<CommandComponent>> ChoiceComponents = Choices.Select(ParseComponents).ToList();

                // Add choices
                Components.Add(new CommandChoicesComponent(ChoiceComponents));
            }
            else if (Char is ']') {
                // Unexpected close bracket
                throw new CommandSyntaxException("Unexpected bracket: ']'");
            }
            else {
                // Unreserved character
                LiteralBuilder.Append(Char);
            }
        }

        // Complete final literal
        CompleteLiteral();

        return Components;
    }
}