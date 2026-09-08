# Prose Agent Protocol

Version: `1.0`  
Status: active

The complete legacy inventory, including both project and global Claude commands, skills, and
hooks, is [`command-catalog.json`](command-catalog.json). Refresh it with
`powershell -File tools/export-agent-catalog.ps1` after adding a legacy entry; new commands belong
in `.prose/commands`.

Prose is a Hub-backed canon and prose engine. The database is authoritative and all database
access goes through `Prose.Hub`; an agent must never use direct SQL, EF, or a private copy of the
database.

## Start every session

1. Run `prose agent bootstrap --json` when available. Until that command is installed, run the
   equivalent checks below.
2. Confirm `GET http://127.0.0.1:5900/api/health` is healthy.
3. Select an explicit universe (`--universe <slug>` or `PROSE_UNIVERSE`) before any
   universe-scoped operation. An omitted or invalid scope is an error.
4. Read the returned agent brief and operation catalog. Load only the canon and book context
   needed for the current task; Dynamic Context Memory is assembled by Prose for beat generation.
5. Check provider readiness with `prose --provider-status --json` before spending an LLM-backed
   operation.

## Operating rules

- The hierarchy is Book → Chapter → Beat. Chapters never contain chapters.
- Generated canon and node markdown are read-only mirrors. Edit their database source through a
  sanctioned operation and regenerate the mirror.
- Reads, deterministic validation, and reports are allowed by default.
- A durable prose, canon, entity, relationship, or structural mutation must first be represented
  as a change proposal. Present the exact target, old value, new value, rationale, and verification
  plan to the human.
- Apply a proposal only after the human has approved it through the local interactive approval
  command. Never treat an agent's own statement as approval.
- Use the existing WriteGate, cost gate, command ledger, findings, temporal history, and
  convergence checks. A successful tool response is not proof that the requested change was
  applied: verify the resulting state through Hub-routed reads.
- For an unresolved command or data gap, report the gap. Do not invent a SQL workaround.

## Canon and prose workflow

For a new or materially changed book, work in this order: resolve universe; inspect the book
outline and chapter synopsis; verify entities and relationships; prepare the construction
blueprint; generate or edit beats through the ProseWriterRouter path; run deterministic checks;
run Reader-Proxy QA and the logic sweep; repair named findings with minimal edits; repeat until
the convergence gate is satisfied; then export.

At beat scope, the engine loads the universal engine rules, base craft, universe craft, book
outline, applicable entity records, narrator register, blueprint slice, and recent beat window.
Do not paste the entire corpus into a prompt to bypass this process.

## Transport-neutral operation envelope

Every adapter should preserve this envelope, whether it is MCP, CLI, or HTTP:

```json
{
  "protocolVersion": "1.0",
  "operation": "read_context",
  "universe": "glmz",
  "arguments": {},
  "requestId": "client-generated-id",
  "approvalGrant": null
}
```

Responses must identify `requestId`, operation, universe, status, result, warnings, findings,
ledger identifiers, and provider/cost metadata when applicable. Errors are structured and must
name the failed precondition (`hub_unreachable`, `missing_universe`, `unknown_argument`,
`approval_required`, `write_gate_rejected`, `provider_unavailable`, or `verification_failed`).

## Handoff

Save resumable work through the Hub-routed agent-session operation. A handoff records the task,
universe, node, decisions, inspected evidence, proposed changes, last verification, and next
action. Do not depend on Claude hook state, editor memory, or an untracked transcript.
