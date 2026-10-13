namespace DialecticPlayground.Hello.Tests;

public sealed class GreetingTests
{
    [Fact]
    public void Line_DefaultName_IsTheClassicGreeting() =>
        Assert.Equal("Hello, World!\n", Greeting.Line(Greeting.DefaultName));

    [Theory]
    [InlineData("Ada", "Hello, Ada!\n")]
    [InlineData("Ada Lovelace", "Hello, Ada Lovelace!\n")]
    [InlineData("Zoë", "Hello, Zoë!\n")]
    [InlineData("-x", "Hello, -x!\n")]
    [InlineData("a=b", "Hello, a=b!\n")]
    [InlineData("😀", "Hello, 😀!\n")]
    public void Line_UsesTheNameVerbatim(string name, string expected) =>
        Assert.Equal(expected, Greeting.Line(name));

    [Fact]
    public void Line_IsPure() => Assert.Equal(Greeting.Line("Grace"), Greeting.Line("Grace"));
}
