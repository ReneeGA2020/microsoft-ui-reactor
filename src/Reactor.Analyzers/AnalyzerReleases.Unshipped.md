### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
DUCT001 | Reactor.Style | Warning | UseThemeRefAnalyzer - Use ThemeRef instead of hard-coded color
DUCT002 | Reactor.Style | Info | UseLightweightStylingAnalyzer - Consider lightweight styling for visual-state overrides
DUCT003 | Reactor.Style | Info | RequestedThemeSetAnalyzer - RequestedTheme modifier available
REACTOR_HOOKS_001 | Reactor.Hooks | Warning | HookRulesAnalyzer - Hook called conditionally
REACTOR_HOOKS_004 | Reactor.Hooks | Warning | HookRulesAnalyzer - Hook deps contains freshly allocated value
REACTOR_HOOKS_005 | Reactor.Hooks | Warning | HookRulesAnalyzer - Hook called outside Render or custom-hook method
REACTOR_HOOKS_006 | Reactor.Hooks | Info | HookRulesAnalyzer - UseResource fetcher looks non-idempotent (use UseMutation for writes)
