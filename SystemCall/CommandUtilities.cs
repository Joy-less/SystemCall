using System.Buffers;
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
    ///  ^                  ^
    /// </code>
    /// </summary>
    public static int FindClosingBracket(ReadOnlySpan<char> Input, Rune OpenBracketRune, Rune CloseBracketRune, Rune? EscapeRune) {
        int Depth = 1;
        int Index = 0;
        while (Index < Input.Length) {
            // Read rune
            if (Rune.DecodeFromUtf16(Input[Index..], out Rune CurrentRune, out int CurrentCharsConsumed) is not OperationStatus.Done) {
                throw new CallSyntaxException("Invalid UTF-16 sequence");
            }
            Index += CurrentCharsConsumed;

            // Open bracket
            if (CurrentRune == OpenBracketRune) {
                Depth++;
            }
            // Close bracket
            else if (CurrentRune == CloseBracketRune) {
                Depth--;
                if (Depth == 0) {
                    return Index - CurrentCharsConsumed;
                }
            }
            // Escape
            else if (CurrentRune == EscapeRune) {
                // Ensure not trailing escape
                if (Index >= Input.Length) {
                    throw new CallSyntaxException("Incomplete escape sequence: `\\`");
                }
                // Read escaped rune
                if (Rune.DecodeFromUtf16(Input[Index..], out Rune _, out int EscapedCharsConsumed) is not OperationStatus.Done) {
                    throw new CallSyntaxException("Invalid UTF-16 sequence");
                }
                Index += EscapedCharsConsumed;
            }
        }
        // Closing bracket not found
        return -1;
    }
}