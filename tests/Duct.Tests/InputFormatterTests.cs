using Duct.Controls.Formatting;
using Xunit;

namespace Duct.Tests;

public class InputFormatterTests
{
    // ════════════════════════════════════════════════════════════════
    //  PhoneUS
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void PhoneUS_Formats_Full_Number()
    {
        var f = InputFormatter.PhoneUS;
        var result = f.Format("5551234567", 10);
        Assert.Equal("(555) 123-4567", result.Output);
    }

    [Fact]
    public void PhoneUS_Formats_Partial()
    {
        var f = InputFormatter.PhoneUS;
        Assert.Equal("(555", f.Format("555", 3).Output);
        Assert.Equal("(555) 12", f.Format("55512", 5).Output);
    }

    [Fact]
    public void PhoneUS_Parses_Back()
    {
        var f = InputFormatter.PhoneUS;
        Assert.Equal("5551234567", f.Parse("(555) 123-4567"));
    }

    [Fact]
    public void PhoneUS_Strips_NonDigits()
    {
        var f = InputFormatter.PhoneUS;
        var result = f.Format("(555) 123-4567", 14);
        Assert.Equal("(555) 123-4567", result.Output);
    }

    // ════════════════════════════════════════════════════════════════
    //  PhoneIntl
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void PhoneIntl_Adds_Country_Code()
    {
        var f = InputFormatter.PhoneIntl("+44");
        var result = f.Format("7911123456", 10);
        Assert.Equal("+44 7911123456", result.Output);
    }

    [Fact]
    public void PhoneIntl_Parse_Strips_Code()
    {
        var f = InputFormatter.PhoneIntl("+1");
        Assert.Equal("5551234567", f.Parse("+1 5551234567"));
    }

    // ════════════════════════════════════════════════════════════════
    //  Currency
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Currency_Formats_With_Symbol_And_Commas()
    {
        var f = InputFormatter.Currency("$");
        var result = f.Format("1234567", 7);
        Assert.Equal("$1,234,567", result.Output);
    }

    [Fact]
    public void Currency_Handles_Decimal()
    {
        var f = InputFormatter.Currency("$");
        var result = f.Format("1234.56", 7);
        Assert.Equal("$1,234.56", result.Output);
    }

    [Fact]
    public void Currency_Limits_Decimal_To_2()
    {
        var f = InputFormatter.Currency("$");
        var result = f.Format("123.456", 7);
        Assert.Equal("$123.45", result.Output);
    }

    [Fact]
    public void Currency_Parse_Strips_Symbol_And_Commas()
    {
        var f = InputFormatter.Currency("$");
        Assert.Equal("1234.56", f.Parse("$1,234.56"));
    }

    [Fact]
    public void Currency_Custom_Symbol()
    {
        var f = InputFormatter.Currency("€");
        var result = f.Format("500", 3);
        Assert.Equal("€500", result.Output);
    }

    // ════════════════════════════════════════════════════════════════
    //  UpperCase
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void UpperCase_Converts()
    {
        var result = InputFormatter.UpperCase.Format("hello World", 5);
        Assert.Equal("HELLO WORLD", result.Output);
        Assert.Equal(5, result.CursorPos); // cursor preserved
    }

    // ════════════════════════════════════════════════════════════════
    //  LowerCase
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void LowerCase_Converts()
    {
        var result = InputFormatter.LowerCase.Format("Hello WORLD", 5);
        Assert.Equal("hello world", result.Output);
    }

    // ════════════════════════════════════════════════════════════════
    //  TitleCase
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void TitleCase_Converts()
    {
        var result = InputFormatter.TitleCase.Format("hello world test", 5);
        Assert.Equal("Hello World Test", result.Output);
    }

    // ════════════════════════════════════════════════════════════════
    //  TrimWhitespace
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void TrimWhitespace_Strips_Edges()
    {
        var result = InputFormatter.TrimWhitespace.Format("  hello  ", 4);
        Assert.Equal("hello", result.Output);
        Assert.Equal(4, result.CursorPos);
    }

    [Fact]
    public void TrimWhitespace_Adjusts_Cursor_If_Past_End()
    {
        var result = InputFormatter.TrimWhitespace.Format("  hi  ", 6);
        Assert.Equal("hi", result.Output);
        Assert.Equal(2, result.CursorPos);
    }

    // ════════════════════════════════════════════════════════════════
    //  MaxLength
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void MaxLength_Truncates()
    {
        var f = InputFormatter.MaxLength(5);
        var result = f.Format("hello world", 8);
        Assert.Equal("hello", result.Output);
        Assert.Equal(5, result.CursorPos);
    }

    [Fact]
    public void MaxLength_NoOp_When_Shorter()
    {
        var f = InputFormatter.MaxLength(10);
        var result = f.Format("hello", 5);
        Assert.Equal("hello", result.Output);
    }

    // ════════════════════════════════════════════════════════════════
    //  AllowOnly
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void AllowOnly_Digits()
    {
        var f = InputFormatter.AllowOnly(@"\d");
        var result = f.Format("a1b2c3", 6);
        Assert.Equal("123", result.Output);
    }

    [Fact]
    public void AllowOnly_Letters()
    {
        var f = InputFormatter.AllowOnly(@"[a-zA-Z]");
        var result = f.Format("a1b2c3", 6);
        Assert.Equal("abc", result.Output);
    }

    // ════════════════════════════════════════════════════════════════
    //  DenyOnly
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void DenyOnly_Removes_Matching()
    {
        var f = InputFormatter.DenyOnly(@"[0-9]");
        var result = f.Format("a1b2c3", 6);
        Assert.Equal("abc", result.Output);
    }

    [Fact]
    public void DenyOnly_Removes_Spaces()
    {
        var f = InputFormatter.DenyOnly(@"\s");
        var result = f.Format("hello world", 11);
        Assert.Equal("helloworld", result.Output);
    }

    // ════════════════════════════════════════════════════════════════
    //  Custom
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Custom_Format_And_Parse()
    {
        var f = InputFormatter.Custom(
            format: s => s.Replace(" ", "-"),
            parse: s => s.Replace("-", " "));

        Assert.Equal("hello-world", f.Format("hello world", 5).Output);
        Assert.Equal("hello world", f.Parse("hello-world"));
    }

    // ════════════════════════════════════════════════════════════════
    //  Pipeline
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Pipeline_Chains_Formatters()
    {
        var pipeline = new FormatterPipeline(
            InputFormatter.TrimWhitespace,
            InputFormatter.UpperCase);

        var result = pipeline.Format("  hello  ", 5);
        Assert.Equal("HELLO", result.Output);
    }

    [Fact]
    public void Pipeline_Multiple_Formatters()
    {
        var pipeline = new FormatterPipeline(
            InputFormatter.AllowOnly(@"\d"),
            InputFormatter.MaxLength(5));

        var result = pipeline.Format("abc123def456", 12);
        Assert.Equal("12345", result.Output);
    }

    [Fact]
    public void Pipeline_Parse_Reverses()
    {
        var pipeline = new FormatterPipeline(
            InputFormatter.Currency("$"));

        Assert.Equal("1234.56", pipeline.Parse("$1,234.56"));
    }

    // ════════════════════════════════════════════════════════════════
    //  Cursor position
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Cursor_Position_Preserved_In_UpperCase()
    {
        var result = InputFormatter.UpperCase.Format("hello", 3);
        Assert.Equal("HELLO", result.Output);
        Assert.Equal(3, result.CursorPos);
    }

    [Fact]
    public void Cursor_Position_Adjusted_In_MaxLength()
    {
        var f = InputFormatter.MaxLength(3);
        var result = f.Format("hello", 5);
        Assert.Equal("hel", result.Output);
        Assert.Equal(3, result.CursorPos);
    }
}
