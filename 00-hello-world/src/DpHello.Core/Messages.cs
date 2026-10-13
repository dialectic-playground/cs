namespace DialecticPlayground.Hello;

/// <summary>
/// The fixed texts of dp-hello: help, version, and usage diagnostics (SRS-00 sections 3 and 6,
/// CONV-01, CONV-02, CONV-13, CONV-14, CONV-23). Every line ends in a single LF (CONV-03).
/// </summary>
public static class Messages
{
    /// <summary>The canonical tool name used in every message (CONV-01).</summary>
    public const string ToolName = "dp-hello";

    /// <summary>The help text, byte-for-byte as in the user manual (FR-07, CONV-23).</summary>
    public const string HelpText =
        "Usage: dp-hello [OPTION]...\n"
        + "Print a greeting to standard output.\n"
        + "\n"
        + "Options:\n"
        + "  -n, --name=NAME     greet NAME instead of World\n"
        + "  -c, --count=COUNT   print the greeting COUNT times (1-1000, default 1)\n"
        + "      --help          display this help and exit\n"
        + "      --version       output version information and exit\n";

    /// <summary>The version line (FR-08, CONV-02).</summary>
    public const string VersionLine = "dp-hello (dialectic-playground) 1.0.0\n";

    /// <summary>
    /// Formats a usage error as written to standard error: the diagnostic line followed by the
    /// CONV-14 hint line.
    /// </summary>
    /// <param name="message">The CONV-13 message without the tool-name prefix.</param>
    /// <returns>Both lines, each terminated by LF.</returns>
    public static string UsageDiagnostic(string message) =>
        $"{ToolName}: {message}\nTry '{ToolName} --help' for more information.\n";

    /// <summary>CONV-13: unknown short option.</summary>
    /// <param name="letter">The unknown option character (one or two UTF-16 code units).</param>
    /// <returns>The message without the tool-name prefix.</returns>
    public static string InvalidOption(string letter) => $"invalid option -- '{letter}'";

    /// <summary>CONV-13: unknown long option, quoting the whole token as given.</summary>
    /// <param name="token">The token, including any <c>=value</c> part.</param>
    /// <returns>The message without the tool-name prefix.</returns>
    public static string UnrecognizedOption(string token) => $"unrecognized option '{token}'";

    /// <summary>CONV-13: short option missing its argument.</summary>
    /// <param name="letter">The option letter.</param>
    /// <returns>The message without the tool-name prefix.</returns>
    public static string ShortOptionRequiresArgument(char letter) =>
        $"option requires an argument -- '{letter}'";

    /// <summary>CONV-13: long option missing its argument.</summary>
    /// <param name="name">The long option name without the leading dashes.</param>
    /// <returns>The message without the tool-name prefix.</returns>
    public static string LongOptionRequiresArgument(string name) =>
        $"option '--{name}' requires an argument";

    /// <summary>CONV-13: <c>=value</c> given to a long option that takes no argument.</summary>
    /// <param name="name">The long option name without the leading dashes.</param>
    /// <returns>The message without the tool-name prefix.</returns>
    public static string OptionDoesNotAllowArgument(string name) =>
        $"option '--{name}' doesn't allow an argument";

    /// <summary>CONV-13 and CONV-15: invalid option value, quoted exactly as given.</summary>
    /// <param name="value">The rejected value.</param>
    /// <param name="option">The option as the user wrote it, for example <c>-n</c>.</param>
    /// <returns>The message without the tool-name prefix.</returns>
    public static string InvalidArgument(string value, string option) =>
        $"invalid argument '{value}' for '{option}'";

    /// <summary>CONV-13: the first surplus operand.</summary>
    /// <param name="operand">The operand as given.</param>
    /// <returns>The message without the tool-name prefix.</returns>
    public static string ExtraOperand(string operand) => $"extra operand '{operand}'";
}
