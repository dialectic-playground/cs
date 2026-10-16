namespace DialecticPlayground.Hello;

/// <summary>An option value as recorded during the scan, with the option as written.</summary>
/// <param name="Option">The option as the user wrote it: <c>-n</c>, <c>--name</c>, and so on.</param>
/// <param name="Value">The raw, unvalidated value.</param>
public sealed record OptionValue(string Option, string Value);

/// <summary>The outcome of scanning the argument list (CONV-07 ... CONV-12).</summary>
public abstract record ParseOutcome
{
    private ParseOutcome()
    {
    }

    /// <summary><c>--help</c> was reached before any option syntax error.</summary>
    public sealed record HelpRequested : ParseOutcome;

    /// <summary><c>--version</c> was reached before any option syntax error.</summary>
    public sealed record VersionRequested : ParseOutcome;

    /// <summary>An option syntax error was found first.</summary>
    /// <param name="Message">The CONV-13 message without the tool-name prefix.</param>
    public sealed record SyntaxError(string Message) : ParseOutcome;

    /// <summary>The whole list was scanned without a syntax error.</summary>
    /// <param name="Name">The last <c>--name</c> / <c>-n</c> occurrence, if any (CONV-11).</param>
    /// <param name="Count">The last <c>--count</c> / <c>-c</c> occurrence, if any (CONV-11).</param>
    /// <param name="Operands">Every operand, in order; validated later (FR-09).</param>
    public sealed record Arguments(
        OptionValue? Name,
        OptionValue? Count,
        IReadOnlyList<string> Operands) : ParseOutcome;
}

/// <summary>
/// Hand-written option parser for dp-hello. It only scans: option values are recorded and
/// operands collected, and both are validated afterwards by <see cref="Validator"/> (CONV-12).
/// </summary>
public static class CommandLine
{
    private enum Valued
    {
        Name,
        Count,
    }

    /// <summary>Scans <paramref name="args"/> from left to right.</summary>
    /// <param name="args">The command-line arguments, without the program name.</param>
    /// <returns>The first of: help, version, a syntax error, or the recorded arguments.</returns>
    public static ParseOutcome Parse(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);
        OptionValue? name = null;
        OptionValue? count = null;
        var operands = new List<string>();

        void Record(Valued option, OptionValue value)
        {
            if (option == Valued.Name)
            {
                name = value;
            }
            else
            {
                count = value;
            }
        }

        var index = 0;
        while (index < args.Count)
        {
            var token = args[index++];
            if (token == "--")
            {
                for (; index < args.Count; index++)
                {
                    operands.Add(args[index]);
                }

                break;
            }

            if (token.StartsWith("--", StringComparison.Ordinal))
            {
                var equals = token.IndexOf('=', StringComparison.Ordinal);
                var longName = equals < 0 ? token[2..] : token[2..equals];
                var inline = equals < 0 ? null : token[(equals + 1)..];
                switch (longName)
                {
                    case "help" or "version" when inline is not null:
                        return new ParseOutcome.SyntaxError(Messages.OptionDoesNotAllowArgument(longName));
                    case "help":
                        return new ParseOutcome.HelpRequested();
                    case "version":
                        return new ParseOutcome.VersionRequested();
                    case "name" or "count":
                        string value;
                        if (inline is not null)
                        {
                            value = inline;
                        }
                        else if (index < args.Count)
                        {
                            value = args[index++];
                        }
                        else
                        {
                            return new ParseOutcome.SyntaxError(Messages.LongOptionRequiresArgument(longName));
                        }
                        Record(
                            longName == "name" ? Valued.Name : Valued.Count,
                            new OptionValue("--" + longName, value));
                        break;
                    default:
                        return new ParseOutcome.SyntaxError(Messages.UnrecognizedOption(token));
                }

                continue;
            }

            if (token.Length > 1 && token[0] == '-')
            {
                // A short option: -n and -c both take a value, which is the rest of the token
                // or, when that is empty, the next token (CONV-07).
                var letter = token[1];
                Valued option;
                switch (letter)
                {
                    case 'n':
                        option = Valued.Name;
                        break;
                    case 'c':
                        option = Valued.Count;
                        break;
                    default:
                        // Quote the whole character, even outside the BMP (CONV-05).
                        var length = char.IsSurrogatePair(token, 1) ? 2 : 1;
                        return new ParseOutcome.SyntaxError(Messages.InvalidOption(token.Substring(1, length)));
                }

                string value;
                if (token.Length > 2)
                {
                    value = token[2..];
                }
                else if (index < args.Count)
                {
                    value = args[index++];
                }
                else
                {
                    return new ParseOutcome.SyntaxError(Messages.ShortOptionRequiresArgument(letter));
                }

                Record(option, new OptionValue("-" + letter, value));
                continue;
            }

            operands.Add(token);
        }

        return new ParseOutcome.Arguments(name, count, operands);
    }
}
