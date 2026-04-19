using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using static Microsoft.UI.Reactor.Factories;

namespace Microsoft.UI.Reactor.AppTests.Host.Fixtures;

/// <summary>
/// Test fixtures that mount controls with accessibility modifiers.
/// Each control has an AutomationId so the out-of-process UIA tests
/// (AccessibilityTests.cs) can find them and read their UIA properties
/// through the real accessibility pipeline (Narrator/NVDA/WinAppDriver).
/// </summary>
internal static class AccessibilityFixtures
{
    /// <summary>
    /// Comprehensive accessibility fixture exercising every Reactor a11y modifier.
    /// Maps to WCAG 2.1 success criteria. UIA properties are validated from
    /// the out-of-process Appium tests via WinAppDriver's GetAttribute() API.
    /// </summary>
    internal static Element AccessibilityShowcase(RenderContext ctx)
    {
        return VStack(8,
            // ── WCAG 1.1.1: Non-text Content ──────────────────
            // Icon-only button needs accessible name
            Button("🔍")
                .AutomationName("Search documents")
                .AutomationId("A11y_SearchBtn"),

            // Decorative image hidden from screen readers
            Image("ms-appx:///Assets/StoreLogo.png")
                .AccessibilityHidden()
                .AutomationId("A11y_DecorativeImg")
                .Width(16).Height(16),

            // ── WCAG 1.3.1: Info and Relationships ────────────
            // Heading levels for document structure
            TextBlock("Account Settings")
                .HeadingLevel(AutomationHeadingLevel.Level1)
                .AutomationId("A11y_H1"),

            TextBlock("Personal Information")
                .HeadingLevel(AutomationHeadingLevel.Level2)
                .AutomationId("A11y_H2"),

            // Navigation landmark
            HStack(
                Button("Home").AutomationId("A11y_NavHome"),
                Button("Profile").AutomationId("A11y_NavProfile")
            ).Landmark(AutomationLandmarkType.Navigation)
             .AutomationId("A11y_NavBar"),

            // Main content landmark
            VStack(
                // ── WCAG 3.3.2: Labels and Instructions ───────
                // Form field with full accessibility annotations
                TextField("user@example.com")
                    .AutomationName("Email address")
                    .Required()
                    .HelpText("Enter your primary contact email")
                    .FullDescription("This email will be used for account recovery and notifications. Must be a valid email format.")
                    .AutomationId("A11y_EmailField"),

                // ── WCAG 4.1.2: Name, Role, Value ────────────
                // Control with item status
                CheckBox(true, label: "Notifications")
                    .AutomationName("Enable notifications")
                    .ItemStatus("Currently enabled")
                    .AutomationId("A11y_NotifCB"),

                // Control with position in set
                TextBlock("Step 2 of 5")
                    .PositionInSet(2, 5)
                    .AutomationId("A11y_StepIndicator"),

                // Hierarchy level
                TextBlock("Category")
                    .HierarchyLevel(1)
                    .AutomationId("A11y_Level1"),

                TextBlock("Sub-category")
                    .HierarchyLevel(2)
                    .AutomationId("A11y_Level2")

            ).Landmark(AutomationLandmarkType.Main)
             .AutomationId("A11y_MainContent"),

            // ── WCAG 2.1.1: Keyboard ─────────────────────────
            Button("File")
                .AccessKey("F")
                .TabIndex(1)
                .AutomationId("A11y_FileBtn"),

            Button("Edit")
                .AccessKey("E")
                .TabIndex(2)
                .AutomationId("A11y_EditBtn"),

            // Toolbar with contained tab navigation
            HStack(
                Button("Bold").AutomationId("A11y_BoldBtn"),
                Button("Italic").AutomationId("A11y_ItalicBtn")
            ).TabNavigation(Microsoft.UI.Xaml.Input.KeyboardNavigationMode.Once)
             .AutomationId("A11y_Toolbar"),

            // ── WCAG 4.1.3: Status Messages ──────────────────
            // Live regions for dynamic announcements
            TextBlock("Status: Ready")
                .LiveRegion(AutomationLiveSetting.Polite)
                .AutomationId("A11y_StatusPolite"),

            TextBlock("Alert: None")
                .LiveRegion(AutomationLiveSetting.Assertive)
                .AutomationId("A11y_AlertAssertive"),

            // ── AccessibilityView variants ────────────────────
            TextBlock("Visible to AT")
                .AccessibilityView(AccessibilityView.Content)
                .AutomationId("A11y_ViewContent"),

            TextBlock("Hidden from AT")
                .AccessibilityView(AccessibilityView.Raw)
                .AutomationId("A11y_ViewRaw")
        );
    }
}
