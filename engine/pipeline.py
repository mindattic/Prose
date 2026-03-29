"""
Pipeline — The full story generation flow.

Orchestrates: Plan → Retrieve → Generate → Validate → Queue

This is the main entry point for generating canon-grounded stories.
It ties together the existing src/ beat generation system with the
new engine/ RAG and validation layers.

Usage:
    from engine.pipeline import generate_scene
    result = generate_scene(
        scene_goal="Recover a stolen augment core from a free clinic",
        location="Gary-Hammond ungoverned zone",
        entities=["Kael"],
        themes=["jurisdictional conflict"],
    )

CLI:
    python -m engine.pipeline build          # Build canon index + graph
    python -m engine.pipeline generate ...   # Generate a scene
    python -m engine.pipeline validate ...   # Validate existing text
    python -m engine.pipeline queue          # Review canon queue
"""

import sys
from datetime import datetime
from pathlib import Path

import yaml

from .config import ROOT, STORIES_DIR
from .embedder import build_canon_index
from .graph import build_graph
from .retriever import retrieve_context, format_context_for_prompt
from .validator import validate_scene, format_validation_report
from .canon_queue import submit_to_queue, list_pending


def build(verbose: bool = True) -> None:
    """Build both the vector index and knowledge graph from canon."""
    print("=" * 60)
    print("BUILDING CANON ENGINE")
    print("=" * 60)
    print()

    # Step 1: Embed worldbuilding into vector store
    chunk_count = build_canon_index(verbose=verbose)
    print()

    # Step 2: Build knowledge graph
    G = build_graph(verbose=verbose)
    print()

    print("=" * 60)
    print(f"Canon engine ready.")
    print(f"  Vector store: {chunk_count} chunks indexed")
    print(f"  Knowledge graph: {G.number_of_nodes()} nodes, {G.number_of_edges()} edges")
    print("=" * 60)


def generate_scene(
    scene_goal: str,
    location: str | None = None,
    entities: list[str] | None = None,
    themes: list[str] | None = None,
    num_beats: int = 5,
    validate: bool = True,
    verbose: bool = True,
) -> dict:
    """
    Generate a full scene using the canon-grounded pipeline.

    Steps:
        1. Retrieve relevant canon context (RAG)
        2. Generate beats using the existing src/ system with injected context
        3. Validate the output against canon
        4. Queue any new entities for review
        5. Save to stories/

    Args:
        scene_goal: What should happen in this scene
        location: Where the scene takes place (essence name or description)
        entities: Named entities involved
        themes: Thematic keywords for context retrieval
        num_beats: Number of narrative beats to generate
        validate: Whether to run the validator
        verbose: Print progress

    Returns:
        dict with scene text, validation report, and file paths
    """
    from src.session import create_session
    entities = entities or []
    themes = themes or []

    # Step 1: Retrieve canon context
    if verbose:
        print("[1/5] Retrieving canon context...")

    context = retrieve_context(
        entities=entities,
        location=location,
        themes=themes,
        query_text=scene_goal,
        max_chunks=20,
    )
    canon_prompt = format_context_for_prompt(context)

    if verbose:
        print(f"  Retrieved {len(context['sources'])} sources")
        for s in context["sources"]:
            print(f"    - {s}")

    # Step 2: Create session with canon context injected
    if verbose:
        print("[2/5] Creating writing session...")

    session = create_session(
        scene_goal=f"{scene_goal}\n\n{canon_prompt}",
        location=location,
        npcs=entities,
    )

    if verbose:
        print(f"  Session: {session.session_dir}")

    # Step 3: Generate beats
    if verbose:
        print(f"[3/5] Generating {num_beats} beats...")

    beat_goals = _plan_beats(scene_goal, num_beats)
    for i, beat_goal in enumerate(beat_goals):
        if verbose:
            print(f"  Beat {i + 1}/{num_beats}: {beat_goal[:60]}...")
        session.add_beat(beat_goal)

    scene_text = session.get_scene_text()

    if verbose:
        print(f"  Generated {len(scene_text)} characters")

    # Step 4: Validate
    validation_report = None
    if validate:
        if verbose:
            print("[4/5] Validating against canon...")

        validation_report = validate_scene(
            scene_text,
            scene_entities=entities,
            scene_location=location,
        )

        if verbose:
            print(format_validation_report(validation_report))

    # Step 5: Save and queue
    if verbose:
        print("[5/5] Saving to story archive...")

    story_path = _save_story(
        scene_text=scene_text,
        scene_goal=scene_goal,
        location=location,
        entities=entities,
        validation_report=validation_report,
        session_dir=session.session_dir,
    )

    # Queue new entities if any
    queued = []
    if validation_report:
        queued = submit_to_queue(validation_report, str(story_path))

    if verbose:
        print(f"  Story saved: {story_path}")
        if queued:
            print(f"  {len(queued)} new entities queued for review")
        print("\nDone.")

    return {
        "scene_text": scene_text,
        "validation_report": validation_report,
        "story_path": str(story_path),
        "session_dir": str(session.session_dir),
        "queued_entities": [str(p) for p in queued],
        "sources": context["sources"],
    }


def _plan_beats(scene_goal: str, num_beats: int) -> list[str]:
    """
    Break a scene goal into individual beat goals.

    For now, this is a simple decomposition. The conductor in src/
    handles the actual facet selection and generation.
    """
    from src.llm import generate

    if num_beats <= 1:
        return [scene_goal]

    response = generate(
        system="""You are a scene planner for literary cyberpunk fiction.
Break the given scene goal into individual narrative beats.
Each beat should be a single dramatic moment or action.
Return ONLY a numbered list, one beat per line. No other text.""",
        user=f"Break this scene into {num_beats} beats:\n\n{scene_goal}",
        model="claude-sonnet-4-6",
        temperature=0.3,
        max_tokens=1024,
    )

    # Parse the numbered list
    beats = []
    for line in response.strip().split("\n"):
        line = line.strip()
        if line and line[0].isdigit():
            # Strip the number prefix
            beat = line.lstrip("0123456789.):- ").strip()
            if beat:
                beats.append(beat)

    # Ensure we have the right count
    if len(beats) < num_beats:
        beats.extend([scene_goal] * (num_beats - len(beats)))
    return beats[:num_beats]


def _save_story(
    scene_text: str,
    scene_goal: str,
    location: str | None,
    entities: list[str],
    validation_report: dict | None,
    session_dir: Path,
) -> Path:
    """Save the generated scene to the story archive with metadata."""
    STORIES_DIR.mkdir(parents=True, exist_ok=True)

    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    story_dir = STORIES_DIR / f"scene_{timestamp}"
    story_dir.mkdir(parents=True, exist_ok=True)

    # Save the scene text
    scene_path = story_dir / "scene.md"
    scene_path.write_text(scene_text, encoding="utf-8")

    # Save metadata
    status = "draft"
    if validation_report:
        status = validation_report.get("status", "draft")

    meta = {
        "canon_status": status,
        "scene_goal": scene_goal,
        "location": location,
        "entities": entities,
        "generated": datetime.now().isoformat(),
        "session_dir": str(session_dir),
        "validator_status": status,
        "contradictions": len(validation_report.get("contradictions", [])) if validation_report else 0,
        "new_entities": [
            e.get("name", "?") for e in validation_report.get("new_entities", [])
        ] if validation_report else [],
    }

    meta_path = story_dir / "meta.yaml"
    meta_path.write_text(
        yaml.dump(meta, default_flow_style=False, allow_unicode=True),
        encoding="utf-8",
    )

    # Save validation report if present
    if validation_report:
        report_path = story_dir / "validation.yaml"
        report_path.write_text(
            yaml.dump(validation_report, default_flow_style=False, allow_unicode=True),
            encoding="utf-8",
        )

    return scene_path


# --- CLI ---

def _cli_build(args):
    build()


def _cli_generate(args):
    result = generate_scene(
        scene_goal=args.goal,
        location=args.location,
        entities=args.entities or [],
        themes=args.themes or [],
        num_beats=args.beats,
        validate=not args.no_validate,
    )

    print("\n" + "=" * 60)
    print("GENERATED SCENE")
    print("=" * 60)
    print(result["scene_text"])


def _cli_queue(args):
    pending = list_pending()
    if not pending:
        print("Canon queue is empty. No pending entries.")
        return

    print(f"Pending canon entries ({len(pending)}):\n")
    for entry in pending:
        print(f"  [{entry.get('type', '?')}] {entry.get('name', '?')}")
        print(f"    Context: {entry.get('context', '?')}")
        print(f"    Source: {entry.get('source_scene', '?')}")
        print(f"    Submitted: {entry.get('submitted', '?')}")
        print(f"    File: {entry.get('_path', '?')}")
        print()


def _cli_validate(args):
    text = Path(args.file).read_text(encoding="utf-8")
    report = validate_scene(
        text,
        scene_entities=args.entities or [],
        scene_location=args.location,
    )
    print(format_validation_report(report))


def main():
    import argparse

    parser = argparse.ArgumentParser(
        description="Street Samurai — Canon-Grounded Story Engine"
    )
    subs = parser.add_subparsers(dest="command")

    # build
    p_build = subs.add_parser("build", help="Build canon index and knowledge graph")
    p_build.set_defaults(func=_cli_build)

    # generate
    p_gen = subs.add_parser("generate", help="Generate a canon-grounded scene")
    p_gen.add_argument("--goal", required=True, help="Scene goal")
    p_gen.add_argument("--location", default=None, help="Scene location")
    p_gen.add_argument("--entities", nargs="*", help="Named entities in the scene")
    p_gen.add_argument("--themes", nargs="*", help="Thematic keywords")
    p_gen.add_argument("--beats", type=int, default=5, help="Number of beats (default 5)")
    p_gen.add_argument("--no-validate", action="store_true", help="Skip validation")
    p_gen.set_defaults(func=_cli_generate)

    # queue
    p_queue = subs.add_parser("queue", help="Review pending canon queue entries")
    p_queue.set_defaults(func=_cli_queue)

    # validate
    p_val = subs.add_parser("validate", help="Validate an existing text file against canon")
    p_val.add_argument("file", help="Path to the text file to validate")
    p_val.add_argument("--entities", nargs="*", help="Expected entities")
    p_val.add_argument("--location", default=None, help="Scene location")
    p_val.set_defaults(func=_cli_validate)

    args = parser.parse_args()
    if not args.command:
        parser.print_help()
        sys.exit(1)

    args.func(args)


if __name__ == "__main__":
    main()
