# Snapshot / diff primitives — deferred (spec 024)

**Status:** deferred — no code in v1.
**Reference:** [spec 024 §10](../024-ai-agent-devtools.md), [tasks §3.9](../tasks/ai-agent-devtools-implementation.md#3.9).

## Why deferred

The spec proposes server-side `reactor.snapshot` and `reactor.diff` primitives that
would let an agent capture a tree + state bundle and ask the runtime to diff two
snapshots (structural, text, bounds, hook values). The goal is to avoid
transporting two full trees across the MCP boundary when the agent only cares
about the delta.

We deferred shipping these in v1 because:

1. **No usage signal yet.** The Phase 2 tool set gives agents `tree`, `state`,
   and `screenshot` as raw materials. Until agents are actually using those on
   real tasks, we don't know which diff shapes are worth supporting — naïve
   text-diff? bounds-moved? hook-value-changed? All three?
2. **Agent-side diffing is usually good enough.** For a 200-node tree the
   response is small. Two tree calls + a client-side comparison is within the
   budget for a human-in-the-loop session.
3. **Persisting snapshots server-side has a storage question.** If a snapshot
   is large (PNG + full-view tree + state), keeping more than a handful per
   session adds memory pressure or forces disk spill, and we'd need to decide
   whose session owns it.

## Signal we're watching for

Ship a first pass once **any** of the following is clear from v1 usage:

- Agents are repeatedly fetching two full trees and diffing locally for the
  same narrow question (e.g. "did this Text node's content change?") —
  suggests a targeted `diff` endpoint would cut round-trip cost.
- The same snapshot is being referenced multiple times in a session (e.g.
  "compare the screenshot from 30 seconds ago against now") — suggests
  server-side retention is worth the complexity.
- A concrete third-party agent framework builds its own snapshot wrapper on
  top of our raw tools — we should adopt their shape rather than invent one.

If none of those signals surface after a release cycle, keep it deferred.

## Non-goals during the wait

- Don't add a `reactor.snapshot` stub that returns "not implemented." Agents
  discover tools via `tools/list`; better to omit than to tease.
- Don't ship a client-side snapshot helper either — clients should remain free
  to build their own over the raw `tree`/`state`/`screenshot` calls.
