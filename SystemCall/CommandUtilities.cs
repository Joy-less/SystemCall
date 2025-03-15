using System.Text;
using System.Buffers;
using LinkDotNet.StringBuilder;

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
    public static int FindClosingBracket(this ReadOnlySpan<char> Input, Rune OpenBracketRune, Rune CloseBracketRune, Rune? EscapeRune) {
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
    /// <inheritdoc cref="FindClosingBracket(ReadOnlySpan{char}, Rune, Rune, Rune?)"/>
    public static int FindClosingBracket(this ReadOnlySpan<char> Input, char OpenBracketChar, char CloseBracketChar, char? EscapeChar) {
        return FindClosingBracket(Input, (Rune)OpenBracketChar, (Rune)CloseBracketChar, (Rune?)EscapeChar);
    }
    /// <summary>
    /// Splits the string by the separator, ignoring escaped separators.<br/>
    /// The escape characters are removed.
    /// </summary>
    public static List<string> SplitWithEscape(this ReadOnlySpan<char> Input, Rune SeparatorRune, Rune? EscapeRune) {
        using ValueStringBuilder SegmentBuilder = new(stackalloc char[64]);
        List<string> Result = [];

        int Index = 0;
        while (Index < Input.Length) {
            // Read rune
            if (Rune.DecodeFromUtf16(Input[Index..], out Rune CurrentRune, out int CurrentCharsConsumed) is not OperationStatus.Done) {
                throw new CallSyntaxException("Invalid UTF-16 sequence");
            }
            Index += CurrentCharsConsumed;

            // Escape
            if (CurrentRune == EscapeRune) {
                // Ensure not trailing escape
                if (Index >= Input.Length) {
                    throw new CallSyntaxException("Incomplete escape sequence: `\\`");
                }
                // Read escaped rune
                if (Rune.DecodeFromUtf16(Input[Index..], out Rune EscapedRune, out int EscapedCharsConsumed) is not OperationStatus.Done) {
                    throw new CallSyntaxException("Invalid UTF-16 sequence");
                }
                Index += EscapedCharsConsumed;

                // Append escaped rune
                SegmentBuilder.Append(EscapedRune);
            }
            // Separator
            else if (CurrentRune == SeparatorRune) {
                Result.Add(SegmentBuilder.ToString());
                SegmentBuilder.Clear();
            }
            // Other
            else {
                SegmentBuilder.Append(CurrentRune);
            }
        }

        // Add last segment
        if (SegmentBuilder.Length != 0) {
            Result.Add(SegmentBuilder.ToString());
        }

        return Result;
    }
    /// <inheritdoc cref="SplitWithEscape(ReadOnlySpan{char}, Rune, Rune?)"/>
    public static List<string> SplitWithEscape(this ReadOnlySpan<char> Input, char SeparatorChar, char? EscapeChar) {
        return SplitWithEscape(Input, (Rune)SeparatorChar, (Rune?)EscapeChar);
    }
}