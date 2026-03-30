"""
Street Samurai — Canon Engine

The layer between worldbuilding (truth) and story generation (fiction).
Embeds canon into a vector store, builds a knowledge graph, retrieves
relevant context for generation, and validates output against canon.

Architecture:
    Canon Vault (worldbuilding/*.md, characters/*.yaml)
        → Embedder (chunks + embeds into ChromaDB)
        → Graph (entities + relationships in NetworkX)
        → Retriever (RAG queries for scene context)
        → Validator (checks generated text against canon)
        → Canon Queue (quarantines new facts for human review)
"""
