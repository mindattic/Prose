# DEPRECATED — MCP project

This MCP server project has been retired. All tool surfaces are now standardized
through the CLI in `StreetSamurai.Blazor`:

- `ss --book` ......... book/chapter operations
- `ss --continuity` ... continuity store (claims, contradictions, resolve)
- `ss --findings` ..... findings inbox
- `ss --refine-story` . refinement notes
- `ss --repair` ....... dossier-driven story repair (timelines, knowledge, conditions)
- `ss --ask` .......... corpus-grounded RAG against the local Ollama
- `ss --story-write` .. autonomous chapter generation

Move this folder out of `v3/` (e.g. `archives/StreetSamurai.Mcp/`) at your
convenience — nothing else in the codebase references it.
