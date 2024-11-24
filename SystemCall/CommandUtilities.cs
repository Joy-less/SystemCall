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
            '0' => '\0', // Null
            'a' => '\a', // Alert
            'b' => '\b', // Backspace
            'e' => '\e', // Escape
            'f' => '\f', // Form feed
            'n' => '\n', // Newline
            'r' => '\r', // Carriage return
            't' => '\t', // Horizontal tab
            'v' => '\v', // Vertical tab
            _ => Char, // Unrecognized
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