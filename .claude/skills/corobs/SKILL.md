---
name: corobs
description: Turn COROBS (RFC 0016, the Correlithm-Object entity geometry) on or off, or show its status. Usage /corobs on | off | status; no argument = status. Off hides the whole new way everywhere and deletes nothing; on restores it with no rebuild.
---

# /corobs on | off | status

The master switch for RFC 0016 (ticket PRS-T863). It is one Hub setting, `CorobsEnabled`
(Vault, group Hub), default **off**, read live by the running Hub.

- `/corobs on`  → `prose --universe glmz --corobs enable`
- `/corobs off` → `prose --universe glmz --corobs disable`
- `/corobs` or `/corobs status` → `prose --universe glmz --corobs status`

Run exactly one of those (one `prose` call), then say in one plain sentence what the switch is now and,
for status, how many points, patterns, rules and open hits the universe has.

What the switch means:
- **Off:** every `--corobs` verb and every `corobs_*` MCP tool answers `disabled` and reads or writes
  nothing; the wiki's Corobs panel is absent; no Corobs row is deleted.
- **On:** the store is visible again exactly as it was. Nothing needs rebuilding.
- The old way (`--graph-query`, `get_neighbors`, the wiki's Related panel, `get_character`) is the same
  with the switch on or off.

The rest of the new way, once on (protocol H, RFC 0016 §5): `--corobs build` → read `--corobs patterns`
and `rules` → confirm each true one with the ruling that records it (`--corobs confirm --pattern <id>
--ruling <id> --label "…"`, `--corobs confirm --rule <id> --ruling <id>`) or `--corobs reject` it →
`--corobs cue` → fix each hit by the normal protocol → `--corobs resolve --hit <id> --order <id>` →
rebuild (hits a fix healed close by cascade). `--corobs explain --entity <x> [--at <date>]` shows one
entity's place in the geometry.
