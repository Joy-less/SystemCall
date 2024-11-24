namespace SystemCall;

/// <summary>
/// Utility functions for the System Call library.
/// </summary>
internal static class CommandUtilities {
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