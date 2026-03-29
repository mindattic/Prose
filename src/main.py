#!/usr/bin/env python3
"""
Street Samurai — Multi-Facet Psychological Character System

A cyberpunk literary fiction engine where each psychological facet of the
protagonist is powered by a separate LLM voice. The conductor orchestrates
which facets lead based on narrative context.

Usage:
    python -m src.main new-session --scene-goal "Scene goal description" [--location "Meridian"] [--npcs "Vex" "Kira"]
    python -m src.main continue-session --session sessions/session_YYYYMMDD_HHMMSS
    python -m src.main beat --session <path> --goal "Beat goal" [--lead wound] [--npcs "Vex"]
    python -m src.main export --session <path> --scene-number 2
    python -m src.main list-facets
    python -m src.main show-character
    python -m src.main list-essences
    python -m src.main show-essence <name>
"""

import argparse
import sys
from pathlib import Path

PROJECT_ROOT = Path(__file__).resolve().parent.parent


def cmd_new_session(args):
    from .session import create_session

    session = create_session(
        args.scene_goal,
        location=args.location,
        npcs=args.npcs,
    )
    print(f"Session created: {session.session_dir}")
    print(f"Scene goal: {args.scene_goal}")
    if session.location:
        print(f"Location: {session.location}")
    if session.npcs:
        print(f"NPCs in scene: {', '.join(session.npcs)}")
    print(f"Character: {session.character.name} — {session.character.title}")
    print(f"Active facets: {', '.join(session.character.list_facets())}")
    essences_count = len(session.essence_network.all_essences) if session.essence_network else 0
    print(f"Essences loaded: {essences_count}")
    print(f"\nUse 'beat' command to generate narrative beats.")
    print(f"  python -m src.main beat --session {session.session_dir} --goal \"description\"")


def cmd_beat(args):
    from .session import load_session

    session_dir = Path(args.session)
    session = load_session(session_dir)

    print(f"Generating beat: {args.goal}")
    if args.lead:
        print(f"Forced lead facet: {args.lead}")

    beat_npcs = args.npcs if args.npcs else None
    beat = session.add_beat(args.goal, force_lead=args.lead, npcs=beat_npcs)

    print(f"\n--- Beat {beat['index']} ---")
    print(f"Lead: {beat['lead_facet']}")
    print(f"Supporting: {', '.join(beat['supporting_facets'])}")
    if beat.get("npcs"):
        print(f"NPCs: {', '.join(beat['npcs'])}")
    tags = beat.get("analysis", {}).get("tags", [])
    if tags:
        print(f"Context tags: {', '.join(tags)}")
    print(f"\n{beat['text']}")
    print(f"\nScene draft: {session.scene_draft_path}")


def cmd_export(args):
    from .session import load_session
    from .export import export_session

    session = load_session(Path(args.session))
    canon_dir = PROJECT_ROOT / "canon"
    paths = export_session(session, canon_dir, args.scene_number)

    print("Exported:")
    for label, path in paths.items():
        print(f"  {label}: {path}")


def cmd_list_facets(args):
    from .character import load_character

    character = load_character(PROJECT_ROOT / "character")
    print(f"Character: {character.name} — {character.title}\n")
    for name, facet in character.facets.items():
        print(f"  {facet.label} {name}")
        print(f"    Domain: {facet.domain}")
        print(f"    Triggers: {', '.join(facet.triggers)}")
        print(f"    Tone: {facet.voice_tone}")
        print(f"    Memories: {len(facet.core_memories)}")
        print()


def cmd_show_character(args):
    from .character import load_character

    character = load_character(PROJECT_ROOT / "character")
    print(character.summary())


def cmd_list_essences(args):
    from .essence import load_essence_network

    network = load_essence_network(PROJECT_ROOT / "essences")
    essences = network.all_essences
    if not essences:
        print("No essences found in essences/ directory.")
        return

    # Group by type
    by_type: dict[str, list] = {}
    for e in essences:
        by_type.setdefault(e.type, []).append(e)

    for etype in sorted(by_type.keys()):
        print(f"\n=== {etype.upper()} ({len(by_type[etype])}) ===")
        for e in sorted(by_type[etype], key=lambda x: x.name):
            aliases_str = f" (aka {', '.join(e.aliases)})" if e.aliases else ""
            print(f"  {e.name}{aliases_str}")
            if e.source_path:
                print(f"    File: {e.source_path.relative_to(PROJECT_ROOT)}")

    print(f"\nTotal: {len(essences)} essences")


def cmd_show_essence(args):
    from .essence import load_essence_network

    network = load_essence_network(PROJECT_ROOT / "essences")
    essence = network.get(args.name)
    if not essence:
        print(f"Essence not found: {args.name}")
        print("Use 'list-essences' to see all available essences.")
        return

    print(essence.summary())
    print()

    # Show connections
    connections = network.connections(args.name)
    if connections:
        print("Connections:")
        for conn in connections:
            print(f"  -> [{conn.type}] {conn.name}")
    print()

    # Show full data keys for exploration
    print(f"Data keys: {', '.join(essence.data.keys())}")
    if essence.source_path:
        print(f"Source: {essence.source_path}")


def main():
    parser = argparse.ArgumentParser(
        description="Street Samurai — Multi-Facet Character System"
    )
    subparsers = parser.add_subparsers(dest="command", help="Available commands")

    # new-session
    p_new = subparsers.add_parser("new-session", help="Start a new writing session")
    p_new.add_argument("--scene-goal", required=True, help="Goal for this scene")
    p_new.add_argument("--location", default=None, help="Location essence name for the scene")
    p_new.add_argument("--npcs", nargs="*", default=None, help="NPC essence names present in scene")
    p_new.set_defaults(func=cmd_new_session)

    # beat
    p_beat = subparsers.add_parser("beat", help="Generate the next narrative beat")
    p_beat.add_argument("--session", required=True, help="Path to session directory")
    p_beat.add_argument("--goal", required=True, help="Goal for this beat")
    p_beat.add_argument("--lead", default=None, help="Force a specific facet to lead (e.g., wound, shadow)")
    p_beat.add_argument("--npcs", nargs="*", default=None, help="Additional NPC essence names for this beat")
    p_beat.set_defaults(func=cmd_beat)

    # export
    p_export = subparsers.add_parser("export", help="Export session to canon")
    p_export.add_argument("--session", required=True, help="Path to session directory")
    p_export.add_argument("--scene-number", type=int, required=True, help="Scene number for canon")
    p_export.set_defaults(func=cmd_export)

    # list-facets
    p_list = subparsers.add_parser("list-facets", help="List all character facets")
    p_list.set_defaults(func=cmd_list_facets)

    # show-character
    p_show = subparsers.add_parser("show-character", help="Show character summary")
    p_show.set_defaults(func=cmd_show_character)

    # list-essences
    p_essences = subparsers.add_parser("list-essences", help="List all loaded essences by type")
    p_essences.set_defaults(func=cmd_list_essences)

    # show-essence
    p_show_ess = subparsers.add_parser("show-essence", help="Display a specific essence")
    p_show_ess.add_argument("name", help="Name or alias of the essence to show")
    p_show_ess.set_defaults(func=cmd_show_essence)

    args = parser.parse_args()
    if not args.command:
        parser.print_help()
        sys.exit(1)

    args.func(args)


if __name__ == "__main__":
    main()
