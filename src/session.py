from datetime import datetime
from pathlib import Path

import yaml

from .character import Character, load_character
from .conductor import run_beat
from .essence import EssenceNetwork, load_essence_network
from .utils import write_text, read_text


PROJECT_ROOT = Path(__file__).resolve().parent.parent


def _load_story_context() -> str:
    bible_path = PROJECT_ROOT / "world" / "story_bible.yaml"
    rules_path = PROJECT_ROOT / "world" / "literary_rules.yaml"
    parts = []
    if bible_path.exists():
        data = yaml.safe_load(bible_path.read_text(encoding="utf-8"))
        parts.append(f"Title: {data.get('title', '')}")
        parts.append(f"Genre: {data.get('genre', '')}")
        parts.append(f"Tone: {data.get('tone', '')}")
        parts.append(f"Core Theme: {data.get('core_theme', '')}")
        proto = data.get("protagonist", {})
        parts.append(f"Protagonist Contradiction: {proto.get('contradiction', '')}")
        parts.append(f"Protagonist Arc: {proto.get('arc', '')}")
    if rules_path.exists():
        rules = yaml.safe_load(rules_path.read_text(encoding="utf-8"))
        prohibitions = rules.get("prohibitions", [])
        if prohibitions:
            parts.append("Prohibitions: " + "; ".join(prohibitions))
    return "\n".join(parts)


class Session:
    def __init__(
        self,
        session_dir: Path,
        character: Character,
        scene_goal: str,
        essence_network: EssenceNetwork | None = None,
        location: str | None = None,
        npcs: list[str] | None = None,
    ):
        self.session_dir = session_dir
        self.character = character
        self.scene_goal = scene_goal
        self.essence_network = essence_network
        self.location = location
        self.npcs = npcs or []
        self.beats: list[dict] = []
        self.scene_text = ""
        self.story_context = _load_story_context()

        session_dir.mkdir(parents=True, exist_ok=True)
        (session_dir / "facet_responses").mkdir(exist_ok=True)

    @property
    def scene_draft_path(self) -> Path:
        return self.session_dir / "scene_draft.md"

    @property
    def conductor_log_path(self) -> Path:
        return self.session_dir / "conductor_log.md"

    @property
    def session_yaml_path(self) -> Path:
        return self.session_dir / "session.yaml"

    def save_state(self) -> None:
        state = {
            "scene_goal": self.scene_goal,
            "beat_count": len(self.beats),
            "character_dir": str(PROJECT_ROOT / "character"),
            "location": self.location,
            "npcs": self.npcs,
            "beats": [
                {
                    "index": b["index"],
                    "lead_facet": b["lead_facet"],
                    "supporting_facets": b["supporting_facets"],
                    "beat_goal": b["beat_goal"],
                    "npcs": b.get("npcs", []),
                }
                for b in self.beats
            ],
        }
        write_text(self.session_yaml_path, yaml.dump(state, default_flow_style=False))

    def _update_scene_draft(self) -> None:
        lines = [f"# Scene Draft\n", f"**Goal:** {self.scene_goal}\n\n---\n"]
        for beat in self.beats:
            lines.append(f"\n### Beat {beat['index']} — Lead: {beat['lead_facet']}\n")
            lines.append(beat["text"])
            lines.append("\n")
        self.scene_text = "\n".join(lines)
        write_text(self.scene_draft_path, self.scene_text)

    def _update_conductor_log(self, beat_index: int, lead: str, supporting: list[str], analysis: dict) -> None:
        entry = f"""
## Beat {beat_index}

- **Lead facet:** {lead}
- **Supporting:** {', '.join(supporting)}
- **Context tags:** {', '.join(analysis.get('tags', []))}
- **Dominant emotion:** {analysis.get('dominant_emotion', 'unknown')}
- **Stakes:** {analysis.get('stakes', 'unknown')}

---
"""
        log_path = self.conductor_log_path
        existing = read_text(log_path) if log_path.exists() else "# Conductor Log\n"
        write_text(log_path, existing + entry)

    def add_beat(
        self,
        beat_goal: str,
        force_lead: str | None = None,
        npcs: list[str] | None = None,
    ) -> dict:
        scene_so_far = "\n\n".join(b["text"] for b in self.beats) if self.beats else ""

        # Combine session-level NPCs with beat-level NPCs
        beat_npcs = list(self.npcs)
        if npcs:
            for npc in npcs:
                if npc not in beat_npcs:
                    beat_npcs.append(npc)

        beat_text, lead, supporting, analysis = run_beat(
            character=self.character,
            story_context=self.story_context,
            scene_so_far=scene_so_far,
            beat_goal=beat_goal,
            force_lead=force_lead,
            essence_network=self.essence_network,
            location=self.location,
            npcs=beat_npcs if beat_npcs else None,
        )

        beat_index = len(self.beats) + 1
        beat_record = {
            "index": beat_index,
            "text": beat_text,
            "lead_facet": lead.name,
            "supporting_facets": [f.name for f in supporting],
            "beat_goal": beat_goal,
            "analysis": analysis,
            "npcs": beat_npcs,
        }
        self.beats.append(beat_record)

        # Save individual facet response
        write_text(
            self.session_dir / "facet_responses" / f"beat_{beat_index:02d}_{lead.name}.md",
            beat_text,
        )

        self._update_scene_draft()
        self._update_conductor_log(
            beat_index, lead.name, [f.name for f in supporting], analysis
        )
        self.save_state()

        return beat_record

    def get_scene_text(self) -> str:
        return "\n\n".join(b["text"] for b in self.beats)


def create_session(
    scene_goal: str,
    location: str | None = None,
    npcs: list[str] | None = None,
) -> Session:
    character = load_character(PROJECT_ROOT / "character")
    essence_network = load_essence_network(PROJECT_ROOT / "essences")
    now = datetime.now().strftime("%Y%m%d_%H%M%S")
    session_dir = PROJECT_ROOT / "sessions" / f"session_{now}"
    return Session(
        session_dir,
        character,
        scene_goal,
        essence_network=essence_network,
        location=location,
        npcs=npcs,
    )


def load_session(session_dir: Path) -> Session:
    state = yaml.safe_load(session_dir.joinpath("session.yaml").read_text(encoding="utf-8"))
    character = load_character(Path(state["character_dir"]))
    essence_network = load_essence_network(PROJECT_ROOT / "essences")
    session = Session(
        session_dir,
        character,
        state["scene_goal"],
        essence_network=essence_network,
        location=state.get("location"),
        npcs=state.get("npcs", []),
    )

    # Reload beats from saved facet responses
    draft_path = session_dir / "scene_draft.md"
    if draft_path.exists():
        for beat_info in state.get("beats", []):
            resp_path = session_dir / "facet_responses" / f"beat_{beat_info['index']:02d}_{beat_info['lead_facet']}.md"
            text = read_text(resp_path) if resp_path.exists() else ""
            session.beats.append({
                "index": beat_info["index"],
                "text": text,
                "lead_facet": beat_info["lead_facet"],
                "supporting_facets": beat_info["supporting_facets"],
                "beat_goal": beat_info["beat_goal"],
                "analysis": {},
                "npcs": beat_info.get("npcs", []),
            })

    return session
