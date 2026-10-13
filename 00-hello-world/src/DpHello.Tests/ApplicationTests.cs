using System.Text;

namespace DialecticPlayground.Hello.Tests;

public sealed class ApplicationTests
{
    private const string Hint = "Try 'dp-hello --help' for more information.\n";

    private static (byte[] Stdout, byte[] Stderr, int Status) Run(params string[] args)
    {
        using var stdout = new MemoryStream();
        using var stderr = new MemoryStream();
        var status = Application.Run(args, stdout, stderr);
        return (stdout.ToArray(), stderr.ToArray(), status);
    }

    private static byte[] Utf8(string text) => Encoding.UTF8.GetBytes(text);

    [Theory]
    [InlineData(new string[0], "Hello, World!\n")]
    [InlineData(new[] { "--name", "Ada" }, "Hello, Ada!\n")]
    [InlineData(new[] { "-n", "Grace" }, "Hello, Grace!\n")]
    [InlineData(new[] { "--name=Linus" }, "Hello, Linus!\n")]
    [InlineData(new[] { "--count", "3" }, "Hello, World!\nHello, World!\nHello, World!\n")]
    [InlineData(new[] { "-c2", "-n", "Ken" }, "Hello, Ken!\nHello, Ken!\n")]
    [InlineData(new[] { "-n", "A", "-n", "B" }, "Hello, B!\n")]
    [InlineData(new[] { "-n", "", "-n", "B" }, "Hello, B!\n")]
    [InlineData(new[] { "--version" }, "dp-hello (dialectic-playground) 1.0.0\n")]
    public void Run_Success(string[] args, string expected)
    {
        var (stdout, stderr, status) = Run(args);
        Assert.Equal(Utf8(expected), stdout);
        Assert.Empty(stderr);
        Assert.Equal(0, status);
    }

    [Fact]
    public void Run_NonAsciiName_IsWrittenAsUtf8WithoutBom()
    {
        var (stdout, _, status) = Run("--name", "Zoë");
        Assert.Equal(0, status);
        Assert.Equal([.. "Hello, Zo"u8, 0xC3, 0xAB, .. "!\n"u8], stdout);
    }

    [Fact]
    public void Run_MaximumCount_WritesExactlyThatManyLfTerminatedLines()
    {
        var (stdout, _, status) = Run("-c", "1000");
        Assert.Equal(0, status);
        Assert.Equal(1000 * "Hello, World!\n".Length, stdout.Length);
        Assert.DoesNotContain((byte)'\r', stdout);
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("--help", "extra", "-x")]
    [InlineData("extra", "--help")]
    [InlineData("--count", "0", "--help")]
    public void Run_Help_PrintsTheManualTextExactly(params string[] args)
    {
        var (stdout, stderr, status) = Run(args);
        Assert.Equal(
            Utf8(
                "Usage: dp-hello [OPTION]...\n"
                + "Print a greeting to standard output.\n"
                + "\n"
                + "Options:\n"
                + "  -n, --name=NAME     greet NAME instead of World\n"
                + "  -c, --count=COUNT   print the greeting COUNT times (1-1000, default 1)\n"
                + "      --help          display this help and exit\n"
                + "      --version       output version information and exit\n"),
            stdout);
        Assert.Empty(stderr);
        Assert.Equal(0, status);
    }

    [Theory]
    [InlineData(new[] { "-x" }, "dp-hello: invalid option -- 'x'\n")]
    [InlineData(new[] { "-x", "--help" }, "dp-hello: invalid option -- 'x'\n")]
    [InlineData(new[] { "extra" }, "dp-hello: extra operand 'extra'\n")]
    [InlineData(new[] { "--", "-n" }, "dp-hello: extra operand '-n'\n")]
    [InlineData(new[] { "--nme", "Ada" }, "dp-hello: unrecognized option '--nme'\n")]
    [InlineData(new[] { "--name" }, "dp-hello: option '--name' requires an argument\n")]
    [InlineData(new[] { "-c" }, "dp-hello: option requires an argument -- 'c'\n")]
    [InlineData(new[] { "--version=1" }, "dp-hello: option '--version' doesn't allow an argument\n")]
    [InlineData(new[] { "--name", "" }, "dp-hello: invalid argument '' for '--name'\n")]
    [InlineData(new[] { "--count", "0" }, "dp-hello: invalid argument '0' for '--count'\n")]
    [InlineData(new[] { "-c", "abc" }, "dp-hello: invalid argument 'abc' for '-c'\n")]
    [InlineData(new[] { "-c", "1001" }, "dp-hello: invalid argument '1001' for '-c'\n")]
    [InlineData(new[] { "-c", "5", "--count=0" }, "dp-hello: invalid argument '0' for '--count'\n")]
    [InlineData(new[] { "Zoë" }, "dp-hello: extra operand 'Zoë'\n")]
    public void Run_UsageErrors_WriteTwoLinesToStderrOnly(string[] args, string firstLine)
    {
        var (stdout, stderr, status) = Run(args);
        Assert.Empty(stdout);
        Assert.Equal(Utf8(firstLine + Hint), stderr);
        Assert.Equal(2, status);
    }

    [Fact]
    public void Run_BrokenPipe_StopsSilently()
    {
        using var stderr = new MemoryStream();
        var status = Application.Run(["-c", "1000"], new BrokenPipeStream(), stderr);
        Assert.Equal(Application.BrokenPipe, status);
        Assert.Empty(stderr.ToArray());
    }

    /// <summary>A write-only stream whose reader has gone away.</summary>
    private sealed class BrokenPipeStream : Stream
    {
        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() => throw new IOException("Broken pipe");

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new IOException("Broken pipe");
    }
}
