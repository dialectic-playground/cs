namespace DialecticPlayground.Hello.Tests;

public sealed class CommandLineTests
{
    private static ParseOutcome.Arguments Scan(params string[] args) =>
        Assert.IsType<ParseOutcome.Arguments>(CommandLine.Parse(args));

    private static string SyntaxError(params string[] args) =>
        Assert.IsType<ParseOutcome.SyntaxError>(CommandLine.Parse(args)).Message;

    [Fact]
    public void Parse_NoArguments_RecordsNothing()
    {
        var arguments = Scan();
        Assert.Null(arguments.Name);
        Assert.Null(arguments.Count);
        Assert.Empty(arguments.Operands);
    }

    [Theory]
    [InlineData(new[] { "-n", "Ken" }, "-n", "Ken")]
    [InlineData(new[] { "-nKen" }, "-n", "Ken")]
    [InlineData(new[] { "--name", "Ken" }, "--name", "Ken")]
    [InlineData(new[] { "--name=Ken" }, "--name", "Ken")]
    [InlineData(new[] { "--name=" }, "--name", "")]
    [InlineData(new[] { "--name=a=b" }, "--name", "a=b")]
    [InlineData(new[] { "--name=-x" }, "--name", "-x")]
    [InlineData(new[] { "--name", "-x" }, "--name", "-x")]
    [InlineData(new[] { "-n-x" }, "-n", "-x")]
    [InlineData(new[] { "-n", "--help" }, "-n", "--help")]
    [InlineData(new[] { "-n", "" }, "-n", "")]
    public void Parse_Name_AllSpellings(string[] args, string option, string value) =>
        Assert.Equal(new OptionValue(option, value), Scan(args).Name);

    [Theory]
    [InlineData(new[] { "-c", "3" }, "-c", "3")]
    [InlineData(new[] { "-c3" }, "-c", "3")]
    [InlineData(new[] { "--count", "3" }, "--count", "3")]
    [InlineData(new[] { "--count=3" }, "--count", "3")]
    [InlineData(new[] { "-c", "abc" }, "-c", "abc")]
    public void Parse_Count_RecordsRawValue(string[] args, string option, string value) =>
        Assert.Equal(new OptionValue(option, value), Scan(args).Count);

    [Fact]
    public void Parse_BundledValues_ConsumeRestOfToken()
    {
        var arguments = Scan("-c2", "-nKen");
        Assert.Equal(new OptionValue("-c", "2"), arguments.Count);
        Assert.Equal(new OptionValue("-n", "Ken"), arguments.Name);
        Assert.Equal(new OptionValue("-c", "n5"), Scan("-cn5").Count);
    }

    [Fact]
    public void Parse_RepeatedOptions_LastWins()
    {
        var arguments = Scan("-n", "A", "--name=B", "-c", "0", "--count", "2");
        Assert.Equal(new OptionValue("--name", "B"), arguments.Name);
        Assert.Equal(new OptionValue("--count", "2"), arguments.Count);
        Assert.Equal(new OptionValue("-n", ""), Scan("--name", "A", "-n", "").Name);
    }

    [Fact]
    public void Parse_Operands_AreCollectedInterleaved() =>
        Assert.Equal(["a", "-", "b"], Scan("a", "-n", "X", "-", "b").Operands);

    [Fact]
    public void Parse_Terminator_MakesEverythingAfterItOperands()
    {
        var arguments = Scan("-n", "A", "--", "-n", "--help", "--");
        Assert.Equal(["-n", "--help", "--"], arguments.Operands);
        Assert.Equal(new OptionValue("-n", "A"), arguments.Name);
        Assert.Empty(Scan("--").Operands);
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("--help", "extra", "-x")]
    [InlineData("extra", "--help")]
    [InlineData("--count", "0", "--help")]
    [InlineData("-n", "", "--help")]
    [InlineData("--help", "--version")]
    public void Parse_Help_WinsWhenReachedFirst(params string[] args) =>
        Assert.IsType<ParseOutcome.HelpRequested>(CommandLine.Parse(args));

    [Theory]
    [InlineData("--version")]
    [InlineData("--version", "--help")]
    [InlineData("extra", "--version", "-x")]
    public void Parse_Version_WinsWhenReachedFirst(params string[] args) =>
        Assert.IsType<ParseOutcome.VersionRequested>(CommandLine.Parse(args));

    [Theory]
    [InlineData(new[] { "-x" }, "invalid option -- 'x'")]
    [InlineData(new[] { "-x", "--help" }, "invalid option -- 'x'")]
    [InlineData(new[] { "-h" }, "invalid option -- 'h'")]
    [InlineData(new[] { "-é" }, "invalid option -- 'é'")]
    [InlineData(new[] { "-😀" }, "invalid option -- '😀'")]
    [InlineData(new[] { "--nme", "Ada" }, "unrecognized option '--nme'")]
    [InlineData(new[] { "--nam" }, "unrecognized option '--nam'")]
    [InlineData(new[] { "--he" }, "unrecognized option '--he'")]
    [InlineData(new[] { "--x=1" }, "unrecognized option '--x=1'")]
    [InlineData(new[] { "--=1" }, "unrecognized option '--=1'")]
    [InlineData(new[] { "-n" }, "option requires an argument -- 'n'")]
    [InlineData(new[] { "-c" }, "option requires an argument -- 'c'")]
    [InlineData(new[] { "--name" }, "option '--name' requires an argument")]
    [InlineData(new[] { "--count" }, "option '--count' requires an argument")]
    [InlineData(new[] { "--help=1" }, "option '--help' doesn't allow an argument")]
    [InlineData(new[] { "--version=" }, "option '--version' doesn't allow an argument")]
    [InlineData(new[] { "extra", "--help=x", "--version" }, "option '--help' doesn't allow an argument")]
    public void Parse_SyntaxErrors_UseConv13Messages(string[] args, string message) =>
        Assert.Equal(message, SyntaxError(args));
}
