# StreetSamurai Project Rules

## Conversation
- A bare "do" / "do it" / "yes" from the user means "continue", "keep going", "proceed". Resume the current task without asking for clarification.

## Rate Limit & Context Protection

### Rate Limit (billing — HARD STOP at 96%)
See global rules in ~/.claude/CLAUDE.md. The rate-limit-monitor skill enforces:
- Warn every 5% starting at 80%, every 1% starting at 91%
- **Hard stop at 96%** — queue pending tasks to ~/.claude/rl-queue.json, write handoff to ~/.claude/rl-handoff.md
- Every exceeded limit = ~$30 charge on the credit card

### Context Window (conversation — HARD STOP at 96%)
- When approaching 96% context usage, STOP immediately
- Create a task list of all pending/in-progress work
- Write a handoff summary to memory so the next session can resume seamlessly
- Tell the user to take a break and come back after cooldown

## Code Style
- Do NOT use underscore-prefixed variables (e.g., `_myField`). Use `camelCase` for private fields without the underscore prefix.
- JSON only for all data files. No Python scripts, no YAML, no Markdown files except README.
- Web-only project (Blazor Server). No MAUI host.

## World Rules
- The symbol Φ is the QUANTA currency symbol. It is NEVER the Greek letter phi.
- Iowan Behemoths are autonomous machines, NOT synthetic life. They are not alive.
- Default to mixed heritage from unexpected global combinations (Ubiquitous Diaspora).
