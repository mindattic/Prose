from pathlib import Path
from dataclasses import dataclass, field

import yaml


@dataclass
class Facet:
    name: str
    label: str
    domain: str
    triggers: list[str]
    voice_tone: str
    voice_style: str
    voice_prohibitions: list[str]
    core_memories: list[str]
    model: str
    temperature: float
    system_prompt: str

    def matches_context(self, context_tags: list[str]) -> float:
        if not context_tags:
            return 0.0
        matches = sum(1 for tag in context_tags if tag in self.triggers)
        return matches / len(self.triggers)


def load_facet(path: Path) -> Facet:
    data = yaml.safe_load(path.read_text(encoding="utf-8"))
    voice = data.get("voice", {})
    return Facet(
        name=data["name"],
        label=data["label"],
        domain=data["domain"],
        triggers=data.get("triggers", []),
        voice_tone=voice.get("tone", ""),
        voice_style=voice.get("style", ""),
        voice_prohibitions=voice.get("prohibitions", []),
        core_memories=data.get("core_memories", []),
        model=data.get("model", "claude-sonnet-4-6"),
        temperature=data.get("temperature", 0.8),
        system_prompt=data.get("system_prompt", ""),
    )


def load_all_facets(facets_dir: Path) -> dict[str, Facet]:
    facets = {}
    for path in sorted(facets_dir.glob("*.yaml")):
        facet = load_facet(path)
        facets[facet.name] = facet
    return facets
