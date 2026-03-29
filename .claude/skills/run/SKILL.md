---
name: run
description: Run the StreetSamurai multi-facet character system. No arguments needed.
---

When invoked:

1. If arguments contain a scene goal (e.g., `/run Scene 2: Kael walks through the city`):
   - Run: `python -m src.main new-session --scene-goal "<the goal>"`
   - Then generate 3 beats automatically using `python -m src.main beat --session <path> --goal "<beat goal>"`
2. If no arguments, show the available commands:
   - `python -m src.main list-facets` — Show all character facets
   - `python -m src.main show-character` — Show character summary
   - `python -m src.main new-session --scene-goal "..."` — Start a new session
   - `python -m src.main beat --session <path> --goal "..."` — Generate a beat
   - `python -m src.main export --session <path> --scene-number N` — Export to canon
3. Stream the output so the user can see progress and any errors
4. If it fails, summarize the error and suggest a fix

The old dual-writer scripts are archived in `backup/`.
