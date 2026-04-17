# 021 — Docs Folder Reorganization

**Status:** Accepted — executed
**Date:** 2026-04-17

## Overview

Reorganize `docs/` into four clearly-separated buckets — **end-user/dev documentation**, **planning** (specs, tasks, proposals), **research** (analysis, investigations, comparisons), and **pipeline internals** — plus small homes for pitch assets and point-in-time reports. The current layout mixes all of these at the top level and scatters loose files in `docs/` root, which makes it hard for new contributors to know what is authoritative, what is generated, and what is scratch work.

The end-user docs stay checked in as pre-rendered Markdown because the compilation pipeline needs a running WinUI host to capture screenshots and cannot run on hosted CI runners today.

---

## 1. Current Layout

```
docs/
├── apps/                          # doc-pipeline: snippet-source Reactor apps (20 topics)
├── bentobox.svg                   # pitch asset
├── bugs/                          # bug investigations
├── compare/                       # per-framework competitive analysis
├── critical-review.md             # research (root)
├── design-targets/                # pitch/design reference images
├── flux-ui-analysis.md            # research (root)
├── investigation/                 # technical investigations
├── output/                        # compiled end-user docs (generated, committed)
├── proposals/                     # pre-spec ideas
├── reactor-pitch.md               # marketing (root)
├── reference/                     # dev-facing architecture deep dives
├── spec/                          # numbered design specs (+ archived/)
├── tasks/                         # per-spec implementation task lists
├── templates/                     # doc-pipeline: .md.dt templates (+ ai-author-skill.md)
├── winui3-integration-proposals.md # pre-spec idea (root)
└── worksummary/                   # point-in-time activity report
```

Problems:

- Five loose `.md` / `.svg` files at `docs/` root with no category.
- `spec/`, `tasks/`, and `proposals/` are all planning artifacts but live as peers.
- `critical-review.md`, `flux-ui-analysis.md`, `compare/`, `investigation/`, and `bugs/` are all research but are scattered.
- `output/` reads like a build artifact, discouraging readers — but it is the website.
- `apps/` and `templates/` are pipeline implementation, not docs, yet sit at the top of the tree.
- No index or README tells a newcomer which folder is authoritative for what.

## 2. Target Layout

```
docs/
├── README.md                      # index: what each folder is for

├── guide/                         # end-user docs (rendered site) — from output/
│   ├── getting-started.md
│   ├── layout.md, styling.md, …
│   └── images/

├── reference/                     # dev-facing architecture deep dives (unchanged)
│   ├── state-and-hooks.md
│   ├── reconciliation.md
│   ├── native-differ.md
│   └── localization-howto.md

├── specs/                         # PLANNING (rename from spec/)
│   ├── 001-theming-design.md … 021-docs-reorganization.md
│   ├── archived/
│   ├── tasks/                     # per-spec implementation checklists
│   └── proposals/                 # pre-spec ideas
│       ├── forms-data-entry.md
│       └── winui3-integration.md  # ← docs/winui3-integration-proposals.md

├── research/                      # RESEARCH
│   ├── critical-review.md         # ← docs/critical-review.md
│   ├── flux-ui-analysis.md        # ← docs/flux-ui-analysis.md
│   ├── compare/                   # unchanged
│   ├── investigations/            # rename from investigation/
│   └── bugs/                      # unchanged

├── pitch/                         # marketing assets
│   ├── reactor-pitch.md           # ← docs/reactor-pitch.md
│   ├── bentobox.svg               # ← docs/bentobox.svg
│   └── design-targets/            # ← docs/design-targets/

├── reports/
│   └── work-summary/              # rename from worksummary/

└── _pipeline/                     # doc-gen internals (not reader-facing)
    ├── apps/                      # ← docs/apps/
    ├── templates/                 # ← docs/templates/
    └── ai-author-skill.md         # stays with the pipeline
```

## 3. Move Map

| From | To |
|---|---|
| `docs/output/` | `docs/guide/` |
| `docs/spec/` | `docs/specs/` |
| `docs/spec/archived/` | `docs/specs/archived/` |
| `docs/tasks/` | `docs/specs/tasks/` |
| `docs/proposals/` | `docs/specs/proposals/` |
| `docs/winui3-integration-proposals.md` | `docs/specs/proposals/winui3-integration.md` |
| `docs/critical-review.md` | `docs/research/critical-review.md` |
| `docs/flux-ui-analysis.md` | `docs/research/flux-ui-analysis.md` |
| `docs/compare/` | `docs/research/compare/` |
| `docs/investigation/` | `docs/research/investigations/` |
| `docs/bugs/` | `docs/research/bugs/` |
| `docs/reactor-pitch.md` | `docs/pitch/reactor-pitch.md` |
| `docs/bentobox.svg` | `docs/pitch/bentobox.svg` |
| `docs/design-targets/` | `docs/pitch/design-targets/` |
| `docs/worksummary/` | `docs/reports/work-summary/` |
| `docs/apps/` | `docs/_pipeline/apps/` |
| `docs/templates/` | `docs/_pipeline/templates/` |
| `docs/reference/` | unchanged |

All moves performed with `git mv` to preserve history.

## 4. Rationale

- **`guide/` + `reference/` split** matches the Rust, Vue, and React conventions for user-facing documentation. "Guide" = learn-by-doing topics; "reference" = deep dives and API contracts.
- **`specs/` with `tasks/` and `proposals/` nested** makes the planning lifecycle visible: proposal → spec → tasks → archived. Keeps planning artifacts co-located instead of as top-level peers.
- **`research/` consolidates** competitive analysis, investigations, and bug write-ups so readers can find "why did we decide X" without digging through the root.
- **`_pipeline/` prefix** pushes doc-generator implementation below the fold. The leading underscore signals "this is plumbing, not docs." Alternative considered: move outside `docs/` entirely into `tools/doc-pipeline/` — rejected because the apps and templates are semantically paired with `guide/` outputs.
- **Pre-compiled output stays committed** because screenshot capture requires a running WinUI host, which is not available on `ubuntu-latest` or `windows-latest` CI runners without a self-hosted agent. Renaming `output/` to `guide/` does not change the publish mechanism (GitHub-rendered Markdown browsing).
- **Plural `specs/`** matches the existing `proposals/`, `tasks/`, `compare/` style and is the community convention.

## 5. Pipeline Code Impact

The doc compiler hardcodes the output path. Affected locations (incomplete — full audit part of the work):

| File | Change |
|---|---|
| `src/Reactor.Cli/Docs/*` (compile pipeline) | `docs/output/` → `docs/guide/`; `docs/apps/` → `docs/_pipeline/apps/`; `docs/templates/` → `docs/_pipeline/templates/` |
| `docs/spec/013-doc-system-design.md` | Update all path references (lines 50, 55, 134, 238, 251, 257, 449, 457–461, 467, 492, 508, 529, 611) |
| `docs/templates/ai-author-skill.md` → `docs/_pipeline/ai-author-skill.md` | Update all path references (lines 11, 14, 16, 31, 78, 81, 496) |
| CI workflows under `.github/workflows/` | Audit for any `docs/output|apps|templates` references (current search found none, but re-check at execution time) |

## 6. Top-Level README Updates

`README.md` links to update:

- `docs/winui3-integration-proposals.md` → `docs/specs/proposals/winui3-integration.md`
- `docs/reference/state-and-hooks.md` (unchanged path)
- `docs/reference/reconciliation.md` (unchanged path)
- `docs/reference/native-differ.md` (unchanged path — file currently missing but referenced)

## 7. New `docs/README.md`

A new index file at `docs/README.md` orients readers:

```markdown
# Reactor Documentation

- **[guide/](guide/)** — User-facing documentation. Getting started, topic guides, screenshots.
- **[reference/](reference/)** — Architecture deep dives for framework contributors.
- **[specs/](specs/)** — Design specs, implementation tasks, and proposals.
- **[research/](research/)** — Competitive analysis, investigations, bug write-ups.
- **[pitch/](pitch/)** — Marketing materials and design targets.
- **[reports/](reports/)** — Point-in-time reports (work summaries, metrics).
- **_pipeline/** — Internals of the doc-generation pipeline. Not reader-facing.

The `guide/` folder is pre-rendered because screenshot capture requires a running
WinUI host. To rebuild it locally, run `mur docs compile`.
```

## 8. Out of Scope

- **CI-based doc regeneration.** Spec 013 describes a CI check that verifies committed `guide/` matches a fresh compile. Wiring that up (probably via a self-hosted Windows runner with WinAppDriver) is a separate effort tracked independently.
- **Gitignoring `guide/`.** Keeping compiled output committed is a deliberate choice tied to the CI constraint above. Revisit only after CI-based regeneration lands.
- **Renaming `reference/`.** The existing name matches convention and contents; no reason to disturb it.
- **Splitting `guide/` into sub-sections** (tutorials vs. how-to vs. concepts, Diátaxis-style). Worth considering, but not this spec — move first, categorize later.

## 9. Execution Plan

Tracked in `docs/specs/tasks/docs-reorganization-implementation.md` (to be written when execution starts). Sketch:

1. Create target directories.
2. `git mv` all paths per Section 3 in a single commit.
3. Update pipeline code references per Section 5 in a second commit.
4. Update `README.md` links and create `docs/README.md` per Sections 6–7 in a third commit.
5. Run `mur docs compile` locally to verify the pipeline still produces byte-identical output under the new path.
6. Run `dotnet build Reactor.sln` and full test pass.

Three commits keep review tractable: a pure rename, a code update, a docs update.

## 10. Resolved Questions

- **`_pipeline/` stays under `docs/`** (not moved to top-level `tools/doc-pipeline/`). Apps, templates, and `guide/` output remain visually paired.
- **`pitch/` stays under `docs/`** (not promoted to top-level `branding/`). Pitch doc links heavily to `guide/` content.
- **`reports/work-summary/` stays committed** (not gitignored + regenerated). Same reasoning as `guide/` — readable from GitHub without running tooling.
