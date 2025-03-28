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
    public static int FindClosingBracket(this ReadOnlySpan<char> Input, char OpenBracketChar, char CloseBracketChar, char? EscapeChar) {
        int Depth = 1;
        for (int Index = 0; Index < Input.Length; Index++) {
            char Char = Input[Index];

            // Open bracket
            if (Char == OpenBracketChar) {
                Depth++;
            }
            // Close bracket
            else if (Char == CloseBracketChar) {
                Depth--;
                if (Depth == 0) {
                    return Index;
                }
            }
            // Escape
            else if (Char == EscapeChar) {
                // Ensure not trailing escape
                if (Index + 1 >= Input.Length) {
                    throw new CallSyntaxException("Incomplete escape sequence: `\\`");
                }

                // Read escaped character
                Index++;
            }
        }
        // Closing bracket not found
        return -1;
    }
    /// <summary>
    /// Splits the string by the separator, ignoring escaped separators.<br/>
    /// The escape characters are removed.
    /// </summary>
    public static List<string> SplitWithEscape(this ReadOnlySpan<char> Input, char SeparatorChar, char? EscapeChar, StringSplitOptions Options = StringSplitOptions.None) {
        List<string> Result = [];

        ValueStringBuilder SegmentBuilder = new(stackalloc char[64]);
        using ValueStringBuilder ReadOnlySegmentBuilder = SegmentBuilder; // Can't pass using variables by-ref

        bool TrySubmitSegment(ref ValueStringBuilder SegmentBuilder) {
            if (Options.HasFlag(StringSplitOptions.TrimEntries)) {
                SegmentBuilder.Trim();
            }

            if (Options.HasFlag(StringSplitOptions.RemoveEmptyEntries)) {
                if (SegmentBuilder.Length == 0) {
                    return false;
                }
            }

            Result.Add(SegmentBuilder.ToString());
            SegmentBuilder.Clear();
            return true;
        }
        
        for (int Index = 0; Index < Input.Length; Index++) {
            char Char = Input[Index];

            // Escape
            if (Char == EscapeChar) {
                // Ensure not trailing escape
                if (Index + 1 >= Input.Length) {
                    throw new CallSyntaxException("Incomplete escape sequence: `\\`");
                }

                // Read escaped character
                Index++;
                char EscapedChar = Input[Index];

                // Append escaped character
                SegmentBuilder.Append(EscapedChar);
            }
            // Separator
            else if (Char == SeparatorChar) {
                TrySubmitSegment(ref SegmentBuilder);
            }
            // Other
            else {
                SegmentBuilder.Append(Char);
            }
        }

        // Add last segment
        TrySubmitSegment(ref SegmentBuilder);

        return Result;
    }
}