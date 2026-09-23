---
description: End the factory session — record what was done, every decision (as a ruling or work order), and what comes next. Replaces the old paper-transcript quicksave.
argument-hint: "[optional note to emphasize what matters most]"
allowed-tools: Write, PowerShell, Bash
---

# Quicksave — end the factory session (RFC 0015)

The session's memory is not a markdown file any more. It is a `FactorySessions` row in the Hub,
opened by the SessionStart hook, and this command closes it. The next session's start hook shows
this summary automatically, next to the factory's live next action.

## Do this now

1. **Record every decision first.** For each decision made this session (an author ruling, a
   change of plan, a book request), make sure it exists in the factory:
   - an author ruling or book law → `record_ruling` (law / metric / incidental), or the entity
     record itself (`set_character_fields` / `create_*`) with a read-back;
   - new work, or a plan change → `work_order_add` (engine or author, under an approved root).
   Note each id.
2. **Write the summary** to a scratch file (your scratchpad, never the repo):

   ```json
   {
     "done": ["one line per completed thing, with its evidence (commit, order id, counts)"],
     "decisions": [{ "text": "what was decided", "rulingId": "<guid>" }, { "text": "…", "orderId": "<guid>" }],
     "next": "the single next step, in one line"
   }
   ```

   `$ARGUMENTS`, if given, goes into `next` or `done` as the user meant it.
3. **End the session:** `prose --session end --file <summary.json>` (MCP: `session_end`).
   - If it is refused, it lists each decision that is not backed by a ruling or order recorded
     this session. Record those (step 1) and run it again. Do not drop a decision to get past it.
4. Tell the user in one line that the session is saved, and what `next` says.

The old `.prose/quicksave.md*` transcripts are archived in `.prose/archive/` and no longer used.
