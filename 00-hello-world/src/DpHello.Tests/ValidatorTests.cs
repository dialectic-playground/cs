namespace DialecticPlayground.Hello.Tests;

public sealed class ValidatorTests
{
    private static ParseOutcome.Arguments Arguments(
        OptionValue? name = null, OptionValue? count = null, params string[] operands) =>
        new(name, count, operands);

    private static Settings Valid(ParseOutcome.Arguments arguments) =>
        Assert.IsType<ValidationOutcome.Valid>(Validator.Validate(arguments)).Settings;

    private static string Invalid(ParseOutcome.Arguments arguments) =>
        Assert.IsType<ValidationOutcome.Invalid>(Validator.Validate(arguments)).Message;

    [Fact]
    public void Validate_Defaults() => Assert.Equal(new Settings("World", 1), Valid(Arguments()));

    [Theory]
    [InlineData("1", 1)]
    [InlineData("2", 2)]
    [InlineData("999", 999)]
    [InlineData("1000", 1000)]
    [InlineData("007", 7)]
    [InlineData("0001000", 1000)]
    [InlineData("00000000000000000000001", 1)]
    public void Validate_CountWithinRange(string text, int expected) =>
        Assert.Equal(expected, Valid(Arguments(count: new OptionValue("-c", text))).Count);

    [Theory]
    [InlineData("0")]
    [InlineData("000")]
    [InlineData("1001")]
    [InlineData("99999999999999999999")]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("-1")]
    [InlineData("+1")]
    [InlineData(" 1")]
    [InlineData("1 ")]
    [InlineData("1.0")]
    [InlineData("1e3")]
    [InlineData("٣")] // Arabic-Indic digit three: a Unicode digit, but not ASCII
    [InlineData("３")] // fullwidth digit three
    public void Validate_InvalidCount(string text)
    {
        Assert.Equal($"invalid argument '{text}' for '-c'", Invalid(Arguments(count: new OptionValue("-c", text))));
        Assert.Equal(
            $"invalid argument '{text}' for '--count'",
            Invalid(Arguments(count: new OptionValue("--count", text))));
    }

    [Fact]
    public void Validate_Name()
    {
        Assert.Equal("Zoë", Valid(Arguments(new OptionValue("-n", "Zoë"))).Name);
        Assert.Equal("invalid argument '' for '-n'", Invalid(Arguments(new OptionValue("-n", ""))));
        Assert.Equal("invalid argument '' for '--name'", Invalid(Arguments(new OptionValue("--name", ""))));
    }

    [Fact]
    public void Validate_Operands_ReportTheFirstBeforeValueErrors() =>
        Assert.Equal(
            "extra operand 'a'",
            Invalid(Arguments(new OptionValue("-n", ""), new OptionValue("-c", "0"), "a", "b")));

    [Fact]
    public void Validate_EmptyName_ReportedBeforeInvalidCount() =>
        Assert.Equal(
            "invalid argument '' for '--name'",
            Invalid(Arguments(new OptionValue("--name", ""), new OptionValue("-c", "0"))));

    [Fact]
    public void TryParseCount_RejectsNull() =>
        Assert.Throws<ArgumentNullException>(() => Validator.TryParseCount(null!, out _));
}
