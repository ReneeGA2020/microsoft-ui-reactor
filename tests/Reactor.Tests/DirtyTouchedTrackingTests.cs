using Microsoft.UI.Reactor.Controls.Validation;
using Xunit;

namespace Microsoft.UI.Reactor.Tests;

public class DirtyTouchedTrackingTests
{
    // ════════════════════════════════════════════════════════════════
    //  1E.1 Touched tracking
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Field_Starts_Untouched()
    {
        var ctx = new ValidationContext();
        ctx.RegisterField("email");
        Assert.False(ctx.IsTouched("email"));
    }

    [Fact]
    public void MarkTouched_Marks_Field_Touched()
    {
        var ctx = new ValidationContext();
        ctx.RegisterField("email");
        ctx.MarkTouched("email");
        Assert.True(ctx.IsTouched("email"));
    }

    [Fact]
    public void Focus_And_Blur_Marks_Touched()
    {
        // Simulate the GotFocus/LostFocus flow
        var ctx = new ValidationContext();
        ctx.RegisterField("email");

        // GotFocus → (user interacts) → LostFocus → MarkTouched
        ctx.MarkTouched("email");
        Assert.True(ctx.IsTouched("email"));
    }

    [Fact]
    public void MarkAllTouched_Touches_Every_Registered_Field()
    {
        var ctx = new ValidationContext();
        ctx.RegisterField("email");
        ctx.RegisterField("name");
        ctx.RegisterField("age");

        Assert.False(ctx.IsTouched("email"));
        Assert.False(ctx.IsTouched("name"));
        Assert.False(ctx.IsTouched("age"));

        ctx.MarkAllTouched();

        Assert.True(ctx.IsTouched("email"));
        Assert.True(ctx.IsTouched("name"));
        Assert.True(ctx.IsTouched("age"));
    }

    [Fact]
    public void MarkAllTouched_Only_Touches_Registered_Fields()
    {
        var ctx = new ValidationContext();
        ctx.RegisterField("email");
        // "other" is NOT registered

        ctx.MarkAllTouched();

        Assert.True(ctx.IsTouched("email"));
        Assert.False(ctx.IsTouched("other"));
    }

    // ════════════════════════════════════════════════════════════════
    //  1E.2 Dirty tracking
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Field_Starts_Not_Dirty()
    {
        var ctx = new ValidationContext();
        ctx.RegisterField("email");
        ctx.SetInitialValue("email", "");
        Assert.False(ctx.IsDirty("email"));
    }

    [Fact]
    public void Changing_Value_Makes_Field_Dirty()
    {
        var ctx = new ValidationContext();
        ctx.RegisterField("email");
        ctx.SetInitialValue("email", "");

        ctx.NotifyValueChanged("email", "new@test.com");
        Assert.True(ctx.IsDirty("email"));
    }

    [Fact]
    public void Reverting_To_Initial_Makes_Not_Dirty()
    {
        var ctx = new ValidationContext();
        ctx.RegisterField("email");
        ctx.SetInitialValue("email", "original");

        ctx.NotifyValueChanged("email", "changed");
        Assert.True(ctx.IsDirty("email"));

        ctx.NotifyValueChanged("email", "original");
        Assert.False(ctx.IsDirty("email"));
    }

    [Fact]
    public void IsDirty_Any_Field()
    {
        var ctx = new ValidationContext();
        ctx.RegisterField("email");
        ctx.RegisterField("name");
        ctx.SetInitialValue("email", "a");
        ctx.SetInitialValue("name", "b");

        Assert.False(ctx.IsDirty()); // no field dirty

        ctx.NotifyValueChanged("email", "changed");
        Assert.True(ctx.IsDirty()); // at least one dirty
    }

    [Fact]
    public void IsDirty_False_When_All_Reverted()
    {
        var ctx = new ValidationContext();
        ctx.RegisterField("email");
        ctx.SetInitialValue("email", "a");

        ctx.NotifyValueChanged("email", "b");
        Assert.True(ctx.IsDirty());

        ctx.NotifyValueChanged("email", "a");
        Assert.False(ctx.IsDirty());
    }

    // ════════════════════════════════════════════════════════════════
    //  1E.3 Reset
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Reset_Restores_Initial_Value()
    {
        var ctx = new ValidationContext();
        ctx.RegisterField("email");
        ctx.SetInitialValue("email", "original");
        ctx.NotifyValueChanged("email", "changed");

        var restored = ctx.Reset("email");
        Assert.Equal("original", restored);
    }

    [Fact]
    public void Reset_Clears_Touched_State()
    {
        var ctx = new ValidationContext();
        ctx.RegisterField("email");
        ctx.MarkTouched("email");

        ctx.Reset("email");
        Assert.False(ctx.IsTouched("email"));
    }

    [Fact]
    public void Reset_Clears_Validation_Messages()
    {
        var ctx = new ValidationContext();
        ctx.RegisterField("email");
        ctx.Add("email", "Required");
        ctx.AddExternal("email", "Server error");

        ctx.Reset("email");
        Assert.Empty(ctx.GetMessages("email"));
        Assert.True(ctx.IsValid());
    }

    [Fact]
    public void Reset_Makes_Field_Not_Dirty()
    {
        var ctx = new ValidationContext();
        ctx.RegisterField("email");
        ctx.SetInitialValue("email", "original");
        ctx.NotifyValueChanged("email", "changed");
        Assert.True(ctx.IsDirty("email"));

        ctx.Reset("email");
        Assert.False(ctx.IsDirty("email"));
    }

    [Fact]
    public void ResetAll_Restores_All_Fields()
    {
        var ctx = new ValidationContext();
        ctx.RegisterField("email");
        ctx.RegisterField("name");
        ctx.SetInitialValue("email", "e@test.com");
        ctx.SetInitialValue("name", "John");

        ctx.NotifyValueChanged("email", "changed");
        ctx.NotifyValueChanged("name", "changed");
        ctx.MarkTouched("email");
        ctx.MarkTouched("name");
        ctx.Add("email", "Error");
        ctx.Add("name", "Error");

        var restored = ctx.ResetAll();

        Assert.Equal("e@test.com", restored["email"]);
        Assert.Equal("John", restored["name"]);
        Assert.False(ctx.IsTouched("email"));
        Assert.False(ctx.IsTouched("name"));
        Assert.Empty(ctx.GetAllMessages());
        Assert.False(ctx.IsDirty());
    }

    // ════════════════════════════════════════════════════════════════
    //  1E.4 Gating behavior integration
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Default_Gating_Hides_Errors_Until_Touched()
    {
        var ctx = new ValidationContext();
        ctx.RegisterField("email");
        ctx.Add("email", "Required");

        // Default ShowWhen is WhenTouched
        Assert.False(ErrorStyling.ShouldShowErrors(ctx, "email", ShowWhen.WhenTouched));
    }

    [Fact]
    public void Touch_Reveals_Errors()
    {
        var ctx = new ValidationContext();
        ctx.RegisterField("email");
        ctx.Add("email", "Required");

        // Simulate: type → blur → see error
        ctx.MarkTouched("email");
        Assert.True(ErrorStyling.ShouldShowErrors(ctx, "email", ShowWhen.WhenTouched));
    }

    [Fact]
    public void Submit_Shows_All_Errors_At_Once()
    {
        var ctx = new ValidationContext();
        ctx.RegisterField("email");
        ctx.RegisterField("name");
        ctx.Add("email", "Required");
        ctx.Add("name", "Required");

        // Before submit: nothing visible
        Assert.False(ErrorStyling.ShouldShowErrors(ctx, "email", ShowWhen.WhenTouched));
        Assert.False(ErrorStyling.ShouldShowErrors(ctx, "name", ShowWhen.WhenTouched));

        // Submit → MarkAllTouched → all errors visible
        ctx.MarkAllTouched();
        Assert.True(ErrorStyling.ShouldShowErrors(ctx, "email", ShowWhen.WhenTouched));
        Assert.True(ErrorStyling.ShouldShowErrors(ctx, "name", ShowWhen.WhenTouched));
    }

    [Fact]
    public void Summary_Visualizer_Shows_Errors_Regardless_Of_Touched()
    {
        var ctx = new ValidationContext();
        ctx.Add("email", "Required");

        // Summary uses ShowWhen.Always
        var messages = ctx.GetAllMessages();
        Assert.True(ErrorBubbling.ShouldDisplay(messages, ShowWhen.Always));
    }

    [Fact]
    public void ShowWhen_Override_Respected()
    {
        var ctx = new ValidationContext();
        ctx.RegisterField("email");
        ctx.SetInitialValue("email", "original");
        ctx.Add("email", "Required");

        // WhenDirty: not dirty yet
        Assert.False(ErrorStyling.ShouldShowErrors(ctx, "email", ShowWhen.WhenDirty));

        // Make dirty
        ctx.NotifyValueChanged("email", "changed");
        ctx.Add("email", "Required"); // Re-add since NotifyValueChanged clears external
        Assert.True(ErrorStyling.ShouldShowErrors(ctx, "email", ShowWhen.WhenDirty));
    }

    // ════════════════════════════════════════════════════════════════
    //  Integration: full touched-gating scenario
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Full_Scenario_Type_Blur_See_Error()
    {
        var ctx = new ValidationContext();
        ctx.RegisterField("email");

        // 1. Render with empty value, run validators
        ValidationReconciler.ValidateField(ctx, "email", "",
            Validate.Required());

        // Error exists but not visible (not touched)
        Assert.False(ctx.IsValid());
        Assert.False(ErrorStyling.ShouldShowErrors(ctx, "email", ShowWhen.WhenTouched));

        // 2. User focuses then blurs the field
        ctx.MarkTouched("email");

        // Now error is visible
        Assert.True(ErrorStyling.ShouldShowErrors(ctx, "email", ShowWhen.WhenTouched));

        // 3. User types a valid email
        ValidationReconciler.ValidateField(ctx, "email", "user@test.com",
            Validate.Required());

        // Error gone
        Assert.True(ctx.IsValid());
        Assert.False(ErrorStyling.ShouldShowErrors(ctx, "email", ShowWhen.WhenTouched));
    }

    [Fact]
    public void Full_Scenario_Submit_Shows_All_Then_Fix()
    {
        var ctx = new ValidationContext();

        // Set up fields
        ValidationReconciler.ValidateField(ctx, "email", "",
            Validate.Required());
        ValidationReconciler.ValidateField(ctx, "name", "",
            Validate.Required());

        // Nothing visible (not touched)
        Assert.False(ErrorStyling.ShouldShowErrors(ctx, "email", ShowWhen.WhenTouched));
        Assert.False(ErrorStyling.ShouldShowErrors(ctx, "name", ShowWhen.WhenTouched));

        // Submit attempt
        ctx.MarkAllTouched();

        // All errors visible now
        Assert.True(ErrorStyling.ShouldShowErrors(ctx, "email", ShowWhen.WhenTouched));
        Assert.True(ErrorStyling.ShouldShowErrors(ctx, "name", ShowWhen.WhenTouched));

        // Fix email
        ValidationReconciler.ValidateField(ctx, "email", "user@test.com",
            Validate.Required());

        // Email error gone, name still showing
        Assert.False(ErrorStyling.ShouldShowErrors(ctx, "email", ShowWhen.WhenTouched));
        Assert.True(ErrorStyling.ShouldShowErrors(ctx, "name", ShowWhen.WhenTouched));
    }
}
