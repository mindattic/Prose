# Essence System

The Essence system is the interconnected YAML network that defines every entity in the Street Samurai world. Characters, places, factions, technology -- everything has an Essence file that describes its properties, motivations, and connections to other Essences.

## What is an Essence?

An Essence is a single YAML file that defines one entity in the world. It can be a character, a place, a faction, a piece of technology, or any other narratively significant thing. Each Essence contains structured data that the story generation engine uses to build rich, consistent scenes.

## Creating a new Essence

Add a YAML file anywhere under the `essences/` directory. The system recursively loads all `.yaml` files from the entire tree.

Every Essence file must have at minimum:

```yaml
type: character    # or: place, faction, technology, setting, npc, district, etc.
name: "Entity Name"
description: |
  A paragraph describing the entity.
```

### Optional common fields

```yaml
aliases: ["nickname", "alternate name"]
location: "Meridian"
faction: "Iron Lotus"
factions: ["Iron Lotus", "The Circuit"]
relationships:
  - name: "Other Entity"
    type: "ally"
speech_patterns: "Short sentences. Never uses contractions."
atmosphere:
  sights: "Neon reflections on wet pavement"
  sounds: "Distant maglev hum, street vendors"
  smells: "Ozone and frying oil"
facet_weights:
  wound: 0.8
  shadow: 0.6
tone: |
  How this entity should feel in the narrative.
```

## Naming convention

- Use lowercase with underscores for filenames: `meridian_city.yaml`, `iron_lotus.yaml`
- Organize by category in subdirectories:
  - `essences/characters/` -- NPCs and named characters
  - `essences/world/` -- cities, general world details
  - `essences/world/districts/` -- specific locations and neighborhoods
  - `essences/world/factions/` -- organizations, corporations, gangs
- The filename does not matter to the system; only the `name` field inside the YAML is used for lookups

## How Essences connect

Essences form an implicit network through shared references:

- **Relationships**: A character's `relationships` list references other entities by name
- **Location**: Multiple characters sharing the same `location` value are implicitly connected
- **Faction membership**: Entities in the same `faction` or `factions` are connected
- **The `connections()` method** on EssenceNetwork traces all these links automatically

## How Essences are used in story generation

When a writing session starts, the system loads all Essences into an `EssenceNetwork`. During beat generation:

1. The session's **location** Essence provides atmosphere (sights, sounds, smells) and tone
2. **NPC** Essences in the scene provide descriptions and speech patterns
3. **Faction** Essences referenced by characters in the scene add political and social dynamics
4. All of this context is assembled and injected into the LLM prompt alongside the protagonist's psychological facets

This means adding a new YAML file to the `essences/` directory immediately enriches any scene that references that entity -- no code changes required.
