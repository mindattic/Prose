from pathlib import Path
from dataclasses import dataclass

import yaml

from .facet import Facet, load_all_facets


@dataclass
class Character:
    name: str
    title: str
    core_contradiction: str
    era: str
    genre: str
    arc: str
    facets: dict[str, Facet]

    def get_facet(self, name: str) -> Facet:
        return self.facets[name]

    def list_facets(self) -> list[str]:
        return list(self.facets.keys())

    def summary(self) -> str:
        lines = [
            f"Character: {self.name} — {self.title}",
            f"Era: {self.era}",
            f"Contradiction: {self.core_contradiction.strip()}",
            f"Arc: {self.arc.strip()}",
            f"Facets: {', '.join(self.list_facets())}",
        ]
        return "\n".join(lines)


def load_character(character_dir: Path) -> Character:
    profile_path = character_dir / "profile.yaml"
    data = yaml.safe_load(profile_path.read_text(encoding="utf-8"))
    facets = load_all_facets(character_dir / "facets")
    return Character(
        name=data["name"],
        title=data["title"],
        core_contradiction=data.get("core_contradiction", ""),
        era=data.get("era", ""),
        genre=data.get("genre", ""),
        arc=data.get("arc", ""),
        facets=facets,
    )
