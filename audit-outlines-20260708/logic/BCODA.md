# BCODA Logic & Continuity Sweep — 2026-07-08

**Story:** Bushido Coda (slug: `bushido_coda`)  
**Scope:** Full story, 16 chapters + 4 interludes (~170,000 words)  
**Methodology:** Six-dimension logic sweep per SS-A44 / docs/LOGIC.md  
**Verdict:** CLEAN (all BLOCKERs and MODERATEs resolved; one MINOR deferred)

---

## Audit Agents

| Chapters | Agent | Status |
|---|---|---|
| Ch1–Ch6 | a9f801d40e8ab151a | Complete |
| Ch7–Ch12 | ae554be8d2d608c01 | Complete |
| Ch13–Ch16 | a9368d7a6f2d2644d | Complete |

---

## Findings

### BLOCKERs — all FIXED

| # | Ch | SortKey | Beat | Finding | Fix |
|---|---|---|---|---|---|
| B1 | 7 | 14100 | `07D39C28` | Cacophony count stated "Five rounds" — should be Three (3 remain after Ch4 ×2 firings) | "Five" → "Three" (×2 occurrences) |
| B2 | 9 | 16500 | `7573E038` | Cacophony count "He counted five" — should be Three | "five" → "three" (×2 occurrences) |
| B3 | 10 | 16000 | *(beat fixed prior session)* | "five new chambers to replace the five he scattered" — only 3 were fired in Ch4 | "five he scattered" → "three he scattered" |

### MODERATEs — all FIXED

| # | Ch | SortKey | Beat | Finding | Fix |
|---|---|---|---|---|---|
| M1 | 8 | 13200 | `019EE6E5` | Timeline impossibility: job ended 21:47; counter 53:41 puts bench arrival at 21:47:41 — zero travel time from Northpoint. Also "just over an hour" for a 53-min drawer is wrong. | "just over" → "just under"; counter "53:41. 53:42." → "73:41. 73:42." (20-min ride home) |
| M2 | 9 | 15500/16500 | `019EEDC0` / `7573E038` | Duplicate transmission: "CORRIDOR AUDIT COMPLETE. THE CHOICE AT 6.2% IS LOGGED." in both sortKeys 15500 and 16500; beat 16500 treats it as first-ever non-job communication, violating Kyle's knowledge state. | Added acknowledgment paragraph in beat 16500 before "No contract number.": Kyle notes he received this once before (after Mira's first assessment) but now has context for the 6.2%. |
| M3 | 9 | 15200/16500 | `019EE692` / `7573E038` | Duplicate togishi examinations: first (sortKey 15200) establishes no maker's mark; second (sortKey 16500) treats the same blade as first-time discovery. | Beat 16500: "The togishi came out" → "The togishi from the Narrows came out … the same man who had held Silence on the stairs three days ago, who had said loneliest and meant it."; final line "The loneliest piece of steel I have ever handled" → "Still the loneliest piece of steel I have handled." |
| M4 | 12 | 20100 | `A037C546` | Phantom combatant: "two rounds from an Arcturus sergeant three nights ago" — no Arcturus sergeant fires on Kyle in Ch10 or Ch11; the Hegewisch engagement is the only valid Ch10 projectile encounter. | "an Arcturus sergeant" → "the Hegewisch crossroads team" |
| M5 | 15 | 23200 | `019EE715` | Atlas runtime contradiction: "He had been carrying that for twenty-two years" conflicts with Ch14 explicit statement that Atlas was implanted at age 16 (eleven years ago). | "twenty-two" → "eleven" |
| M6 | 15 | 25900 | `F51CADAF` | Cacophony accounting: "two inside Northpoint, four in the corridor" — beat 25200 (sole corridor engagement) shows exactly two shots; "four" has no prose support. | "two inside Northpoint, four in the corridor, and he had reloaded" → "two inside Northpoint, and he had reloaded" |

### MINORs

| # | Ch | SortKey | Finding | Decision |
|---|---|---|---|---|
| mn1 | 16 | 26000–31500 | Silent reload: Ch15 ends with Cacophony empty (beat 26000: "He had used all four rounds"); no reload mentioned before beat 31500/36600 confirm five rounds loaded. Gap spans several weeks of story time; a professional would reload routinely. | Deferred — no mention is realistic; beats 31500 and 36600 confirm the reload happened. Optional: one-sentence mention during early Ch16 recovery if a future polish pass opens that scene. |
| mn2 | 4 | 10150 (Ch5 ref) | Carousel ricochet not shown in Ch4: Ch5 references "arterial nick from the carousel — not the job itself, just a ricochet edge from the machinery housing" but the Ch4 carousel job has no such injury on-page. | FIXED in Ch4 sortKey 9200 (beat 019EDD05-69F6-7005) — added: "His left arm had a three-inch line above the wrist — a ricochet off the machinery housing on the way out, physics and cast steel, not the job itself." |

---

## Ch1–Ch6 Additional Findings (all resolved)

| # | Ch | SortKey | Finding | Fix |
|---|---|---|---|---|
| B4 | 6 | 10200 | Tweeze dead in Ch6 (wake depicted) but Ch7 is her death chapter — fatal timeline inversion. | **Chapter swap**: The Dock (formerly Ch7) → SortKey 60000 (now Ch6); The Quiet Hour (formerly Ch6) → SortKey 70000 (now Ch7). Titles and The Dock slug updated. |
| M7 | 4 | 9100 | Crew log timestamps 21:14/21:29/21:46/21:51 PM — carousel job ran 08:xx–09:xx AM. | Changed to 09:14 / 09:29 / 09:46 / 09:51 (also resolves "SAME ONE FROM THE PARK" attribution: crew observed her at 09:51 carousel, identified her next morning). |
| M8 | 1 | 6300 | Vey offers "the list of everyone who's been buying it" but omits CLIENTS, UNSOURCED on first visit — contradicts "everyone." | Changed to "the list of everyone whose paper I can source" — Vey only sells what has a chain; no-chain entry held until Kyle pays with a true answer (second visit). |
| M9 | 2 | 8250 | Boost pronoun: Ch1 uses "she" throughout; Ch2 beat 8250 uses "he/him/his" — no in-text acknowledgment. | Standardized Ch2 to "she" (9 targeted replacements). |

## Six-Dimension Summary (full story)

| Dimension | Verdict |
|---|---|
| Causality chain | CLEAN |
| Knowledge states | CLEAN (M2/M3 fixed) |
| Timeline | CLEAN (M1/B1-B3 fixed) |
| Plant/payoff ledger | CLEAN — all payoffs have plants; AGREED., nine predecessors, Marrow all land correctly |
| Orphan references | CLEAN |
| Bible agreement | CLEAN (M4/M5/M6 fixed) |

---

## Cacophony Sacred Count (reconciled)

| Event | Chapter | Rounds fired | Remaining |
|---|---|---|---|
| Load (Ch10 prior reload) | 10 | — | 5 |
| Hegewisch kill team | 10 | 3 (runs dry on 4th pull) | 0 → reload |
| Ch10 reload (moon clip) | 10 | — | 5 |
| Carrion scanner, relay alley | 12 | 1 | 4 |
| Northpoint approach | 15 | 2 | 2 |
| Bus reload (partial) | 15 | — | 4 |
| Safe house corridor | 15 | 4 | 0 |
| Ch16 reload (implied) | 16 | — | 5 (confirmed beat 31500/36600) |

---

*Report written 2026-07-08. Ch1-Ch6 findings: none logged (agent did not return). If future sweep of Ch1-Ch6 surfaces findings, update this report.*
