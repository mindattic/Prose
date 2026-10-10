---
name: upgrade-book
description: Bring one existing book into the plan-first format (RFC 0015 protocol G). For every prose beat, restate its Description against the prose as it stands, by hand, one chapter at a time, until strict F3 passes; bind unbound plant/payoff edges; then record the plan-first gate on the book. Prose is never touched. Usage /upgrade-book <slug>.
---

Canonical definition: **RFC 0015 §7 protocol G** (`docs/rfc/0015-novel-factory.md`), including its lessons. Follow that. This file is the working checklist.

**Hard rules**
- The prose is not touched. A beat that cannot be described honestly is a Rule #1 defect: use `add_read_note` (kind defect) and leave it for its own pass.
- No bulk or generated pass: no loop tool, no `write_synopsis`, no LLM service. Every Description is written in session by reading that beat whole, in its chapter's context (Law 6).
- Always pass `--universe`. All writes go through the Hub. Read back every write.

**Per book**
1. **Measure.**
   - Run `prose --universe <u> --beat-index --slug <slug> --json` to get each beat's Description trust state (ok / stale / missing).
   - Run `factory_status` for F2, F3, F4 and F6.
   - Run `get_plant_payoffs` on the book and its series to find unbound ends.
2. **Read canon first.**
   - Load the entity records of the POV characters and of anything the beats name (`get_character`, `get_place`, `get_faction`).
   - Do not read old outlines; the book is its beats (Law 1).
3. **Per chapter, in order:**
   1. Read the whole chapter (`read_beats` for the unit range), with the prior chapters' Descriptions in view.
   2. Do capture first (F4): run `factory_capture`, then pin with `--pin-name` (longest names first), use incidental rulings or `find_entities` before creating anything, and reword sentence-start false names. Tags live in `Beat.Text`, so doing this before reconciling avoids re-staling.
   3. For every prose beat whose Description is not current, call `update_beat_metadata(description)`. Say who is present, what happens and changes, and what the beat PLANTS or PAYS OFF, by chapter. Restate what is on the page; never plan anew.
   4. Keep Descriptions that are already current.
   5. Confirm the chapter with `factory_status`: F3 under the gate must pass.
4. **Edges.**
   - Bind every plant/payoff that has a missing end (`link_plant_beat` / `link_payoff_beat`), or bring it to the author.
   - Register the arcs the read surfaced.
   - Cross-book links between standalone GLMZ books stay texture, not edges, unless the author rules otherwise.
5. **Lock it.** When every unit passes F3 and the F7 edge check is clean, call `record_ruling(kind: gate, text: plan-first)` on the book, so it cannot regress.

**Traps** (from writing CYB1-3)
- **Re-stamping hazard.** Re-sending an unchanged Description stamps it "current", whether or not it was ever reconciled.
  - Only re-stamp beats you reconciled before a tag change.
  - Only re-stamp by chapter range, never book-wide.
- **Retags stale beats.** A retag (`--retag-name`) stales every beat it touches; re-stamp those beats afterwards.
- **Common-word aliases.** Never pin an alias that is a common word (One, Seven, Read).
- **Name collisions.** The universe holds about 13k names; check with `find_entities` before naming anyone.

**Session end:** `/quicksave`, with every decision referencing a ruling or order id.
