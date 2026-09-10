# do

Typing a bare `do` (optionally `do it`) restores the last `/quicksave` transcript, the same as
running `/quickload`. A prompt-submit hook (`.prose/hooks/quickload-on-do.ps1`) reads
`.prose/quicksave.md`, injects it as authoritative resume context for the model, and deletes the
file (one-shot — it will not refill on its own). See `.prose/commands/quicksave.md` and
`.prose/commands/quickload.md` for the full mechanism.

Any other prompt passes through untouched — this only fires on an exact bare `do`/`do it`.
