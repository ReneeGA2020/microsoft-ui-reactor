# Windows 11 Design Skill for Duct

AI-assisted guidance for building Windows 11-compliant UI with the Duct framework.

## What's Included

| File | Purpose |
|------|---------|
| `SKILL.md` | Main skill — rules, patterns, checklist, all with Duct C# examples |
| `docs/theme-aware-resources.md` | Theme token reference, acrylic pairings, HC rules |
| `docs/typography-and-colors.md` | Type ramp, approved brush list, color usage |
| `docs/layout-and-scaling.md` | 4px grid, container choice, sizing, shadows |
| `docs/control-styles.md` | Lightweight styling, resource overrides, button patterns |
| `docs/code-review-checklist.md` | Full PR review checklist |

## Usage

### As a GitHub Copilot Custom Instruction

Reference `SKILL.md` from your `.github/copilot-instructions.md` or workspace settings.

### As a VS Code Copilot Skill

Place the `design-skill/` folder in your project and reference it from your VS Code Copilot skill configuration.

### As a Claude Code Skill

Reference `SKILL.md` from your `CLAUDE.md` or `.claude/skills/` directory.

## Origin

Adapted from the Windows UXE team's XAML design guidance for the Duct C# projection. All Windows 11 design rules (theming, High Contrast, typography, layout grid, accessibility) are preserved — XAML syntax has been translated to Duct's fluent C# API, and Shell-specific content has been removed.
