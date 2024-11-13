namespace SystemCall;

public abstract record CommandComponent;
public record CommandOptionalComponent(List<CommandComponent> Components) : CommandComponent {
    public readonly List<CommandComponent> Components = Components;
}
public record CommandArgumentComponent(string Argument) : CommandComponent {
    public readonly string Argument = Argument;
}
public record CommandLiteralComponent(string Literal, bool CaseSensitive = false) : CommandComponent {
    public readonly string Literal = Literal;
    public readonly bool CaseSensitive = CaseSensitive;

    private readonly string[] LiteralTokens = [.. CommandCallParser.TokenizeInput(Literal).SingleOrDefault()
        ?? throw new CommandSyntaxException($"Command literal contains line breaks: '{Literal}'") ];

    public StringComparison GetStringComparison() {
        return CaseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase;
    }
    public bool TryMatch(ReadOnlySpan<string> Tokens, out int TokenCount) {
        for (int Index = 0; Index < LiteralTokens.Length; Index++) {
            string LiteralToken = LiteralTokens[Index];
            string? Token = Index < Tokens.Length ? Tokens[Index] : null;

            // Token mismatch
            if (!LiteralToken.Equals(Token, GetStringComparison())) {
                TokenCount = 0;
                return false;
            }
        }
        // All tokens match
        TokenCount = LiteralTokens.Length;
        return true;
    }
}
public record CommandChoicesComponent(List<CommandComponent> Choices) : CommandComponent {
    public readonly List<CommandComponent> Choices = Choices;
}