# SPRW Structural Fix Plan — Bible Resync + Beat Patches

Slug: `the-number-that-works-019ed367`. Score 95.0 (corpus flagship). Minimal-touch discipline
throughout — every change below is the smallest edit that closes the finding. **No database
writes were made.** This file is the edit list for `docs/nodes/SPRW.md`; the main thread
applies it. Patched beat files live alongside this plan in `beats/*.md`.

---

## Part 1 — Bible edits (`docs/nodes/SPRW.md`)

### Edit 1 — stale header status/score

**Old (frontmatter, line 7):**
```
status: complete (standalone two-minds novelette; SS-A7 realized; reviewed 87.0)
```

**New:**
```
status: complete (standalone two-minds novelette; SS-A7 realized; reviewed 95.0)
```

**Old (frontmatter, line 8):**
```
updated: 2026-06-23
```

**New:**
```
updated: 2026-07-03
```

**Rationale:** SPRW-US-5 records the 87.0 review; the strand has since been revised upward to
95.0 (current audit baseline). The header is the first thing anyone reads and it's currently
citing a superseded score.

---

### Edit 2 — §5 rule 10 (the locked ending)

**Old:**
```
10. The story ends on the balcony, no broadcast, no thriller turn. He keeps the comm in his hand,
    stands in the cold air, does not name the number, keeps his count. LOCKED.
```

**New:**
```
10. The story ends at his desk, not the balcony (revised 2026-07-03 — see beat 4232, "The
    Window"; supersedes the earlier balcony-ending draft this rule originally described). He
    types the number — 400, his standing rate — into the RATE field, holds his finger over
    submit for the length of one ventilation cycle, then clears it and leaves RATE: OPEN
    standing. No broadcast, no thriller turn: the refusal to name the number is the same
    restraint the balcony scene once carried, relocated from watching the sky to the work
    itself. He keeps the field coat on and opens the first of the six coordinates to begin
    fieldwork. LOCKED.
```

**Rationale:** Finding 3. The balcony/"comm in hand"/"keeps his count" imagery is real in the
text — it's beat 4117, the end of Movement II (the first, unanswered contact attempt) — but it
is not how the story now ends. The realized final beat (4232) is entirely desk-bound and, per
the audit's own read, "arguably stronger — active, forward-moving, consistent with the wound
arc." This edit re-points the lock at what's actually on the page instead of retconning the
prose back toward a superseded draft.

---

### Edit 3 — §6 Movement III closing beat description

**Old:**
```
the record **"for whoever
comes after"** and her first unqualified **yes**; **the eleven days**; the **`RATE: OPEN`** work
order; *Downstairs* (he walks three blocks, eats eggs, uses the front door for the first time); **The
Window** — the balcony, the comm dark, the count kept.
```

**New:**
```
the record **"for whoever
comes after"** and her first unqualified **yes**; **the eleven days**; the **`RATE: OPEN`** work
order; *Downstairs* (he walks six blocks, eats eggs, passes the lobby's terminal doorman); **The
Window** — back at his desk, the coat still on: he types 400 into the rate field, clears it,
leaves `RATE: OPEN` standing, and opens the first of six coordinates to start fieldwork.
```

**Rationale:** Same drift as Edit 2, plus a small factual slip caught in the same sentence:
beat 4231 ("Downstairs") has Elias walk **six** blocks, not three ("Six blocks. Eggs. Coffee
going half-cold."). Fixed in the same pass since this line was already being rewritten.

---

### Edit 4 — §5 rule 4 (Tadesse's locked line)

**Old:**
```
4. Tadesse's "it doesn't estimate." LOCKED.
```

**New:**
```
4. Tadesse's assessment of Sparrow's attention (beat 4113, LOCKED): *"I think it measured
   everything it could reach... I can't say whether that is the same as noticing."* (Supersedes
   an earlier draft phrasing, "it doesn't estimate," which is not present in the realized text —
   see character-rules §3 as well, which should be updated to match — the actual quoted line is
   already correct there.)
```

**Rationale:** Finding 4. Checked the realized text (beat 4113) for a close variant before
retiring the lock — one exists, and it's better than the phrase it's replacing (it does the
same job — Tadesse's careful agnosticism about whether Sparrow's precision amounts to
noticing — with more texture). §3's character-rules section already quotes this correct line
("His 'it doesn't estimate' / 'I think it measured everything it could reach…' lines are
LOCKED" — the character-rules entry is half right, half stale); recommend also trimming the
stale "it doesn't estimate" fragment out of §3-support when this edit lands, so the two
sections agree. That §3 line reads today:

> **Tadesse Bekele-Sørensen:** Ethiopian-Danish facility caretaker. 20 years under the contract. His
> "it doesn't estimate" / "I think it measured everything it could reach… I can't say whether that is
> the same as noticing" lines are LOCKED.

**§3 new line:**
> **Tadesse Bekele-Sørensen:** Ethiopian-Danish facility caretaker. 22 years under the contract
> (see beat 4113's "twenty-two years," not 20 — minor drift, fix alongside this edit). His line
> "I think it measured everything it could reach… I can't say whether that is the same as
> noticing" is LOCKED.

(Flagging the 20-vs-22-years mismatch as a bonus catch while in this section — beat 4113 is
explicit and repeated: "Twenty-two," Tadesse says. "The contract renewed automatically." The
bible's §3 says "20 years" twice. Small, free fix, same sentence being touched.)

---

### Edit 5 — §5 rule 6 (retired by revision)

**Old:**
```
6. "The third is the most complete explanation. That is what worries him about it." LOCKED.
```

**New:**
```
6. RETIRED BY REVISION. The original line does not appear anywhere in the realized 58-beat
   text (checked; no close variant exists — this is not a citation gap, the line was cut). The
   beat's function — Elias arriving at the systemic "third answer" (not person, not crew, a
   standing system) and marking it as the reading he is least willing to be wrong about — is now
   carried by beat 4098's closing line: *"A system. Running since 2189,"* written "in smaller
   letters, the way he writes the things he is least willing to be wrong about." Cite that line
   if this beat needs a locked quote going forward.
```

**Rationale:** Finding 4 (second half). Unlike Tadesse's line, no close variant of the original
quote exists anywhere in the text — confirmed by full-text search of all 58 beats. Rather than
force a citation that isn't there, this marks the lock retired and points at the line that
actually does the job today, per the task's instruction ("if no, mark those locks as
retired-by-revision").

---

## Part 2 — Beat patches (files in `beats/`)

| File | Beat | Fix | Scope |
|---|---|---|---|
| `beats/4198.md` | 4198 "The First Word" | `CLUSTER-6` → `CLUSTER-1` in the selection dossier's cross-ref field | 1 term |
| `beats/4199.md` | 4199 "Why Him" | `CLUSTER-6` → `CLUSTER-1` in the residence-proximity criterion | 1 term |
| `beats/4909.md` | 4909 (untitled) | (a) "two certified items" → "the certified item" (singular — only Makena's piece exists at this SortKey position); (b) drop the "2187" figure — Elias registers the satellite as "long dark" without the year that later lands as the Movement II shock | 1 paragraph |

Full rationale, exact old/new lines, and complete patched beat text are in each file's header
and body. All three patches are register-consistent with Elias's live voice (precise,
inventory-minded, no editorializing) and change nothing else in their beats.

---

## Part 3 — Sanity check: beats 4908 and 4969 (Finding 1 follow-up)

The audit flagged that `Beats.Number` for 4908, 4909, and 4969 lands far from their true
reading position (`NodeBeats.SortKey`) — a landmine for any tool that sorts by `Number` instead
of `SortKey`. That's a data-hygiene finding, not necessarily a content finding. Checked both
remaining beats' **content** against their actual `SortKey` position:

**Beat 4908 (SortKey 2675.0)** — sits between beat 4230 "He Files It" (2650.0, files the RATE:
OPEN record and Siosaia note) and beat 4231 "Downstairs" (2700.0, walks out to breakfast). Content:
Elias sits with the open rate field, reasons through why Sparrow left it blank, decides to go
outside instead of pricing it, doesn't reach for the pill case, puts on the field coat, walks out
the door toward the elevator. This is exactly the connective tissue between "He Files It" and
"Downstairs" — no leaked reveal, no anachronistic detail, no inventory error. **Verdict: no patch
needed.** The Number field (4908) is simply disconnected from its SortKey-true neighbors (4230,
4231); that's the `Number`-vs-`SortKey` data problem the audit already flagged in Finding 1, not a
prose problem.

**Beat 4969 (SortKey 337.5)** — sits between beat 4909 (325.0, Day One evening, tea/registry
dead-ends) and beat 4095 (350.0, Leandro's Day Two call). Content: the front-door credentials
attempt — Elias submits a formal documentation-access request against the 2213 file through his
own still-active Cordon Freight credentials, and the session is closed live, mid-submission, by
"the administrator." This fits its Movement I position cleanly (it's the second escalation after
4909's dead-end registry queries, and it sets up "they know about you" in 4095) and contains no
premature reveals or count errors. **Verdict: no patch needed.** Same Number-field artifact as
4908 — its Number (4969) is nowhere near 4909/4095's Numbers, but its SortKey position and its
content agree.

Net: only beat 4909 required a content patch. 4908 and 4969 are correctly slotted by SortKey and
their prose matches that slot; they're only "wrong" if something reads `Number` as reading order,
which is a tooling/export risk already captured by Finding 1 and not something a prose patch can
fix.
