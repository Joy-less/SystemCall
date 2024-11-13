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

        bool Optional = false;
        StringBuilder LiteralBuilder = new();
        List<CommandComponent> OptionalBuilder = [];

        void AddComponent(CommandComponent Component) {
            if (Optional) {
                OptionalBuilder.Add(Component);
                return;
            }
            Components.Add(Component);
        }
        void CompleteLiteral() {
            // Take literal
            string Literal = LiteralBuilder.ToString().Trim();
            LiteralBuilder.Clear();

            // Ensure literal present
            if (string.IsNullOrWhiteSpace(Literal)) {
                return;
            }

            // Add literal
            AddComponent(new CommandLiteralComponent(Literal));
        }
        void CompleteOptional() {
            // Ensure components present
            if (OptionalBuilder.Count == 0) {
                return;
            }

            // Take optional components
            List<CommandComponent> OptionalComponents = Components;
            Components = [];

            // Add optional
            AddComponent(new CommandOptionalComponent(OptionalComponents));
        }

        for (int Index = 0; Index < Format.Length; Index++) {
            char Char = Format[Index];

            if (Char is '(') {
                // Complete previous literal
                CompleteLiteral();

                // Disallow recursion
                if (Optional) {
                    throw new CommandSyntaxException("Invalid recursion: '('");
                }
                // Start optional component
                Optional = true;
            }
            else if (Char is ')') {
                // Complete previous literal
                CompleteLiteral();

                // Unexpected close bracket
                if (!Optional) {
                    throw new CommandSyntaxException("Unexpected bracket: ')'");
                }
                // Ensure components present
                if (OptionalBuilder.Count == 0) {
                    throw new CallSyntaxException($"Expected token in brackets: '{Format}'");
                }
                // End optional component
                Optional = false;
                CompleteOptional();
            }
            else if (Char is '{') {
                // Complete previous literal
                CompleteLiteral();

                // Find closing bracket
                int EndArgumentIndex = Format.IndexOf('}', Index + 1);
                if (EndArgumentIndex < 0) {
                    throw new CommandSyntaxException("Unclosed bracket: '{'");
                }

                // Get argument in brackets
                string Argument = Format[(Index + 1)..EndArgumentIndex];
                // Move past argument
                Index = EndArgumentIndex;

                // Disallow recursion
                if (Argument.Contains('{')) {
                    throw new CommandSyntaxException("Invalid recursion: '{'");
                }

                // Add argument
                AddComponent(new CommandArgumentComponent(Argument));
            }
            else if (Char is '}') {
                // Unexpected close bracket
                throw new CommandSyntaxException("Unexpected bracket: '}'");
            }
            else if (Char is '[') {
                // Complete previous literal
                CompleteLiteral();

                // Find closing bracket
                int EndChoicesIndex = Format.IndexOf(']', Index + 1);
                if (EndChoicesIndex < 0) {
                    throw new CommandSyntaxException("Unclosed bracket: '['");
                }

                // Get choices in brackets
                string Choices = Format[(Index + 1)..EndChoicesIndex];
                // Move past choices
                Index = EndChoicesIndex;
                // Split choices by commas
                string[] ChoiceList = Choices.Split(',', StringSplitOptions.TrimEntries);
                // Parse choices as components
                List<CommandComponent> ChoiceComponents = ChoiceList.Select(Choice => ParseComponents(Choice).SingleOrDefault()
                    ?? throw new CommandSyntaxException($"Choice cannot contain multiple components: '{Choice}'")).ToList();

                // Add choices
                AddComponent(new CommandChoicesComponent(ChoiceComponents));
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

        // Flush tokens
        CompleteLiteral();
        CompleteOptional();

        return Components;
    }
}