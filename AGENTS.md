# Prose agent entrypoint

This repository is operated through the provider-neutral Prose agent protocol. Read
[`docs/agent/PROSE_PROTOCOL.md`](docs/agent/PROSE_PROTOCOL.md) before using the CLI or MCP.

The protocol is the source of truth for universe scope, read/write permissions, approvals,
cost gates, context loading, verification, and handoff. Client-specific instruction files may
point here, but must not duplicate those rules.

Portable prompt aliases such as `/do`, `/quicksave`, `/quickload`, `/progress`, and `/show` are
defined in [`docs/agent/PROMPT_COMMANDS.md`](docs/agent/PROMPT_COMMANDS.md) and run through
`tools/prose-agent.ps1`; they are not Claude-only commands.
