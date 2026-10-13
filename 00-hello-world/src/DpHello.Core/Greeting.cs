namespace DialecticPlayground.Hello;

/// <summary>Greeting construction (FR-01, FR-02; NFR-05).</summary>
public static class Greeting
{
    /// <summary>The name greeted when <c>--name</c> is not given (FR-01).</summary>
    public const string DefaultName = "World";

    /// <summary>
    /// Builds one greeting line. Pure: the result depends only on <paramref name="name"/>,
    /// which is used verbatim.
    /// </summary>
    /// <param name="name">The name to greet.</param>
    /// <returns><c>Hello, NAME!</c> followed by a single LF.</returns>
    public static string Line(string name) => $"Hello, {name}!\n";
}
