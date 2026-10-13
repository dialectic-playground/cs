namespace DialecticPlayground.Hello;

/// <summary>
/// The dp-hello program as a function of its arguments and output streams: scan, validate,
/// build the greeting, write (NFR-04). It never reads input, files, or the environment
/// (NFR-07, CONV-26).
/// </summary>
public static class Application
{
    /// <summary>Exit status of a successful run (SRS-00 section 3.7).</summary>
    public const int Success = 0;

    /// <summary>Exit status of a usage error (CONV-14).</summary>
    public const int UsageError = 2;

    /// <summary>
    /// Exit status when standard output is closed by the reader (CONV-25 leaves it
    /// unspecified); 141 is the conventional 128 + SIGPIPE.
    /// </summary>
    public const int BrokenPipe = 141;

    /// <summary>Runs dp-hello.</summary>
    /// <param name="args">The command-line arguments, without the program name.</param>
    /// <param name="stdout">Standard output, as a binary stream.</param>
    /// <param name="stderr">Standard error, as a binary stream.</param>
    /// <returns>The exit status.</returns>
    public static int Run(IReadOnlyList<string> args, Stream stdout, Stream stderr)
    {
        var (text, times, error) = CommandLine.Parse(args) switch
        {
            ParseOutcome.HelpRequested => (Messages.HelpText, 1, null),
            ParseOutcome.VersionRequested => (Messages.VersionLine, 1, null),
            ParseOutcome.SyntaxError syntax => (string.Empty, 0, syntax.Message),
            ParseOutcome.Arguments arguments => Validator.Validate(arguments) switch
            {
                ValidationOutcome.Valid { Settings: var settings } =>
                    (Greeting.Line(settings.Name), settings.Count, (string?)null),
                ValidationOutcome.Invalid invalid => (string.Empty, 0, invalid.Message),
                _ => throw new InvalidOperationException("unreachable validation outcome"),
            },
            _ => throw new InvalidOperationException("unreachable parse outcome"),
        };

        // Usage errors are detected before anything is written to standard output (FR-11).
        if (error is not null)
        {
            return Report(stderr, error);
        }

        try
        {
            Output.WriteRepeated(stdout, text, times);
        }
        catch (IOException)
        {
            // The reader went away (EPIPE): stop promptly and silently (CONV-25).
            return BrokenPipe;
        }

        return Success;
    }

    private static int Report(Stream stderr, string message)
    {
        try
        {
            Output.Write(stderr, Messages.UsageDiagnostic(message));
        }
        catch (IOException)
        {
            // Standard error is gone too; the exit status still reports the usage error.
        }

        return UsageError;
    }
}
