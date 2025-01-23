namespace SystemCall;

/// <summary>
/// A grammar component for calling a command.
/// </summary>
public abstract record CommandComponent;
/// <summary>
/// A component that may be entirely omitted.
/// </summary>
public record CommandOptionalComponent(List<CommandComponent> Components) : CommandComponent {
    /// <summary>
    /// The components that may be entirely omitted.
    /// </summary>
    public List<CommandComponent> Components { get; set; } = Components;
}
/// <summary>
/// A component that is passed as an argument.
/// </summary>
public record CommandArgumentComponent(string ArgumentName) : CommandComponent {
    /// <summary>
    /// The name of the argument.
    /// </summary>
    public string ArgumentName { get; set; } = ArgumentName;
}
/// <summary>
/// A component that must be included verbatim.
/// </summary>
public record CommandLiteralComponent(List<string> LiteralTokens, bool CaseSensitive = false) : CommandComponent {
    /// <summary>
    /// The tokens to match.
    /// </summary>
    public List<string> LiteralTokens { get; set; } = LiteralTokens;
    /// <summary>
    /// Whether the literal must be included in the same letter case.
    /// </summary>
    public bool CaseSensitive { get; set; } = CaseSensitive;

    /// <summary>
    /// Constructs a <see cref="CommandLiteralComponent"/> by tokenizing the literal.
    /// </summary>
    public CommandLiteralComponent(string Literal, bool CaseSensitive = false)
        : this(CommandCallParser.TokenizeInputCall(Literal), CaseSensitive) {
    }
    /// <summary>
    /// Converts <see cref="CaseSensitive"/> to a <see cref="StringComparison"/> used to compare literals.
    /// </summary>
    public StringComparison GetStringComparison() {
        return CaseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase;
    }
    /// <summary>
    /// Matches the tokens in the literal with the given tokens.
    /// </summary>
    public bool TryMatch(ReadOnlySpan<string> Tokens, out int TokenCount) {
        for (int Index = 0; Index < LiteralTokens.Count; Index++) {
            string LiteralToken = LiteralTokens[Index];
            string? Token = Index < Tokens.Length ? Tokens[Index] : null;

            // Token mismatch
            if (!LiteralToken.Equals(Token, GetStringComparison())) {
                TokenCount = 0;
                return false;
            }
        }
        // All tokens match
        TokenCount = LiteralTokens.Count;
        return true;
    }
}
/// <summary>
/// A component that supports any one of the choices as the component.
/// </summary>
public record CommandChoicesComponent(List<List<CommandComponent>> Choices) : CommandComponent {
    /// <summary>
    /// The component sequences that may be chosen.
    /// </summary>
    public List<List<CommandComponent>> Choices { get; set; } = Choices;
}