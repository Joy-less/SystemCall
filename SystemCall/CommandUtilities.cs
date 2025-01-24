using System.Text;

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
    public static int FindClosingBracket(string String, int StartIndex, Rune OpenBracketRune, Rune CloseBracketRune, Rune? EscapeRune) {
        int Depth = 1;

        int Index = StartIndex;
        while (Index < String.Length) {
            // Read rune
            Rune Rune = Rune.GetRuneAt(String, Index);
            Index += Rune.Utf16SequenceLength;

            // Open bracket
            if (Rune == OpenBracketRune) {
                Depth++;
            }
            // Close bracket
            else if (Rune == CloseBracketRune) {
                Depth--;
                if (Depth == 0) {
                    return Index - Rune.Utf16SequenceLength;
                }
            }
            // Escape
            else if (Rune == EscapeRune) {
                // Ensure not trailing escape
                if (Index >= String.Length) {
                    throw new CallSyntaxException("Incomplete escape sequence: `\\`");
                }
                // Read escaped rune
                Rune EscapedRune = Rune.GetRuneAt(String, Index);
                Index += EscapedRune.Utf16SequenceLength;
            }
        }

        return -1;
    }
}