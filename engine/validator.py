"""
Canon Validator — Checks generated text against established canon.

Uses the knowledge graph and LLM analysis to detect:
  - Contradictions with established facts
  - New entities not in the canon (potential hallucinations or valid additions)
  - Tone/style violations

Usage:
    from engine.validator import validate_scene
    report = validate_scene(generated_text, scene_entities=["Kael", "Tessera"])
"""

import json

from .graph import load_graph, query_entity
from .retriever import retrieve_context
from .config import VALIDATOR_MODEL, VALIDATOR_TEMPERATURE, VALIDATOR_MAX_TOKENS


VALIDATOR_SYSTEM = """You are a canon validator for a literary fiction project called Street Samurai.

You will receive:
1. GENERATED TEXT — a scene or narrative beat that was just produced
2. CANON CONTEXT — excerpts from the authoritative worldbuilding documents

Your job is to compare the generated text against the canon and identify:

A) CONTRADICTIONS — facts in the generated text that directly conflict with canon.
   Example: Generated says "Kael's blade is titanium." Canon says "ACNT composite with piezoelectric layer."

B) NEW ENTITIES — names, places, organizations, or technologies mentioned in the
   generated text that do NOT appear in the canon context. These are not necessarily
   wrong — they may be valid new additions. Flag them for human review.

C) TONE VIOLATIONS — passages that violate the established literary rules:
   - Sentences longer than 25 words
   - Paragraphs without action, sensory detail, or a lie
   - Generic noir narration, trailer lines, or slogans
   - Samurai clichés or anime dialogue

Return a JSON object with this exact structure:
{
    "status": "green" | "yellow" | "red",
    "contradictions": [
        {"claim": "what the text says", "canon": "what canon says", "source": "file.md", "severity": "high|medium|low"}
    ],
    "new_entities": [
        {"name": "entity name", "type": "character|location|technology|organization|other", "context": "how it appears"}
    ],
    "tone_violations": [
        {"issue": "description", "excerpt": "the offending text"}
    ],
    "summary": "one-line assessment"
}

Status levels:
- "green": No contradictions, no new entities. Safe to publish.
- "yellow": No contradictions, but new entities need human review. Safe to publish with review.
- "red": Contains contradictions with established canon. Must be revised.

Return ONLY valid JSON."""


def validate_scene(
    generated_text: str,
    scene_entities: list[str] | None = None,
    scene_location: str | None = None,
) -> dict:
    """
    Validate generated text against canon.

    Args:
        generated_text: The text to validate
        scene_entities: Named entities expected in the scene
        scene_location: Location of the scene

    Returns:
        Validation report dict with status, contradictions, new entities, etc.
    """
    from src.llm import generate

    scene_entities = scene_entities or []

    # Retrieve relevant canon for comparison
    context = retrieve_context(
        entities=scene_entities,
        location=scene_location,
        max_chunks=15,
    )

    # Also check the knowledge graph for entity verification
    graph_info = ""
    try:
        G = load_graph()
        known_entities = set(G.nodes)
        graph_info = f"\nKnown entities in canon graph ({len(known_entities)} total). "
        graph_info += "Any entity name in the generated text that is NOT in this list should be flagged as a new entity."
        # Include a relevant subset
        if scene_entities:
            relevant = set()
            for name in scene_entities:
                info = query_entity(G, name)
                if info:
                    for rel in info.get("outgoing", []):
                        relevant.add(rel["target"])
                    for rel in info.get("incoming", []):
                        relevant.add(rel["source"])
            graph_info += f"\nEntities related to this scene: {', '.join(sorted(relevant)[:50])}"
    except FileNotFoundError:
        pass

    # Build the validation prompt
    user_prompt = f"""GENERATED TEXT:
---
{generated_text}
---

CANON CONTEXT:
---
{context['canon_text']}
{graph_info}
---

Validate the generated text against the canon context. Return JSON."""

    # Call the LLM for validation
    response = generate(
        system=VALIDATOR_SYSTEM,
        user=user_prompt,
        model=VALIDATOR_MODEL,
        temperature=VALIDATOR_TEMPERATURE,
        max_tokens=VALIDATOR_MAX_TOKENS,
    )

    # Parse the JSON response
    try:
        # Strip markdown code fences if present
        clean = response.strip()
        if clean.startswith("```"):
            clean = clean.split("\n", 1)[1]
            if clean.endswith("```"):
                clean = clean.rsplit("```", 1)[0]
        report = json.loads(clean)
    except json.JSONDecodeError:
        report = {
            "status": "yellow",
            "contradictions": [],
            "new_entities": [],
            "tone_violations": [],
            "summary": f"Validator returned non-JSON response: {response[:200]}",
        }

    return report


def format_validation_report(report: dict) -> str:
    """Format a validation report for human reading."""
    lines = []

    status = report.get("status", "unknown")
    status_icon = {"green": "✓", "yellow": "⚠", "red": "✗"}.get(status, "?")
    lines.append(f"Validation: {status_icon} {status.upper()}")
    lines.append(report.get("summary", ""))
    lines.append("")

    contradictions = report.get("contradictions", [])
    if contradictions:
        lines.append(f"CONTRADICTIONS ({len(contradictions)}):")
        for c in contradictions:
            lines.append(f"  [{c.get('severity', '?')}] Claim: {c.get('claim', '?')}")
            lines.append(f"         Canon: {c.get('canon', '?')}")
            lines.append(f"         Source: {c.get('source', '?')}")
        lines.append("")

    new_entities = report.get("new_entities", [])
    if new_entities:
        lines.append(f"NEW ENTITIES ({len(new_entities)}) — needs human review:")
        for e in new_entities:
            lines.append(f"  [{e.get('type', '?')}] {e.get('name', '?')}: {e.get('context', '?')}")
        lines.append("")

    tone_violations = report.get("tone_violations", [])
    if tone_violations:
        lines.append(f"TONE VIOLATIONS ({len(tone_violations)}):")
        for t in tone_violations:
            lines.append(f"  {t.get('issue', '?')}")
            excerpt = t.get("excerpt", "")
            if excerpt:
                lines.append(f"    \"{excerpt[:100]}...\"" if len(excerpt) > 100 else f"    \"{excerpt}\"")
        lines.append("")

    return "\n".join(lines)
