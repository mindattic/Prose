"""
Essence System — The interconnected YAML network that defines every entity
in the Street Samurai world. Characters, places, factions, technology —
everything has an Essence that defines its properties, motivations, and
connections to other Essences.
"""

from dataclasses import dataclass, field
from pathlib import Path

import yaml


@dataclass
class Essence:
    type: str          # "character", "place", "faction", "technology"
    name: str
    aliases: list[str]
    data: dict         # Full YAML data for flexible access
    source_path: Path | None = None

    @property
    def description(self) -> str:
        """Return the description field, or empty string."""
        desc = self.data.get("description", "")
        if isinstance(desc, str):
            return desc.strip()
        return str(desc).strip()

    @property
    def facet_weights(self) -> dict[str, float] | None:
        """Return facet weights if this essence defines them (e.g. NPCs)."""
        weights = self.data.get("facet_weights")
        if isinstance(weights, dict):
            return {str(k): float(v) for k, v in weights.items()}
        return None

    def get(self, key, default=None):
        """Flexible accessor into the underlying data dict."""
        return self.data.get(key, default)

    @property
    def relationships(self) -> list[dict]:
        """Return relationships list if present."""
        return self.data.get("relationships", [])

    @property
    def location(self) -> str | None:
        """Return the location field if present."""
        return self.data.get("location")

    @property
    def faction(self) -> str | None:
        """Return the faction field if present."""
        return self.data.get("faction")

    @property
    def factions(self) -> list[str]:
        """Return factions list (normalizes single faction to list)."""
        f = self.data.get("factions", [])
        if isinstance(f, str):
            return [f]
        single = self.data.get("faction")
        if not f and single:
            return [single]
        return f

    @property
    def speech_patterns(self) -> str | None:
        """Return speech patterns for characters/NPCs."""
        return self.data.get("speech_patterns") or self.data.get("speech")

    @property
    def atmosphere(self) -> dict | None:
        """Return atmosphere dict (sights, sounds, smells) for places."""
        return self.data.get("atmosphere")

    @property
    def tone(self) -> str | None:
        """Return the tone field if present."""
        val = self.data.get("tone")
        if isinstance(val, str):
            return val.strip()
        return None

    def summary(self) -> str:
        """Return a human-readable summary of this essence."""
        lines = [f"[{self.type.upper()}] {self.name}"]
        if self.aliases:
            lines.append(f"  Aliases: {', '.join(self.aliases)}")
        desc = self.description
        if desc:
            # Truncate long descriptions for summary view
            if len(desc) > 200:
                desc = desc[:200] + "..."
            lines.append(f"  {desc}")
        if self.location:
            lines.append(f"  Location: {self.location}")
        if self.factions:
            lines.append(f"  Factions: {', '.join(self.factions)}")
        return "\n".join(lines)


def _normalize_type(raw_type: str) -> str:
    """Normalize type strings to standard categories."""
    mapping = {
        "setting": "place",
        "district": "place",
        "location": "place",
        "place": "place",
        "character": "character",
        "npc": "character",
        "protagonist": "character",
        "faction": "faction",
        "organization": "faction",
        "corp": "faction",
        "technology": "technology",
        "tech": "technology",
        "weapon": "technology",
        "augmentation": "technology",
    }
    return mapping.get(raw_type.lower(), raw_type.lower())


def load_essence(path: Path) -> Essence:
    """Load a single YAML file into an Essence."""
    data = yaml.safe_load(path.read_text(encoding="utf-8"))
    if not isinstance(data, dict):
        raise ValueError(f"Essence file {path} does not contain a YAML mapping")

    raw_type = data.get("type", "unknown")
    etype = _normalize_type(raw_type)
    name = data.get("name", path.stem)

    # Gather aliases from various possible fields
    aliases = []
    if "aliases" in data:
        a = data["aliases"]
        aliases = a if isinstance(a, list) else [a]
    if "official_name" in data and data["official_name"] != name:
        aliases.append(data["official_name"])
    if "tagline" in data:
        aliases.append(data["tagline"])

    return Essence(
        type=etype,
        name=name,
        aliases=[str(a) for a in aliases],
        data=data,
        source_path=path,
    )


class EssenceNetwork:
    """Holds all loaded essences and provides query methods."""

    def __init__(self):
        self._essences: dict[str, Essence] = {}  # keyed by lowercase name
        self._alias_index: dict[str, str] = {}    # alias -> canonical name key

    @property
    def all_essences(self) -> list[Essence]:
        return list(self._essences.values())

    def add(self, essence: Essence) -> None:
        """Add an essence to the network."""
        key = essence.name.lower()
        self._essences[key] = essence
        for alias in essence.aliases:
            self._alias_index[alias.lower()] = key

    def get(self, name: str) -> Essence | None:
        """Find an essence by name or alias."""
        key = name.lower()
        if key in self._essences:
            return self._essences[key]
        if key in self._alias_index:
            return self._essences.get(self._alias_index[key])
        # Fuzzy: check if name is a substring of any essence name
        for ename, essence in self._essences.items():
            if key in ename:
                return essence
        return None

    def find_by_type(self, type_name: str) -> list[Essence]:
        """Get all essences of a given type."""
        normalized = _normalize_type(type_name)
        return [e for e in self._essences.values() if e.type == normalized]

    def connections(self, name: str) -> list[Essence]:
        """Find all essences connected to a given one via relationships,
        location, or faction membership."""
        target = self.get(name)
        if not target:
            return []

        connected = set()
        target_key = target.name.lower()

        # Direct relationships from the target
        for rel in target.relationships:
            rel_name = rel.get("name") or rel.get("target") or rel.get("entity", "")
            found = self.get(rel_name)
            if found:
                connected.add(found.name.lower())

        # Same location
        if target.location:
            loc_key = target.location.lower()
            for e in self._essences.values():
                if e.name.lower() == target_key:
                    continue
                if e.location and e.location.lower() == loc_key:
                    connected.add(e.name.lower())
                if e.name.lower() == loc_key:
                    connected.add(e.name.lower())

        # Same faction
        for faction_name in target.factions:
            fkey = faction_name.lower()
            for e in self._essences.values():
                if e.name.lower() == target_key:
                    continue
                if fkey in [f.lower() for f in e.factions]:
                    connected.add(e.name.lower())
                if e.name.lower() == fkey:
                    connected.add(e.name.lower())

        # Check if this essence is referenced in other essences' relationships
        for e in self._essences.values():
            if e.name.lower() == target_key:
                continue
            for rel in e.relationships:
                rel_name = rel.get("name") or rel.get("target") or rel.get("entity", "")
                if rel_name.lower() == target_key or rel_name.lower() in [a.lower() for a in target.aliases]:
                    connected.add(e.name.lower())

        return [self._essences[k] for k in connected if k in self._essences]

    def context_for_scene(self, location: str | None, characters: list[str] | None = None) -> str:
        """Build a rich context string for a scene by pulling in all relevant
        essences: the place, the characters present, their relationships,
        and relevant factions."""
        parts = []
        characters = characters or []
        mentioned_factions = set()

        # Location context
        if location:
            loc_essence = self.get(location)
            if loc_essence:
                parts.append(f"=== LOCATION: {loc_essence.name} ===")
                parts.append(loc_essence.description)
                atmo = loc_essence.atmosphere
                if atmo:
                    if "sights" in atmo:
                        parts.append(f"Sights: {atmo['sights']}")
                    if "sounds" in atmo:
                        parts.append(f"Sounds: {atmo['sounds']}")
                    if "smells" in atmo:
                        parts.append(f"Smells: {atmo['smells']}")
                if loc_essence.tone:
                    parts.append(f"Tone: {loc_essence.tone}")
                parts.append("")

        # Character/NPC context
        for char_name in characters:
            char_essence = self.get(char_name)
            if not char_essence:
                continue
            parts.append(f"=== NPC: {char_essence.name} ===")
            parts.append(char_essence.description)
            speech = char_essence.speech_patterns
            if speech:
                parts.append(f"Speech patterns: {speech}")
            # Note their faction membership
            for f in char_essence.factions:
                mentioned_factions.add(f)
            # Show relationships relevant to other characters in scene
            for rel in char_essence.relationships:
                rel_target = rel.get("name") or rel.get("target") or rel.get("entity", "")
                if rel_target.lower() in [c.lower() for c in characters]:
                    rel_type = rel.get("type", rel.get("relationship", "connected"))
                    parts.append(f"  -> Relationship with {rel_target}: {rel_type}")
            parts.append("")

        # Faction context
        for faction_name in mentioned_factions:
            faction_essence = self.get(faction_name)
            if faction_essence:
                parts.append(f"=== FACTION: {faction_essence.name} ===")
                parts.append(faction_essence.description)
                parts.append("")

        return "\n".join(parts)


def load_essence_network(essences_dir: Path) -> EssenceNetwork:
    """Recursively load ALL essences from the essences/ directory tree."""
    network = EssenceNetwork()
    if not essences_dir.exists():
        return network

    for yaml_path in sorted(essences_dir.rglob("*.yaml")):
        try:
            essence = load_essence(yaml_path)
            network.add(essence)
        except Exception as exc:
            # Log but don't crash on malformed files
            print(f"Warning: Could not load essence {yaml_path}: {exc}")

    return network
