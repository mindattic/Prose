---
description: Execute RFC 0007 — Universe Interchange & the EVE universe (handoff from ExperimentEve)
---

# /eve — build the Universe Interchange

This is a handoff from the ExperimentEve session. The complete, Prose-convention-
aware plan is **docs/rfc/0007-universe-interchange.md**. Read it fully, then
execute Phase 1 end to end (steps 1–8, in order), respecting every hard rule in
CLAUDE.md (Hub-only DB access; Book→Chapter→Beat untouched; additive only —
no existing command, tool, table, or behavior changes).

Key inputs:
- Seed data (75 validated entities): `D:\Projects\MindAttic\ExperimentEve\universe\eve.universe.json`
- Schema (copy to docs/schemas/): `D:\Projects\MindAttic\ExperimentEve\universe\universe.schema.json`
- The consumer side is already installed in ExperimentEve:
  `npm run universe -- push|pull` and a UserPromptSubmit hook draining
  `GET /api/outbox/eve` — your job includes making those real (import CLI flag,
  snapshot already exists, outbox endpoints per RFC step 5).

Work milestone-by-milestone with tests (`dotnet test v3/Prose.UnitTests`) and
commit directly to the default branch as you complete each RFC step. When done:
1. Verify the acceptance list at the bottom of the RFC.
2. Enqueue a first outbox event for consumer `eve`:
   kind `hello`, summary "EVE universe live in Prose: 75 entities. Pull when ready."
3. Mark the RFC implemented (note any deviations).

If anything in the RFC conflicts with an existing Prose law or convention, the
law wins — deviate minimally and record the deviation in the RFC. Phase 2
(GDD / game script / barks export / prequel novella books) is design-approved
but starts only after Phase 1 acceptance passes.
