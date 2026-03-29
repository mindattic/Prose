"""
Knowledge Graph — Entity relationships built from worldbuilding canon.

Uses NetworkX to maintain an in-memory graph of every named entity
(corponation, character, location, technology, drug, weapon) and their
relationships. The graph is used for:
  - Contradiction detection (validator checks claims against the graph)
  - Scene planning (find all entities related to a scene's participants)
  - Cross-referencing (follow relationship chains between entities)

Usage:
    from engine.graph import build_graph, load_graph, query_entity
    build_graph()                      # Build from canon files
    G = load_graph()                   # Load the saved graph
    info = query_entity(G, "Kael")     # Get entity info + connections
"""

import json
import re
from pathlib import Path

import networkx as nx
import yaml

from .config import (
    WORLDBUILDING_DIR,
    CHARACTERS_DIR,
    ESSENCES_DIR,
    GRAPH_PATH,
)


def _extract_entities_from_essences(essences_dir: Path, G: nx.DiGraph) -> None:
    """Load structured entities from the essences/ YAML files."""
    if not essences_dir.exists():
        return

    for yaml_path in sorted(essences_dir.rglob("*.yaml")):
        try:
            data = yaml.safe_load(yaml_path.read_text(encoding="utf-8"))
            if not isinstance(data, dict):
                continue

            name = data.get("name", yaml_path.stem)
            etype = data.get("type", "unknown")
            source = str(yaml_path.relative_to(yaml_path.parent.parent))

            G.add_node(name, type=etype, source=source, data=_safe_data(data))

            # Extract relationships
            for rel in data.get("relationships", []):
                target = rel.get("name") or rel.get("target") or rel.get("entity", "")
                rel_type = rel.get("type") or rel.get("relationship", "related_to")
                if target:
                    G.add_edge(name, target, relationship=rel_type)

            # Location relationship
            loc = data.get("location")
            if loc:
                G.add_edge(name, loc, relationship="located_in")

            # Faction/affiliation
            factions = data.get("factions", [])
            if isinstance(factions, str):
                factions = [factions]
            aff = data.get("faction") or data.get("affiliation")
            if aff and aff not in factions:
                factions.append(aff)
            for faction in factions:
                G.add_edge(name, faction, relationship="affiliated_with")

        except Exception as e:
            print(f"  Warning: Could not process {yaml_path}: {e}")


def _extract_entities_from_characters(chars_dir: Path, G: nx.DiGraph) -> None:
    """Load character entities from characters/ YAML files."""
    if not chars_dir.exists():
        return

    for yaml_path in sorted(chars_dir.rglob("*.yaml")):
        try:
            data = yaml.safe_load(yaml_path.read_text(encoding="utf-8"))
            if not isinstance(data, dict):
                continue

            name = data.get("name", yaml_path.stem)
            source = str(yaml_path.relative_to(yaml_path.parent.parent))

            G.add_node(name, type="character", source=source, data=_safe_data(data))

            # Aliases
            for alias in data.get("aliases", []):
                G.add_node(alias, type="alias", alias_of=name)
                G.add_edge(alias, name, relationship="alias_of")

            # Relationships
            for rel in data.get("relationships", []):
                target = rel.get("name", "")
                rel_type = rel.get("facet_connection", rel.get("status", "related_to"))
                if target:
                    G.add_edge(name, target, relationship=rel_type)

            # Affiliation
            aff = data.get("affiliation", "")
            if aff:
                G.add_edge(name, aff, relationship="affiliated_with")

            # Augmentation references
            aug = data.get("augmentation", "")
            if "NeoCortex" in str(aug):
                G.add_edge(name, "NeoCortex Industries", relationship="tested_by")
            if "Tessera" in str(aug):
                G.add_edge(name, "Tessera", relationship="hardware_from")

        except Exception as e:
            print(f"  Warning: Could not process {yaml_path}: {e}")


def _extract_corponations_from_worldbuilding(wb_dir: Path, G: nx.DiGraph) -> None:
    """Extract corponation entities from worldbuilding markdown files."""
    if not wb_dir.exists():
        return

    corp_files = sorted(wb_dir.glob("corponations_*.md"))
    for filepath in corp_files:
        text = filepath.read_text(encoding="utf-8")
        source = str(filepath.relative_to(filepath.parent.parent))

        # Extract corp names from ### headers in the format "### #. Name"
        # or "**#. Name**" patterns
        corp_matches = re.findall(
            r"(?:^###?\s*(?:\d+\.?\s*)?(.+?)$)|(?:\*\*\d+\.\s*(.+?)\*\*)",
            text,
            re.MULTILINE,
        )

        for match in corp_matches:
            name = (match[0] or match[1]).strip()
            if not name or len(name) > 80 or name.startswith("[") or name.startswith("("):
                continue
            # Skip section headers that aren't corp names
            if any(kw in name.lower() for kw in [
                "overview", "appendix", "summary", "table", "sector",
                "relationship", "filing", "note", "source",
            ]):
                continue

            if not G.has_node(name):
                G.add_node(name, type="corponation", source=source)


def _safe_data(data: dict) -> dict:
    """Create a JSON-serializable subset of entity data for the graph."""
    safe = {}
    for key in ["name", "type", "tier", "status", "occupation", "affiliation",
                 "sector", "origin", "valuation"]:
        if key in data:
            val = data[key]
            if isinstance(val, (str, int, float, bool)):
                safe[key] = val
    # Facet weights
    facets = data.get("facets") or data.get("facet_weights")
    if isinstance(facets, dict):
        safe["facets"] = {str(k): float(v) for k, v in facets.items()}
    return safe


def build_graph(verbose: bool = True) -> nx.DiGraph:
    """
    Build the knowledge graph from all canon sources.

    Scans essences/, characters/, and worldbuilding/ to extract entities
    and relationships. Saves the graph to JSON for fast reload.

    Returns the built graph.
    """
    G = nx.DiGraph()

    if verbose:
        print("Building knowledge graph...")

    # Structured sources first (most reliable)
    _extract_entities_from_essences(ESSENCES_DIR, G)
    if verbose:
        print(f"  After essences: {G.number_of_nodes()} nodes, {G.number_of_edges()} edges")

    _extract_entities_from_characters(CHARACTERS_DIR, G)
    if verbose:
        print(f"  After characters: {G.number_of_nodes()} nodes, {G.number_of_edges()} edges")

    # Semi-structured sources (worldbuilding markdown)
    _extract_corponations_from_worldbuilding(WORLDBUILDING_DIR, G)
    if verbose:
        print(f"  After worldbuilding: {G.number_of_nodes()} nodes, {G.number_of_edges()} edges")

    # Save the graph
    save_graph(G)
    if verbose:
        print(f"\nGraph saved to {GRAPH_PATH}")
        print(f"Total: {G.number_of_nodes()} nodes, {G.number_of_edges()} edges")

    return G


def save_graph(G: nx.DiGraph) -> None:
    """Save the graph to JSON."""
    GRAPH_PATH.parent.mkdir(parents=True, exist_ok=True)
    data = nx.node_link_data(G)
    GRAPH_PATH.write_text(json.dumps(data, indent=2, default=str), encoding="utf-8")


def load_graph() -> nx.DiGraph:
    """Load the graph from JSON. Assumes build_graph() has been run."""
    if not GRAPH_PATH.exists():
        raise FileNotFoundError(
            f"Knowledge graph not found at {GRAPH_PATH}. Run 'build' first."
        )
    data = json.loads(GRAPH_PATH.read_text(encoding="utf-8"))
    return nx.node_link_graph(data)


def query_entity(G: nx.DiGraph, name: str) -> dict | None:
    """
    Query an entity and its connections.

    Returns a dict with the entity's attributes and its direct connections,
    or None if the entity is not found.
    """
    if name not in G:
        # Try case-insensitive search
        for node in G.nodes:
            if node.lower() == name.lower():
                name = node
                break
        else:
            return None

    attrs = dict(G.nodes[name])

    # Outgoing relationships
    outgoing = []
    for _, target, edge_data in G.out_edges(name, data=True):
        outgoing.append({
            "target": target,
            "relationship": edge_data.get("relationship", "related_to"),
        })

    # Incoming relationships
    incoming = []
    for source, _, edge_data in G.in_edges(name, data=True):
        incoming.append({
            "source": source,
            "relationship": edge_data.get("relationship", "related_to"),
        })

    return {
        "name": name,
        "attributes": attrs,
        "outgoing": outgoing,
        "incoming": incoming,
    }


def find_related(G: nx.DiGraph, names: list[str], depth: int = 2) -> set[str]:
    """
    Find all entities within `depth` hops of any entity in `names`.
    Useful for discovering relevant context for a scene.
    """
    related = set()
    for name in names:
        if name not in G:
            continue
        # BFS up to depth
        for node in nx.single_source_shortest_path_length(G, name, cutoff=depth):
            related.add(node)
        # Also search incoming edges (reverse)
        G_rev = G.reverse()
        for node in nx.single_source_shortest_path_length(G_rev, name, cutoff=depth):
            related.add(node)
    return related
