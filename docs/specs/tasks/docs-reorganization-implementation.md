# Docs Reorganization — Implementation Tasks

Derived from: [`specs/021-docs-reorganization.md`](../021-docs-reorganization.md)

## Commit 1 — pure `git mv`

- [x] Create target parents: `research/`, `pitch/`, `reports/`, `_pipeline/`
- [x] `docs/output/` → `docs/guide/`
- [x] `docs/spec/` → `docs/specs/` (with `archived/` preserved)
- [x] `docs/tasks/` → `docs/specs/tasks/`
- [x] `docs/proposals/` → `docs/specs/proposals/`
- [x] `docs/winui3-integration-proposals.md` → `docs/specs/proposals/winui3-integration.md`
- [x] `docs/critical-review.md`, `docs/flux-ui-analysis.md` → `docs/research/`
- [x] `docs/compare/` → `docs/research/compare/`
- [x] `docs/investigation/` → `docs/research/investigations/`
- [x] `docs/bugs/` → `docs/research/bugs/`
- [x] `docs/reactor-pitch.md`, `docs/bentobox.svg`, `docs/design-targets/` → `docs/pitch/`
- [x] `docs/worksummary/` → `docs/reports/work-summary/`
- [x] `docs/apps/`, `docs/templates/` → `docs/_pipeline/`
- [x] `docs/_pipeline/templates/ai-author-skill.md` → `docs/_pipeline/ai-author-skill.md`

## Commit 2 — pipeline code and internal references

- [x] `src/Reactor.Cli/Docs/CompileCommand.cs`: `apps`, `templates`, `output` → `_pipeline/apps`, `_pipeline/templates`, `guide`
- [x] `docs/specs/013-doc-system-design.md`: tree diagram and path refs
- [x] `docs/_pipeline/ai-author-skill.md`: path refs
- [x] `dotnet build src/Reactor.Cli/Reactor.Cli.csproj` clean, 0 warnings

## Commit 3 — top-level README, index, spec status

- [x] Top-level `README.md`: update 3 doc links
- [x] `docs/pitch/reactor-pitch.md`: update broken relative links to specs/
- [x] Create `docs/README.md` index
- [x] Flip `021-docs-reorganization.md` status Draft → Accepted — executed
- [x] Resolve Open Questions section with user-confirmed answers

## Out of scope (per spec §8)

- CI-based doc regeneration (separate effort, needs self-hosted Windows runner)
- Gitignoring `guide/` or `reports/work-summary/`
- Renaming `reference/`
- Splitting `guide/` into Diátaxis-style sub-sections

## Historical references not rewritten

Task files under `specs/tasks/*-implementation.md` and `specs/018-namespace-rename.md`
contain path references to pre-reorg locations (e.g., `docs/spec/…`, `docs/output/…`).
Following the precedent set by spec 018, these are preserved verbatim as historical
record of what was done at the time.
