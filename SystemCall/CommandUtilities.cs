namespace SystemCall;

/// <summary>
/// Utility functions for the System Call library.
/// </summary>
public static class CommandUtilities {
    /// <summary>
    /// Parses the character as an escape sequence (e.g. 'n' returns '\n').
    /// </summary>
    /// <remarks>Only supports single-character escape sequences.</remarks>
    public static char EscapeCharacter(char Char) {
        return Char switch {
            // Null
            '0' => '\0',
            // Alert
            'a' => '\a',
            // Backspace
            'b' => '\b',
            // Escape
            'e' => '\e',
            // Form feed
            'f' => '\f',
            // Newline
            'n' => '\n',
            // Carriage return
            'r' => '\r',
            // Horizontal tab
            't' => '\t',
            // Vertical tab
            'v' => '\v',
            // Unrecognized
            _ => Char,
        };
    }
    /// <summary>
    /// Returns the index of the bracket that closes the opening bracket.
    /// <br/>
    /// For example:
    /// <br/>
    /// <code>
    /// ([a, (b + c), d] - e) + f
    /// ^                   ^
    /// </code>
    /// </summary>
    public static int FindClosingBracket(string String, int OpenIndex, char OpenChar, char CloseChar) {
        int Depth = 1;
        for (int Index = OpenIndex + 1; Index < String.Length; Index++) {
            char Char = String[Index];

            if (Char == OpenChar) {
                Depth++;
            }
            else if (Char == CloseChar) {
                Depth--;
                if (Depth == 0) {
                    return Index;
                }
            }
        }
        return -1;
    }
}