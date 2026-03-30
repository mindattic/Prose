"""
Retriever — RAG context retrieval from the canon vector store.

Queries ChromaDB for worldbuilding chunks relevant to a scene's entities,
location, themes, and actions. Returns a structured context package that
grounds the generator in actual canon.

Usage:
    from engine.retriever import retrieve_context
    context = retrieve_context(
        entities=["Kael", "Tessera", "RingoGuard"],
        location="Gary-Hammond ungoverned zone",
        themes=["jurisdictional conflict", "forced augmentation"],
        max_chunks=20,
    )
"""

from .embedder import get_collection
from .graph import load_graph, find_related


def retrieve_context(
    entities: list[str] | None = None,
    location: str | None = None,
    themes: list[str] | None = None,
    query_text: str | None = None,
    max_chunks: int = 20,
    include_graph_context: bool = True,
) -> dict:
    """
    Retrieve relevant canon context for scene generation.

    Combines vector similarity search (semantic) with knowledge graph
    traversal (structural) to build a comprehensive context package.

    Args:
        entities: Named entities involved in the scene
        location: Where the scene takes place
        themes: Thematic keywords (e.g., "exclusion", "augmentation")
        query_text: Free-text query for additional semantic search
        max_chunks: Maximum number of chunks to retrieve
        include_graph_context: Whether to include knowledge graph data

    Returns:
        dict with keys:
            - "canon_text": Concatenated relevant worldbuilding text
            - "sources": List of source files referenced
            - "graph_entities": Entity data from the knowledge graph
            - "related_entities": Entities discovered via graph traversal
    """
    collection = get_collection()
    entities = entities or []
    themes = themes or []

    # Build search queries from the scene parameters
    queries = []
    if entities:
        queries.append(" ".join(entities))
    if location:
        queries.append(location)
    if themes:
        queries.append(" ".join(themes))
    if query_text:
        queries.append(query_text)

    if not queries:
        return {
            "canon_text": "",
            "sources": [],
            "graph_entities": {},
            "related_entities": set(),
        }

    # Query the vector store
    all_results = []
    seen_ids = set()
    chunks_per_query = max(3, max_chunks // len(queries))

    for query in queries:
        results = collection.query(
            query_texts=[query],
            n_results=chunks_per_query,
        )
        if results and results["documents"]:
            for i, doc in enumerate(results["documents"][0]):
                chunk_id = results["ids"][0][i] if results["ids"] else f"q_{i}"
                if chunk_id not in seen_ids:
                    seen_ids.add(chunk_id)
                    metadata = results["metadatas"][0][i] if results["metadatas"] else {}
                    distance = results["distances"][0][i] if results["distances"] else 0
                    all_results.append({
                        "text": doc,
                        "source": metadata.get("source", "unknown"),
                        "heading": metadata.get("heading", ""),
                        "distance": distance,
                    })

    # Sort by relevance (lower distance = more relevant)
    all_results.sort(key=lambda x: x["distance"])

    # Trim to max_chunks
    all_results = all_results[:max_chunks]

    # Build canon text
    sources = set()
    canon_parts = []
    for r in all_results:
        source_label = r["source"]
        heading = r["heading"]
        sources.add(source_label)

        header = f"[Source: {source_label}"
        if heading:
            header += f" > {heading}"
        header += "]"

        canon_parts.append(f"{header}\n{r['text']}")

    canon_text = "\n\n---\n\n".join(canon_parts)

    # Knowledge graph context
    graph_entities = {}
    related_entities = set()

    if include_graph_context and entities:
        try:
            from .graph import query_entity
            G = load_graph()

            for entity_name in entities:
                info = query_entity(G, entity_name)
                if info:
                    graph_entities[entity_name] = info

            related_entities = find_related(G, entities, depth=1)
            # Remove the query entities themselves
            related_entities -= set(entities)

        except FileNotFoundError:
            pass  # Graph not built yet, skip

    return {
        "canon_text": canon_text,
        "sources": sorted(sources),
        "graph_entities": graph_entities,
        "related_entities": related_entities,
    }


def format_context_for_prompt(context: dict) -> str:
    """
    Format the retrieved context into a string suitable for injection
    into an LLM prompt.
    """
    parts = []

    parts.append("=== CANON CONTEXT (from worldbuilding archive) ===")
    parts.append("The following is canonical truth. Do not contradict it.")
    parts.append("Do not invent facts not supported by this context.")
    parts.append("")

    if context["canon_text"]:
        parts.append(context["canon_text"])
        parts.append("")

    if context["graph_entities"]:
        parts.append("=== ENTITY RELATIONSHIPS (from knowledge graph) ===")
        for name, info in context["graph_entities"].items():
            parts.append(f"\n[{name}]")
            attrs = info.get("attributes", {})
            if "type" in attrs:
                parts.append(f"  Type: {attrs['type']}")
            data = attrs.get("data", {})
            for k, v in data.items():
                if k != "name":
                    parts.append(f"  {k}: {v}")
            for rel in info.get("outgoing", []):
                parts.append(f"  → {rel['relationship']} → {rel['target']}")
            for rel in info.get("incoming", []):
                parts.append(f"  ← {rel['relationship']} ← {rel['source']}")
        parts.append("")

    if context["sources"]:
        parts.append(f"Sources referenced: {', '.join(context['sources'])}")

    return "\n".join(parts)
