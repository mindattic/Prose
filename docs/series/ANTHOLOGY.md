# ANTHOLOGY — Isolated-Anthology Coordination Board

> ANTHOLOGY is not a connected universe in the GLMZ/SCRY sense — it is a container for
> deliberately **unconnected** stories, each written as a distinct fictional Author persona whose
> biography causes their prose style. The shared "prompt" every ANTHOLOGY story answers is a single
> anthology theme; each Author-filter refracts that theme into a wholly separate story. No
> character, place, entity, or plot fact crosses between ANTHOLOGY books — this is the load-bearing
> constraint, not an oversight. There is deliberately no arc ledger, plant/payoff registry, or
> world-revelation sequencing section here, because none of that applies across isolated stories.
>
> This universe is not scoped to one submission target — it's the standing home for any future
> batch of author-isolated, unconnected short stories, whichever anthology or market they're
> aimed at. The current batch below happens to target one specific market.

## Current anthology target

**Easton Tales Publishing — "Enshrouded: A Horror Anthology."** Theme: something buried, hidden,
or concealed, and the darkness that follows when revealed. 2,000–6,000 words. Deadline
2026-08-31. **Their submission rule: no AI-generated content.** These four stories are a craft
experiment in authorial-persona isolation (Stephen King/Richard Bachman model) — see the note in
each brief; whether/how any of them could honestly go back to Easton Tales is a separate,
unresolved question from writing them.

## Story Roster

| Code | Title | Author Persona | Bio (age/sex/location) | Theme reading | Status |
|---|---|---|---|---|---|
| GRAVE | The Warm Ground | Corwin Ashby Teale | 66, M, rural NC | Literal burial — the land remembers | Drafted (7 beats, exported) |
| ALIAS | Collection | Nadia Kessler | 43, F, Philadelphia | Concealed identity — a debt collected on a false name | Drafted (7 beats, exported) |
| HEIRS | The Room That Was Never Locked | Eamon Bellhaven Cray | 58, M, Wiltshire, England | Hidden family secret — an inherited unstated rule | Drafted (8 beats, exported) |
| ECHO | Two Streets Over | Priya Okonkwo-Lindqvist | 31, F, raised Lagos/Malmö/Toronto | Suppressed memory — a dream converging on reality | Drafted (6 beats, exported) |

## Isolation constraint (binding)

- No two ANTHOLOGY stories may share a character, place, faction, or named entity — every entity is
  seeded with `originNodeSlug` scoped to its own story.
- No cross-story plants/payoffs. No shared timeline. No shared arc.
- Each story's `NodeBible` carries its own Author Persona's biography and style contract as a
  hand-authored voice-register section — that section is the enforcement mechanism (DCM tier 3,
  evicted on book change), not a convention Claude has to remember to apply by hand.
- A future fifth ANTHOLOGY story is free to invent a fifth unrelated Author persona; it must not
  reuse any of the four above or any entity from GRAVE/ALIAS/HEIRS/ECHO.
