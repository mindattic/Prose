# STREET SAMURAI — Story Engine Architecture

## The Problem

You have 50+ worldbuilding documents, 120 corponations, thousands of named entities, a character system with cascading facets, and a narrative bible that demands bureaucratic rigor. You want to generate stories in this world without:

1. **Poisoning the well** — generated content hallucinating facts that contradict canon
2. **Contaminating canon** — generated stories being treated as source of truth
3. **Losing coherence** — the 500th generated scene forgetting what the 1st scene established
4. **Producing slop** — generic AI fiction that ignores the worldbuilding entirely

A Python script can't do this. A wiki can't do this. A neural network alone DEFINITELY can't do this. What can do this is a **pipeline** — multiple systems working together, each handling one part of the problem.

---

## The Architecture: Three Layers

```
┌─────────────────────────────────────────────┐
│           LAYER 1: CANON VAULT              │
│   (Source of truth. Never auto-modified.)    │
│                                             │
│   worldbuilding/*.md  →  Vector Embeddings  │
│   characters/*.yaml   →  Knowledge Graph    │
│   narrative_bible.md  →  Entity Registry    │
└──────────────────┬──────────────────────────┘
                   │ READ ONLY (to generators)
                   ▼
┌─────────────────────────────────────────────┐
│          LAYER 2: GENERATION ENGINE         │
│   (Produces stories grounded in canon.)     │
│                                             │
│   1. Scene Planner (queries canon)          │
│   2. Context Retriever (RAG)                │
│   3. Multi-Voice Generator (6 facets)       │
│   4. Canon Validator (checks output)        │
│   5. Contradiction Flagger                  │
└──────────────────┬──────────────────────────┘
                   │ WRITE (to stories only)
                   ▼
┌─────────────────────────────────────────────┐
│         LAYER 3: STORY ARCHIVE              │
│   (Generated content. NOT canon.)           │
│                                             │
│   stories/*.md       — generated scenes     │
│   canon_queue/*.md   — facts awaiting       │
│                        human promotion      │
│   contradictions.log — flagged conflicts    │
└─────────────────────────────────────────────┘
```

---

## Layer 1: The Canon Vault

### What It Contains
Everything in `worldbuilding/` and `characters/`. These files are the **single source of truth**. Nothing auto-generated ever modifies them. Changes to canon require human decision.

### How It's Indexed

**1. Vector Embeddings (Semantic Search)**

Every worldbuilding document is split into chunks (~500 tokens each), embedded into vectors, and stored in a vector database. When the generator needs context about "what happens when someone is excluded from Ringo," it queries the vector store and retrieves the 10-20 most relevant chunks from across all worldbuilding files.

This is **RAG (Retrieval Augmented Generation)** — the single most important technology for grounding AI output in existing documents rather than training data hallucinations.

**Tech:** ChromaDB (local, free, Python-native) or Pinecone (hosted, scalable). ChromaDB is fine for this scale.

**2. Knowledge Graph (Entity Relationships)**

A structured graph database where every named entity is a node and every relationship is an edge:

```
[Kyle] --worked_for--> [Eastside Vago Kings]
[Kyle] --tested_by--> [NeoCortex Industries]
[NeoCortex] --subsidiary_of--> [Tessera]
[Tessera] --rival_of--> [Zheng-Dao]
[Kyle] --carries--> [Piezoelectric Katana]
[Piezoelectric Katana] --made_from--> [ACNT Composite]
[ACNT Composite] --manufactured_by--> [NovaChem]
```

The graph enforces consistency. If a generated story says Kyle works for Ringo, the graph says "Kyle has no employment relationship with Ringo" and flags the contradiction.

**Tech:** Neo4j (industry standard graph DB) or NetworkX (Python library, simpler, good enough for this scale). Start with NetworkX, migrate to Neo4j if it gets complex.

**3. Entity Registry (Structured YAML)**

Every named entity — corponation, character, location, weapon, drug, technology — gets a structured YAML entry with canonical facts:

```yaml
# registry/entities/tessera.yaml
name: "Tessera CorpoNation"
type: corponation
rank: 1
valuation: "$14.2T"
sector: "BCI, neural technology, cognitive services"
headquarters: "Austin Sovereign Campus"
security_force: "Tessera Security Services (TSS), 68,000"
key_products: ["NovaMind", "CogAd", "Apex Division"]
subsidiaries: ["NeoCortex Industries"]
allies: ["Arcturus Defense Solutions"]
rivals: ["Zheng-Dao Bioelectric"]
exclusion_registry: null  # Tessera doesn't operate one directly
canon_source: "worldbuilding/corponations_01_05.md"
```

This is the machine-readable version of the worldbuilding. The generator queries it. The validator checks against it.

---

## Layer 2: The Generation Engine

This replaces `duo_writer_lit.py`. It's not one script — it's a pipeline of five steps.

### Step 1: Scene Planner

**Input:** A scene prompt (from the narrative bible's filing system, or from human direction).
**Process:**
- Identifies which entities are involved (characters, corponations, locations, technologies)
- Queries the knowledge graph for their relationships
- Queries each involved character's YAML essence for current facet state and active modifiers
- Determines which facets are in tension for each character
- Outputs a **scene brief** — not the scene itself, but the constraints the scene must satisfy

**Output:**
```yaml
scene_brief:
  thread: "001 - The Clinic Job"
  location: "Kindred Medical Collective, Gary-Hammond UGZ"
  entities:
    - kael: {wound: 0.85, ideal: 0.75, ...}  # effective weights with modifiers
    - teen_thief: {wound: 0.9, ideal: 0.1, ...}
    - tessera_officer: {mask: 0.8, shadow: 0.7, ...}
  canon_constraints:
    - "Clinic operates under Helix trademark license, not Helix control"
    - "Gary-Hammond is ungoverned zone, no corpo jurisdiction"
    - "Dead-man switch targets life support, not protagonist"
  facet_tensions:
    - "Kyle: WOUND vs IDEAL (the facility memory vs the code)"
    - "Teen: WOUND overwhelming all other facets (desperation)"
```

### Step 2: Context Retriever (RAG)

**Input:** The scene brief's entities and location.
**Process:**
- Queries the vector store for all relevant worldbuilding chunks
- Retrieves: location details, technology specs, cultural context, relevant corponation procedures, applicable laws/jurisdiction
- Assembles a **canon context package** — typically 3,000-8,000 tokens of retrieved worldbuilding text

**Output:** A context document that the generator receives as grounding material. The generator is instructed: "The following is canonical truth. Do not contradict it. Do not invent facts not supported by it."

### Step 3: Multi-Voice Generator (6 Facets)

This is the creative engine. It replaces the old WOUND/IDEAL dual-voice system with a 6-facet generation approach:

**Option A: Full 6-Voice Drafting**
- Generate 6 draft fragments, one from each facet's perspective
- Each voice receives the scene brief + canon context + its facet's specific concerns
- Cross-critique (each voice reviews the others for facet violations)
- Merge into a single scene

**Option B: Weighted 2-Voice with 4-Facet Modulation (Recommended)**
- WOUND and IDEAL remain the primary drafting voices (they produce the best prose tension)
- But both voices receive the full 6-facet state as context
- ID, SHADOW, MASK, and GHOST act as **modulation instructions** — they shape how WOUND and IDEAL speak, what they notice, what they suppress
- The merge phase uses all 6 facets to determine the final balance

Option B is better because 6 full drafts is expensive (6 LLM calls per scene) and most of the facet interplay can be captured by modulating the two primary voices. The 6-facet state is in the prompt; the prose comes from the tension between two voices informed by all six.

**Output format:** The scene, in the bureaucratic document format specified by the narrative bible. Includes: document type header, source citations, any corrections/retractions, and cross-references to worldbuilding files.

### Step 4: Canon Validator

**Input:** The generated scene.
**Process:**
- Extracts all factual claims from the scene (entity names, relationships, locations, technologies, events)
- Checks each claim against the entity registry and knowledge graph
- Flags contradictions: "Scene says Kyle's blade is titanium. Canon says ACNT composite with piezoelectric layer."
- Flags inventions: "Scene introduces a character named 'Dex' not in any canon file. New entity or hallucination?"
- Flags tone violations: "Scene contains a sentence longer than 25 words" (if style rules are enforced)

**Output:** A validation report. Green (no contradictions), yellow (new entities that need human review), red (contradicts established canon, must be revised or retracted).

### Step 5: Canon Queue

**Input:** Validated scene + validation report.
**Process:**
- Any new facts, characters, or relationships introduced in the scene that are NOT contradictions are extracted and placed in `canon_queue/`
- These are proposals, not facts. They become canon only when a human reviews and promotes them to the entity registry and worldbuilding files
- The scene itself is saved to `stories/` with a metadata header noting its canon status:

```yaml
---
canon_status: "draft"  # draft | reviewed | canonical
thread: "001"
scene: "001-01"
generated: "2026-03-29"
validator_status: "yellow"  # green | yellow | red
new_entities: ["teen_thief (unnamed)", "clinic_staff"]
contradictions: []
---
```

---

## Layer 3: The Story Archive

### File Structure

```
stories/
├── thread_001/
│   ├── scene_001_01.md          # generated scene
│   ├── scene_001_01_meta.yaml   # validation metadata
│   ├── scene_001_02.md
│   └── ...
├── thread_002/
│   └── ...
canon_queue/
├── pending/
│   ├── new_entity_teen_thief.yaml
│   └── new_relationship_kael_clinic.yaml
├── promoted/                     # moved here after human approval
└── rejected/                     # moved here if contradicts canon
```

### The Promotion Pipeline

```
Generated Story → Validator → Canon Queue (pending)
                                    ↓
                            Human Review
                           /            \
                    Promote              Reject
                    (→ canon)            (→ rejected/)
```

Nothing becomes canon without human review. The AI proposes. The human disposes.

---

## The Tech Stack

### What You Need

| Component | Technology | Purpose |
|-----------|-----------|---------|
| Vector Store | ChromaDB | Semantic search over worldbuilding docs |
| Knowledge Graph | NetworkX → Neo4j | Entity relationships, contradiction detection |
| Entity Registry | YAML files | Machine-readable canon facts |
| LLM | Claude API (Anthropic) | Scene planning, generation, validation |
| Embeddings | Voyage AI or local model | Convert text chunks to vectors |
| Orchestrator | Python (FastAPI or CLI) | Pipeline coordination |
| Storage | Git repo (this repo) | Version control for everything |

### What You DON'T Need

- A wiki (too unstructured, no validation, anyone can edit anything)
- A neural network you train yourself (the LLM is the neural network; you don't train it, you ground it)
- A game engine (this isn't a simulation; it's a constrained generation pipeline)
- A database server (ChromaDB and NetworkX run in-process, no server needed)

### Estimated Complexity

This is not a weekend project, but it's not a year-long effort either. The core pipeline:

1. **Embedding the worldbuilding** — 1-2 days (script to chunk docs, embed, store in ChromaDB)
2. **Entity registry extraction** — 2-3 days (parse existing worldbuilding into structured YAML)
3. **Knowledge graph construction** — 2-3 days (build graph from entity registry)
4. **Scene planner** — 1-2 days (query graph + embeddings, produce scene brief)
5. **Generator (replacing duo_writer_lit.py)** — 2-3 days (multi-step LLM pipeline with canon context)
6. **Validator** — 2-3 days (extract claims, check against graph/registry)
7. **Canon queue + promotion flow** — 1-2 days (file management, human review interface)

**Total: ~2-3 weeks of focused development for a working v1.**

---

## Why This Works (And Wikis Don't)

A wiki is a flat document store. Anyone (or any AI) can write anything. There's no validation layer. There's no separation between canon and speculation. A wiki with 50,000 words of worldbuilding and 100,000 words of generated stories becomes a swamp — the AI reads its own previous hallucinations as canon and compounds them.

This architecture works because:

1. **Canon is read-only to the AI.** The generator can read worldbuilding but never write to it. Contamination is structurally impossible.
2. **Every generation is grounded.** RAG retrieval means the AI is always looking at the actual worldbuilding documents, not its training data. If the worldbuilding says BallCer stops gauss rounds, the AI says BallCer stops gauss rounds — even if its training data says something different.
3. **Contradictions are caught.** The validator checks every generated fact against the knowledge graph. Hallucinations are flagged before they reach the story archive.
4. **New facts are quarantined.** Generated content that introduces new information goes to the canon queue, not to the canon vault. The well is protected by a one-way valve.
5. **The human is the gatekeeper.** Nothing becomes canon without human review. The AI does the heavy lifting of generation and validation. The human makes the decisions that matter.

---

## The Narrative Bible Connection

The narrative bible describes a bureaucratic filing system where scenes transition through cross-reference, errors are retracted not corrected, and every document cites its sources. This architecture IS that filing system, implemented as software:

- The **entity registry** is the filing cabinet
- The **knowledge graph** is the cross-reference index
- The **scene planner** is the filing clerk who pulls relevant documents
- The **generator** is the field agent writing the report
- The **validator** is the compliance officer checking the report against records
- The **canon queue** is the inbox on the supervisor's desk
- The **human** is the supervisor who stamps "APPROVED" or "RETRACTED"

The story's structure and the software's structure are the same structure. The machine of machines, all the way down.

---

## Next Step

Build it. The repo already has the worldbuilding. The next commit should contain:

```
engine/
├── embedder.py          # Chunk and embed worldbuilding docs
├── graph.py             # Build and query knowledge graph
├── registry.py          # Parse and manage entity registry
├── planner.py           # Scene planning from canon
├── generator.py         # Multi-voice generation with RAG
├── validator.py         # Canon validation
├── pipeline.py          # Orchestrate the full flow
├── promote.py           # Canon queue management
└── config.yaml          # API keys, model settings, paths
registry/
├── entities/            # Structured YAML for every named entity
├── relationships/       # Relationship definitions
└── schema.yaml          # Entity schema definition
```

The worldbuilding is the foundation. The engine is the machine that builds on it without breaking it.
