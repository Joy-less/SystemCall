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

        void SubmitLiteral() {
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

        int Index = 0;
        while (Index < Format.Length) {
            // Read rune
            Rune Rune = Rune.GetRuneAt(Format, Index);
            Index += Rune.Utf16SequenceLength;

            if (Rune.Value is '\\') {
                // Append escape
                LiteralBuilder.Append(Rune);

                // Ensure not trailing escape
                if (Index >= Format.Length) {
                    throw new CallSyntaxException("Incomplete escape sequence: `\\`");
                }

                // Read escaped rune
                Rune EscapedRune = Rune.GetRuneAt(Format, Index);
                Index += EscapedRune.Utf16SequenceLength;

                // Append escaped rune
                LiteralBuilder.Append(EscapedRune);
            }
            else if (Rune.Value is '(') {
                // Complete previous literal
                SubmitLiteral();

                // Find closing bracket
                int EndContentsSubIndex = Format.AsSpan(Index).FindClosingBracket('(', ')', '\\');
                if (EndContentsSubIndex < 0) {
                    throw new CommandSyntaxException("Unclosed bracket: '('");
                }
                int EndContentsIndex = Index + EndContentsSubIndex;

                // Get contents in brackets
                string Contents = Format[Index..EndContentsIndex];
                // Move past contents
                Index = EndContentsIndex + 1;

                // Parse contents as components
                List<CommandComponent> ContentsComponents = ParseComponents(Contents);
                // Add optional
                Components.Add(new CommandOptionalComponent(ContentsComponents));
            }
            else if (Rune.Value is ')') {
                // Unexpected close bracket
                throw new CommandSyntaxException("Unexpected bracket: ')'");
            }
            else if (Rune.Value is '{') {
                // Complete previous literal
                SubmitLiteral();

                // Find closing bracket
                int EndContentsSubIndex = Format.AsSpan(Index).FindClosingBracket('{', '}', '\\');
                if (EndContentsSubIndex < 0) {
                    throw new CommandSyntaxException("Unclosed bracket: '{'");
                }
                int EndContentsIndex = Index + EndContentsSubIndex;

                // Get argument in brackets
                string Argument = Format[Index..EndContentsIndex];
                // Move past argument
                Index = EndContentsIndex + 1;

                // Disallow recursion
                if (Argument.Contains('{')) {
                    throw new CommandSyntaxException("Invalid recursion: '{'");
                }
                // Add argument
                Components.Add(new CommandArgumentComponent(Argument));
            }
            else if (Rune.Value is '}') {
                // Unexpected close bracket
                throw new CommandSyntaxException("Unexpected bracket: '}'");
            }
            else if (Rune.Value is '[') {
                // Complete previous literal
                SubmitLiteral();

                // Find closing bracket
                int EndContentsSubIndex = Format.AsSpan(Index).FindClosingBracket('[', ']', '\\');
                if (EndContentsSubIndex < 0) {
                    throw new CommandSyntaxException("Unclosed bracket: '['");
                }
                int EndContentsIndex = Index + EndContentsSubIndex;

                // Get contents in brackets
                ReadOnlySpan<char> Contents = Format.AsSpan(Index..EndContentsIndex);
                // Move past contents
                Index = EndContentsIndex + 1;

                // Split choices by commas
                IEnumerable<string> Choices = Contents.SplitWithEscape(',', '\\').Select(Choice => Choice.Trim());
                // Parse choices as components
                List<List<CommandComponent>> ChoiceComponents = Choices.Select(ParseComponents).ToList();
                // Add choices
                Components.Add(new CommandChoicesComponent(ChoiceComponents));
            }
            else if (Rune.Value is ']') {
                // Unexpected close bracket
                throw new CommandSyntaxException("Unexpected bracket: ']'");
            }
            else {
                // Unreserved character
                LiteralBuilder.Append(Rune);
            }
        }

        // Complete final literal
        SubmitLiteral();

        return Components;
    }
}