using Duct.Core;
using Duct.Validation;
using Duct.Validation.Validators;
using static Duct.UI;
using static Duct.Validation.FormFieldDsl;
using Xunit;

namespace Duct.Tests;

public class FormFieldTests
{
    // ════════════════════════════════════════════════════════════════
    //  FormField element creation
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void FormField_Creates_With_Label_And_Content()
    {
        var el = FormField(
            TextField("test"),
            label: "Email",
            description: "Enter your email address");

        Assert.IsType<FormFieldElement>(el);
        Assert.Equal("Email", el.Label);
        Assert.Equal("Enter your email address", el.Description);
        Assert.IsType<TextFieldElement>(el.Content);
    }

    [Fact]
    public void FormField_Required_True()
    {
        var el = FormField(TextField(""), label: "Name", required: true);
        Assert.True(el.Required);
    }

    [Fact]
    public void FormField_Default_ShowWhen_Is_WhenTouched()
    {
        var el = FormField(TextField(""), label: "Name");
        Assert.Equal(ShowWhen.WhenTouched, el.ShowWhen);
    }

    [Fact]
    public void FormField_Explicit_FieldName()
    {
        var el = FormField(TextField(""), fieldName: "email");
        Assert.Equal("email", el.FieldName);
    }

    // ════════════════════════════════════════════════════════════════
    //  Display label with required indicator
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void GetDisplayLabel_Without_Required()
    {
        Assert.Equal("Email", FormFieldHelpers.GetDisplayLabel("Email", false));
    }

    [Fact]
    public void GetDisplayLabel_With_Required()
    {
        Assert.Equal("Email *", FormFieldHelpers.GetDisplayLabel("Email", true));
    }

    [Fact]
    public void GetDisplayLabel_Null_Label()
    {
        Assert.Equal("", FormFieldHelpers.GetDisplayLabel(null, true));
    }

    // ════════════════════════════════════════════════════════════════
    //  Description / error swapping
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void GetDescriptionOrError_Shows_Description_When_Valid()
    {
        var ctx = new ValidationContext();

        var (text, isError) = FormFieldHelpers.GetDescriptionOrError(
            ctx, "email", "Enter your email", ShowWhen.Always);

        Assert.Equal("Enter your email", text);
        Assert.False(isError);
    }

    [Fact]
    public void GetDescriptionOrError_Shows_Error_When_Invalid()
    {
        var ctx = new ValidationContext();
        ctx.Add("email", "Required");

        var (text, isError) = FormFieldHelpers.GetDescriptionOrError(
            ctx, "email", "Enter your email", ShowWhen.Always);

        Assert.Equal("Required", text);
        Assert.True(isError);
    }

    [Fact]
    public void GetDescriptionOrError_Multiple_Errors_Concatenated()
    {
        var ctx = new ValidationContext();
        ctx.Add("email", "Required");
        ctx.Add("email", "Invalid format");

        var (text, isError) = FormFieldHelpers.GetDescriptionOrError(
            ctx, "email", "hint", ShowWhen.Always);

        Assert.Contains("Required", text);
        Assert.Contains("Invalid format", text);
        Assert.Contains("•", text); // Bullet separator
        Assert.True(isError);
    }

    [Fact]
    public void GetDescriptionOrError_Respects_ShowWhen()
    {
        var ctx = new ValidationContext();
        ctx.Add("email", "Required");
        // Field not touched

        var (text, isError) = FormFieldHelpers.GetDescriptionOrError(
            ctx, "email", "hint", ShowWhen.WhenTouched);

        // Not touched, so show description not error
        Assert.Equal("hint", text);
        Assert.False(isError);
    }

    [Fact]
    public void GetDescriptionOrError_Shows_Error_When_Touched()
    {
        var ctx = new ValidationContext();
        ctx.Add("email", "Required");
        ctx.MarkTouched("email");

        var (text, isError) = FormFieldHelpers.GetDescriptionOrError(
            ctx, "email", "hint", ShowWhen.WhenTouched);

        Assert.Equal("Required", text);
        Assert.True(isError);
    }

    [Fact]
    public void GetDescriptionOrError_Swaps_Back_When_Valid()
    {
        var ctx = new ValidationContext();
        ctx.Add("email", "Required");
        ctx.MarkTouched("email");

        // Error showing
        var (text1, isError1) = FormFieldHelpers.GetDescriptionOrError(
            ctx, "email", "hint", ShowWhen.WhenTouched);
        Assert.True(isError1);

        // Clear error
        ctx.Clear("email");
        var (text2, isError2) = FormFieldHelpers.GetDescriptionOrError(
            ctx, "email", "hint", ShowWhen.WhenTouched);
        Assert.Equal("hint", text2);
        Assert.False(isError2);
    }

    [Fact]
    public void GetDescriptionOrError_Null_Context_Returns_Description()
    {
        var (text, isError) = FormFieldHelpers.GetDescriptionOrError(
            null, "email", "hint", ShowWhen.Always);

        Assert.Equal("hint", text);
        Assert.False(isError);
    }

    [Fact]
    public void GetDescriptionOrError_Null_FieldName_Returns_Description()
    {
        var ctx = new ValidationContext();
        ctx.Add("email", "error");

        var (text, isError) = FormFieldHelpers.GetDescriptionOrError(
            ctx, null, "hint", ShowWhen.Always);

        Assert.Equal("hint", text);
        Assert.False(isError);
    }

    // ════════════════════════════════════════════════════════════════
    //  Accessibility helpers
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void GetAutomationName_Strips_Required_Indicator()
    {
        Assert.Equal("Email", FormFieldHelpers.GetAutomationName("Email *"));
    }

    [Fact]
    public void GetAutomationName_Returns_Label_Without_Required()
    {
        Assert.Equal("Email", FormFieldHelpers.GetAutomationName("Email"));
    }

    [Fact]
    public void GetAutomationName_Null_Returns_Null()
    {
        Assert.Null(FormFieldHelpers.GetAutomationName(null));
    }

    // ════════════════════════════════════════════════════════════════
    //  Field name auto-detection
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void DetectFieldName_From_ValidationAttached()
    {
        var el = TextField("test")
            .Validate("email", Validate.Required());

        Assert.Equal("email", FormFieldHelpers.DetectFieldName(el));
    }

    [Fact]
    public void DetectFieldName_Returns_Null_Without_Validation()
    {
        var el = TextField("test");
        Assert.Null(FormFieldHelpers.DetectFieldName(el));
    }

    [Fact]
    public void ResolveFieldName_Prefers_Explicit()
    {
        var el = TextField("test")
            .Validate("auto", Validate.Required());

        Assert.Equal("explicit", FormFieldHelpers.ResolveFieldName("explicit", el));
    }

    [Fact]
    public void ResolveFieldName_Falls_Back_To_Auto()
    {
        var el = TextField("test")
            .Validate("auto", Validate.Required());

        Assert.Equal("auto", FormFieldHelpers.ResolveFieldName(null, el));
    }
}
