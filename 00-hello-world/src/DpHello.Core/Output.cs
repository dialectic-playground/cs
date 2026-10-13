using System.Text;

namespace DialecticPlayground.Hello;

/// <summary>
/// Output: writes text as UTF-8 bytes (no byte order mark) to a binary stream, so no newline
/// translation or console code page applies on any OS (CONV-03, CONV-05).
/// </summary>
public static class Output
{
    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>Writes <paramref name="text"/> once.</summary>
    /// <param name="stream">The destination stream.</param>
    /// <param name="text">The text to write.</param>
    public static void Write(Stream stream, string text) => WriteRepeated(stream, text, 1);

    /// <summary>
    /// Writes <paramref name="text"/> <paramref name="times"/> times through a buffer, encoding
    /// it only once, so memory stays bounded by the size of one line.
    /// </summary>
    /// <param name="stream">The destination stream.</param>
    /// <param name="text">The text to write.</param>
    /// <param name="times">How many times to write it.</param>
    public static void WriteRepeated(Stream stream, string text, int times)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var bytes = Utf8.GetBytes(text);
        using var buffered = new BufferedStream(stream, 64 * 1024);
        for (var i = 0; i < times; i++)
        {
            buffered.Write(bytes);
        }

        buffered.Flush();
    }
}
