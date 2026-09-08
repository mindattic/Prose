# Claude-layer migration map

The former `CLAUDE.md` and `.claude/` tree were useful training material, but they mixed four
different concerns. The portable replacement keeps the behavior while removing the dependency on
Claude hook and command syntax:

| Former material | Portable destination |
|---|---|
| Structural, database, canon, universe, DCM, and approval laws in `CLAUDE.md` | `PROSE_PROTOCOL.md`, canon docs, and generated operation metadata |
| `/dcm`, `/show`, `/progress`, and reader/logic workflows | Curated catalog operations plus CLI/MCP workflow documentation |
| `/quicksave`, `/quickload`, and `UserPromptSubmit` injection | Hub-routed agent session handoff and `agent bootstrap` |
| MCP build/start hooks | Native MCP client configuration using `docs/agent/CLIENTS.md` |
| Post-edit validation hooks | Hub-routed verification operations and explicit protocol checkpoints |
| Commit, discard, and revert skills | Host-neutral repository workflow instructions; mutations remain human-approved |

During rollout, the legacy files remain available as historical reference. New work must begin at
`AGENTS.md`; no new rule may be added only to a Claude-specific file.
