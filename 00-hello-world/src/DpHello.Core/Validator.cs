namespace DialecticPlayground.Hello;

/// <summary>Validated settings for one run.</summary>
/// <param name="Name">The name to greet; never empty.</param>
/// <param name="Count">How many greeting lines to print, 1 to 1000.</param>
public sealed record Settings(string Name, int Count);

/// <summary>The outcome of validating the scanned arguments.</summary>
public abstract record ValidationOutcome
{
    private ValidationOutcome()
    {
    }

    /// <summary>The arguments are valid.</summary>
    /// <param name="Settings">The settings to run with.</param>
    public sealed record Valid(Settings Settings) : ValidationOutcome;

    /// <summary>The arguments are invalid.</summary>
    /// <param name="Message">The CONV-13 message without the tool-name prefix.</param>
    public sealed record Invalid(string Message) : ValidationOutcome;
}

/// <summary>
/// Validation of operands and option values, after the scan (CONV-12): operands first
/// (FR-09), then the winning name (FR-03), then the winning count (FR-04, FR-05).
/// </summary>
public static class Validator
{
    /// <summary>The smallest accepted COUNT.</summary>
    public const int MinimumCount = 1;

    /// <summary>The largest accepted COUNT.</summary>
    public const int MaximumCount = 1000;

    /// <summary>Validates the recorded arguments.</summary>
    /// <param name="arguments">The result of a successful scan.</param>
    /// <returns>The settings, or the first usage error.</returns>
    public static ValidationOutcome Validate(ParseOutcome.Arguments arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        if (arguments.Operands.Count > 0)
        {
            return new ValidationOutcome.Invalid(Messages.ExtraOperand(arguments.Operands[0]));
        }

        var name = arguments.Name?.Value ?? Greeting.DefaultName;
        if (name.Length == 0)
        {
            return new ValidationOutcome.Invalid(Messages.InvalidArgument(name, arguments.Name!.Option));
        }

        var count = MinimumCount;
        if (arguments.Count is { } given)
        {
            if (!TryParseCount(given.Value, out count))
            {
                return new ValidationOutcome.Invalid(Messages.InvalidArgument(given.Value, given.Option));
            }
        }

        return new ValidationOutcome.Valid(new Settings(name, count));
    }

    /// <summary>
    /// Parses COUNT: ASCII decimal digits only (leading zeros allowed), value 1 to 1000 (FR-04).
    /// </summary>
    /// <param name="text">The raw value.</param>
    /// <param name="count">The parsed value when the result is <see langword="true"/>.</param>
    /// <returns>Whether <paramref name="text"/> is a valid COUNT.</returns>
    public static bool TryParseCount(string text, out int count)
    {
        ArgumentNullException.ThrowIfNull(text);
        count = 0;
        if (text.Length == 0)
        {
            return false;
        }

        foreach (var character in text)
        {
            if (!char.IsAsciiDigit(character))
            {
                return false;
            }

            // Saturate instead of overflowing: any value above the maximum is rejected anyway.
            count = Math.Min((count * 10) + (character - '0'), MaximumCount + 1);
        }

        return count is >= MinimumCount and <= MaximumCount;
    }
}
