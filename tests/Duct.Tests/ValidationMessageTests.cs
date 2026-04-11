using Duct.Validation;
using Xunit;

namespace Duct.Tests;

public class ValidationMessageTests
{
    [Fact]
    public void Default_Severity_Is_Error()
    {
        var msg = new ValidationMessage("email", "Required");
        Assert.Equal(Severity.Error, msg.Severity);
    }

    [Fact]
    public void Default_Code_Is_Null()
    {
        var msg = new ValidationMessage("email", "Required");
        Assert.Null(msg.Code);
    }

    [Fact]
    public void Creates_With_All_Properties()
    {
        var msg = new ValidationMessage("name", "Too short", Severity.Warning, "MIN_LENGTH");
        Assert.Equal("name", msg.Field);
        Assert.Equal("Too short", msg.Text);
        Assert.Equal(Severity.Warning, msg.Severity);
        Assert.Equal("MIN_LENGTH", msg.Code);
    }

    [Fact]
    public void Record_Equality_Same_Values()
    {
        var a = new ValidationMessage("f", "text", Severity.Error, "CODE");
        var b = new ValidationMessage("f", "text", Severity.Error, "CODE");
        Assert.Equal(a, b);
    }

    [Fact]
    public void Record_Equality_Different_Field()
    {
        var a = new ValidationMessage("f1", "text");
        var b = new ValidationMessage("f2", "text");
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Record_Equality_Different_Severity()
    {
        var a = new ValidationMessage("f", "text", Severity.Error);
        var b = new ValidationMessage("f", "text", Severity.Warning);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void With_Expression_Creates_New_Instance()
    {
        var original = new ValidationMessage("email", "Bad format", Severity.Error);
        var modified = original with { Severity = Severity.Warning };

        Assert.Equal(Severity.Error, original.Severity);
        Assert.Equal(Severity.Warning, modified.Severity);
        Assert.Equal("email", modified.Field);
        Assert.Equal("Bad format", modified.Text);
    }

    [Fact]
    public void With_Expression_Changes_Field()
    {
        var original = new ValidationMessage("email", "Required");
        var modified = original with { Field = "name" };

        Assert.Equal("email", original.Field);
        Assert.Equal("name", modified.Field);
    }

    [Fact]
    public void Info_Severity()
    {
        var msg = new ValidationMessage("hint", "Consider a stronger password", Severity.Info);
        Assert.Equal(Severity.Info, msg.Severity);
    }

    [Fact]
    public void Severity_Enum_Values()
    {
        Assert.Equal(0, (int)Severity.Info);
        Assert.Equal(1, (int)Severity.Warning);
        Assert.Equal(2, (int)Severity.Error);
    }
}
