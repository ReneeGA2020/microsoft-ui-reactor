# Reactor Devtools — MCP Surface

This folder implements the `--devtools` runtime path: the MCP server that
exposes `reactor.*` tools (tree, screenshot, click, state, fire, …), the
`mur devtools` supervisor, and the rolling log that records every call.

Reference specs: [024-ai-agent-devtools.md](../../Docs/specs/024-ai-agent-devtools.md),
[task list](../../../../docs/specs/tasks/ai-agent-devtools-implementation.md).

## Security model

The devtools surface is **developer-loop only**. It is not designed to be a
production endpoint and ships with three hard gates:

1. **Opt-in at the app level.** `ReactorApp.Run(..., devtools: true)` is the
   only way to start the MCP server, the capture server, or the logger. When
   `devtools: false` (the default), no extra listeners are created.
2. **`#if DEBUG` in the scaffold.** `mur new` generates a call with
   `, devtools: true` wrapped in `#if DEBUG`, so a Release build of the
   template has no devtools surface at all.
3. **Loopback-only binding.** The MCP `HttpListener` binds to
   `http://127.0.0.1:{port}/`. It is never exposed on any non-loopback adapter
   and the `ScreenshotCapture` path only reads the local window.

### Caveat: any local process can connect

There is **no authentication on the MCP port in v1**. Any process running on
the same machine as the app — including unrelated applications, browser
tabs connecting over `fetch("http://127.0.0.1:NNNN/mcp", …)`, other user
sessions with local access, etc. — can call every registered tool. That is
acceptable for the dev-inner-loop use case (a human running `mur devtools`
with an agent in the same terminal session), but it means:

- **Do not run `mur devtools` in an environment with untrusted local
  processes.** `reactor.fire` can reach any handler on the live component;
  `reactor.state` can enumerate hook shapes; `reactor.click` can drive the UI.
- **Do not enable `devtools: true` in Release builds that ship to end users.**
  The scaffold's `#if DEBUG` guard makes this hard to do by accident.
- **Do not assume the MCP surface is safe to expose beyond localhost** — e.g.
  via a reverse proxy, SSH tunnel with remote binding, or container port
  forwarding to 0.0.0.0. Loopback is the only supported deployment.

If v1 usage signals push us toward scenarios where these caveats bite
(e.g., CI runners, remote pair-programming), we revisit with an auth story
in a follow-up spec.

## Observability

`DevtoolsLogger` writes one line per tool call to
`%LOCALAPPDATA%/Reactor/devtools/{pid}.log` (Windows) or
`$XDG_STATE_HOME/reactor/devtools/` (non-Windows). Files roll at 10 MB
and we keep the newest five archives. Log level is configured via
`--devtools-log-level off|error|call|trace` (default: `call`).

Line shape (tab-separated):

```
2026-04-18T12:34:56.789Z	tree	r:main/btn-inc	42ms	ok	0
```

Columns: UTC timestamp, tool name, selector (or `-`), latency, `ok`/`err`,
JSON-RPC result code.

## `--print-config`

`mur devtools --print-config [--mcp-port N]` emits JSON fragments for
Claude Code, VS Code, and GitHub Copilot MCP configs, parameterized with
the requested port. The tool never writes to disk — the user pastes the
fragment they want into the target config file themselves.
