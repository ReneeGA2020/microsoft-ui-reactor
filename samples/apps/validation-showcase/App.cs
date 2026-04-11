// Validation Showcase — Demonstrates the Duct forms & validation system.
// Shows: ValidationContext, built-in validators, error styling,
// MaskedTextBox, InputFormatters, FocusManager, cross-field rules,
// window-level InfoBar validation summary.

using Duct;
using Duct.Core;
using Duct.Hooks;
using Duct.Validation;
using Duct.Validation.Validators;
using Duct.Controls.Formatting;
using Duct.Controls.MaskedTextBox;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Duct.UI;
using static Duct.Validation.ValidationRuleDsl;
using Duct.Animation;

DuctApp.Run<ShowcaseApp>("Validation Showcase", width: 720, height: 900
#if DEBUG
    , preview: true
#endif
);

// ─── Helpers ────────────────────────────────────────────────────────────

static class FieldUI
{
    public static Element Field(
        string label,
        Element control,
        ValidationContext ctx,
        string fieldName,
        bool required = false,
        string? description = null,
        ShowWhen showWhen = ShowWhen.WhenTouched,
        bool submitted = false)
    {
        var showErrors = ErrorStyling.ShouldShowErrors(ctx, fieldName, showWhen,
            submitAttempted: submitted);
        var msgs = showErrors ? ctx.GetMessages(fieldName) : [];

        var labelText = required ? $"{label} *" : label;
        var descOrError = msgs.Count > 0
            ? string.Join(" · ", msgs.Select(m => m.Text))
            : description;
        var isError = msgs.Count > 0;

        return VStack(4,
            Text(labelText).FontSize(13)
                .Set(t => t.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold),
            control,
            When(descOrError is not null, () =>
                Text(descOrError!)
                    .FontSize(12)
                    .Foreground(isError ? "#d13438" : "#888888"))
        );
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  1. Basic inline validation with window-level InfoBar
// ═══════════════════════════════════════════════════════════════════════

class BasicValidationDemo : Component
{
    public override Element Render()
    {
        var ctx = this.UseValidationContext();
        var fm = this.UseFocus();
        var (submitted, setSubmitted) = UseState(false);
        var (infoDismissed, setInfoDismissed) = UseState(false);

        var (email, setEmail) = UseState("");
        var (age, setAge) = UseState(0.0);
        var (website, setWebsite) = UseState("");

        ValidationReconciler.ValidateField(ctx, "email", email,
            Validate.Required("Email is required"),
            Validate.Email("Enter a valid email"));
        ValidationReconciler.ValidateField(ctx, "age", age,
            Validate.Range(18, 120, "Age must be between 18 and 120"));
        ValidationReconciler.ValidateField(ctx, "website", website,
            Validate.Url("Enter a valid URL (https://...)"));

        var sw = submitted ? ShowWhen.Always : ShowWhen.WhenTouched;
        var errorCount = ctx.InvalidFields.Count;
        var showInfoBar = submitted && !ctx.IsValid() && !infoDismissed;

        // Reset dismissed state when errors change
        if (submitted && ctx.IsValid())
            infoDismissed = false;

        return VStack(0,
            // Window-level InfoBar — slides in from top
            When(showInfoBar, () =>
                (InfoBar($"Please fix {errorCount} error{(errorCount == 1 ? "" : "s")} before submitting",
                         string.Join(", ", ctx.InvalidFields.Select(f => f)))
                    with
                    {
                        IsOpen = true,
                        IsClosable = true,
                        OnClosed = () => setInfoDismissed(true),
                    })
                    .Severity(InfoBarSeverity.Error)
                    .Transition(Transition.Fade + Transition.Slide(Edge.Top))
                    .Margin(0, 0, 0, 8)),

            When(submitted && ctx.IsValid(), () =>
                (InfoBar("Success", "Form submitted successfully!") with
                    {
                        IsOpen = true,
                        IsClosable = true,
                    })
                    .Severity(InfoBarSeverity.Success)
                    .Transition(Transition.Fade + Transition.Slide(Edge.Top))
                    .Margin(0, 0, 0, 8)),

            VStack(12,
                FieldUI.Field("Email",
                    TextField(email, v => { setEmail(v); ctx.MarkTouched("email"); setInfoDismissed(false); },
                            placeholder: "user@example.com")
                        .Focus(fm, "email", autoFocus: true),
                    ctx, "email", required: true,
                    description: "We'll never share your email",
                    showWhen: sw, submitted: submitted),

                FieldUI.Field("Age",
                    NumberBox(age, v => { setAge(v); ctx.MarkTouched("age"); setInfoDismissed(false); })
                        .Focus(fm, "age"),
                    ctx, "age", required: true,
                    description: "Must be 18 or older",
                    showWhen: sw, submitted: submitted),

                FieldUI.Field("Website",
                    TextField(website, v => { setWebsite(v); ctx.MarkTouched("website"); setInfoDismissed(false); },
                            placeholder: "https://example.com")
                        .Focus(fm, "website"),
                    ctx, "website",
                    description: "Optional — your personal site",
                    showWhen: sw, submitted: submitted),

                HStack(12,
                    Button("Submit", () =>
                    {
                        setSubmitted(true);
                        setInfoDismissed(false);
                        ctx.MarkAllTouched();
                        if (!ctx.IsValid())
                            fm.FocusFirst(ctx.InvalidFields);
                    }),
                    Button("Reset", () =>
                    {
                        setSubmitted(false);
                        setInfoDismissed(false);
                        setEmail("");
                        setAge(0.0);
                        setWebsite("");
                        ctx.ClearAll();
                    })
                ).Margin(0, 8, 0, 0)
            )
        ).Padding(24);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  2. Password form with cross-field validation
// ═══════════════════════════════════════════════════════════════════════

class PasswordFormDemo : Component
{
    public override Element Render()
    {
        var ctx = this.UseValidationContext();
        var (submitted, setSubmitted) = UseState(false);

        var (password, setPassword) = UseState("");
        var (confirm, setConfirm) = UseState("");
        var (agree, setAgree) = UseState(false);

        ValidationReconciler.ValidateField(ctx, "password", password,
            Validate.Required("Password is required"),
            Validate.MinLength(8, "At least 8 characters"),
            Validate.Must<string>(
                s => s.Any(char.IsUpper) && s.Any(char.IsDigit),
                "Must contain uppercase letter and digit"));

        ValidationReconciler.ValidateField(ctx, "confirm", confirm,
            Validate.Required("Please confirm your password"));

        ValidationReconciler.ValidateField(ctx, "agree", agree,
            Validate.MustBeTrue("You must accept the terms"));

        var matchRule = ValidationRule(
            () => string.IsNullOrEmpty(confirm) || password == confirm,
            "Passwords do not match",
            "confirm");
        matchRule.Evaluate(ctx);

        var sw = submitted ? ShowWhen.Always : ShowWhen.WhenTouched;

        return VStack(12,
            SubHeading("Cross-Field Validation"),
            Caption("Password confirmation uses a cross-field ValidationRule."),

            FieldUI.Field("Password",
                PasswordBox(password, v => { setPassword(v); ctx.MarkTouched("password"); }),
                ctx, "password", required: true,
                description: "8+ chars, uppercase + digit",
                showWhen: sw, submitted: submitted),

            FieldUI.Field("Confirm Password",
                PasswordBox(confirm, v => { setConfirm(v); ctx.MarkTouched("confirm"); }),
                ctx, "confirm", required: true,
                showWhen: sw, submitted: submitted),

            FieldUI.Field("Terms",
                CheckBox(agree, v => { setAgree(v); ctx.MarkTouched("agree"); },
                    label: "I accept the terms of service"),
                ctx, "agree",
                showWhen: sw, submitted: submitted),

            HStack(12,
                Button("Set Password", () =>
                {
                    setSubmitted(true);
                    ctx.MarkAllTouched();
                }),
                When(submitted && ctx.IsValid(), () =>
                    Text("Password set!").Foreground("#107c10").FontSize(13)
                        .VAlign(VerticalAlignment.Center))
            ).Margin(0, 8, 0, 0)
        ).Padding(24)
         .Provide(ValidationContexts.Current, ctx);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  3. Masked input & formatters
// ═══════════════════════════════════════════════════════════════════════

class MaskedInputDemo : Component
{
    public override Element Render()
    {
        var (phone, setPhone) = UseState("");
        var (ssn, setSsn) = UseState("");
        var (zip, setZip) = UseState("");
        var (amount, setAmount) = UseState("");
        var (shout, setShout) = UseState("");

        var phoneMask = new MaskEngine(MaskPreset.PhoneUS);
        var ssnMask = new MaskEngine(MaskPreset.SSN);
        var zipMask = new MaskEngine(MaskPreset.ZipCode);

        var phoneFormatted = phoneMask.Apply(phone);
        var ssnFormatted = ssnMask.Apply(ssn);
        var zipFormatted = zipMask.Apply(zip);

        var currencyFmt = InputFormatter.Currency("$");
        var currencyResult = amount.Length > 0
            ? currencyFmt.Format(amount, amount.Length) : new FormatResult("", 0);

        return VStack(12,
            SubHeading("Masked Input & Formatters"),
            Caption("Input masks enforce structured formats. Formatters transform text live."),

            VStack(8,
                Text("Phone (US) — digits only").FontSize(13).Opacity(0.7),
                TextField(phone, v => setPhone(new string(v.Where(char.IsDigit).ToArray())),
                    placeholder: "5551234567"),
                When(phone.Length > 0, () =>
                    HStack(8,
                        Text($"Formatted: {phoneFormatted}").FontSize(12).Opacity(0.6),
                        Text($"Raw: {phoneMask.GetRawValue(phoneFormatted)}").FontSize(12).Opacity(0.6),
                        Text(phoneMask.IsComplete(phoneFormatted) ? "Complete" : "Incomplete")
                            .FontSize(12)
                            .Foreground(phoneMask.IsComplete(phoneFormatted) ? "#107c10" : "#d13438")
                    ))
            ),

            VStack(8,
                Text("SSN — digits only").FontSize(13).Opacity(0.7),
                TextField(ssn, v => setSsn(new string(v.Where(char.IsDigit).ToArray())),
                    placeholder: "123456789"),
                When(ssn.Length > 0, () =>
                    Text($"Formatted: {ssnFormatted}").FontSize(12).Opacity(0.6))
            ),

            VStack(8,
                Text("ZIP Code — digits only").FontSize(13).Opacity(0.7),
                TextField(zip, v => setZip(new string(v.Where(char.IsDigit).ToArray())),
                    placeholder: "90210"),
                When(zip.Length > 0, () =>
                    Text($"Formatted: {zipFormatted}").FontSize(12).Opacity(0.6))
            ),

            VStack(8,
                Text("Currency (InputFormatter)").FontSize(13).Opacity(0.7),
                TextField(amount,
                    v => setAmount(new string(v.Where(c => char.IsDigit(c) || c == '.').ToArray())),
                    placeholder: "1234.56"),
                When(amount.Length > 0, () =>
                    Text($"Display: {currencyResult.Output}  |  Raw: {currencyFmt.Parse(currencyResult.Output)}")
                        .FontSize(12).Opacity(0.6))
            ),

            VStack(8,
                Text("Uppercase Formatter").FontSize(13).Opacity(0.7),
                TextField(shout, v => setShout(v.ToUpperInvariant()),
                    placeholder: "auto uppercased")
                    .Set(tb => tb.CharacterCasing = CharacterCasing.Upper)
            )
        ).Padding(24);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  4. Dirty / Touched / Reset demo
// ═══════════════════════════════════════════════════════════════════════

class DirtyResetDemo : Component
{
    public override Element Render()
    {
        var ctx = this.UseValidationContext();
        // tick forces re-render when we mutate ctx outside of state setters
        var (tick, setTick) = UseState(0);

        var (name, setName) = UseState("John Doe");
        var (color, setColor) = UseState(0);
        var colors = new[] { "Red", "Green", "Blue" };

        ctx.RegisterField("name");
        ctx.SetInitialValue("name", "John Doe");
        ctx.NotifyValueChanged("name", name);

        ctx.RegisterField("color");
        ctx.SetInitialValue("color", 0);
        ctx.NotifyValueChanged("color", color);

        return VStack(12,
            SubHeading("Dirty & Touched Tracking"),
            Caption("Tracks whether fields changed from initial values or were interacted with."),

            VStack(4,
                Text("Name").FontSize(13)
                    .Set(t => t.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold),
                TextField(name, v => { setName(v); ctx.MarkTouched("name"); }),
                Text("Initial: \"John Doe\"").FontSize(12).Foreground("#888888")
            ),

            VStack(4,
                Text("Favorite Color").FontSize(13)
                    .Set(t => t.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold),
                ComboBox(colors, color, v => { setColor(v); ctx.MarkTouched("color"); }),
                Text("Initial: Red").FontSize(12).Foreground("#888888")
            ),

            VStack(4,
                Text($"name — touched: {ctx.IsTouched("name")}, dirty: {ctx.IsDirty("name")}")
                    .FontSize(12).Opacity(0.6),
                Text($"color — touched: {ctx.IsTouched("color")}, dirty: {ctx.IsDirty("color")}")
                    .FontSize(12).Opacity(0.6),
                Text($"Form dirty: {ctx.IsDirty()}").FontSize(12).Opacity(0.6)
            ),

            HStack(12,
                Button("Reset All", () =>
                {
                    var restored = ctx.ResetAll();
                    if (restored.TryGetValue("name", out var n)) setName((string)(n ?? ""));
                    if (restored.TryGetValue("color", out var c)) setColor((int)(c ?? 0));
                    setTick(tick + 1);
                }).Disabled(!ctx.IsDirty()),
                Button("Mark All Touched", () =>
                {
                    ctx.MarkAllTouched();
                    setTick(tick + 1);
                })
            ).Margin(0, 8, 0, 0)
        ).Padding(24);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  5. Focus management demo
// ═══════════════════════════════════════════════════════════════════════

class FocusDemo : Component
{
    public override Element Render()
    {
        var fm = this.UseFocus();
        var (log, setLog) = UseState("");

        fm.SetSubmitHandler(() => setLog("Form submitted via Enter on last field!"));

        var (f1, setF1) = UseState("");
        var (f2, setF2) = UseState("");
        var (f3, setF3) = UseState("");

        return VStack(12,
            SubHeading("Focus Management"),
            Caption("UseFocus provides programmatic focus control and enter-to-advance."),

            TextField(f1, setF1, placeholder: "First field", header: "First Name")
                .Focus(fm, "first"),
            TextField(f2, setF2, placeholder: "Second field", header: "Last Name")
                .Focus(fm, "last"),
            TextField(f3, setF3, placeholder: "Third (last) field", header: "Nickname")
                .Focus(fm, "nick"),

            HStack(8,
                Button("Focus First", () => fm.FocusField("first")),
                Button("Focus Next", () => fm.FocusNext("first")),
                Button("Focus Last", () => fm.FocusField("nick"))
            ),

            When(log.Length > 0, () =>
                Text(log).FontSize(12).Opacity(0.6).Margin(0, 4, 0, 0))
        ).Padding(24);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  Root app
// ═══════════════════════════════════════════════════════════════════════

class ShowcaseApp : Component
{
    public override Element Render()
    {
        return Grid(
            ["*"], ["Auto", "*"],

            TitleBar("Validation Showcase")
                .Subtitle("Duct Forms & Data Entry")
                .Grid(row: 0),

            ScrollView(
                VStack(0,
                    Component<BasicValidationDemo>(),

                    Separator(),

                    Component<PasswordFormDemo>(),

                    Separator(),

                    Component<MaskedInputDemo>(),

                    Separator(),

                    Component<DirtyResetDemo>(),

                    Separator(),

                    Component<FocusDemo>(),

                    Empty().Height(24)
                )
            ).Grid(row: 1)
        );
    }

    static Element Separator() =>
        Border(Empty().Height(1))
            .Set(b => b.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Microsoft.UI.Colors.Gray))
            .Opacity(0.15)
            .Margin(24, 8, 24, 8);
}
