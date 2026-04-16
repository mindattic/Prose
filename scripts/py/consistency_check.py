"""
Consistency Check Pipeline -- Cross-Entity Contradiction Detection

Finds semantic contradictions between worldbuilding entities. Unlike the fact
pipeline (which extracts SPO triples), this script does SEMANTIC cross-checking:
does entity A's claims contradict entity B's claims, even when phrased differently?

THREE CHECKING STRATEGIES:
  local    -- all entities within the same repo checked against each other
  named    -- entities that reference each other by name are checked as a pair
  semantic -- 20 most embedding-similar entities checked per entity (uses facts.db)

HARDCODED WORLD RULES (always checked):
  - No city police (Arcturus Civil Security only, Meridian PD dissolved 2208)
  - Currency is Φ (Quanta), never dollars or credits
  - Iowan Behemoths are autonomous machines, not synthetic life
  - City name is GLMZ, not "Meridian City" or "Meridian 88" (only the Behemoth keeps that name)

USAGE:
  python consistency_check.py              # all three strategies
  python consistency_check.py --strategy local    # within-repo only
  python consistency_check.py --strategy named    # named-reference pairs only
  python consistency_check.py --strategy semantic # embedding-nearest only
  python consistency_check.py --repo weaponry     # check one repo
  python consistency_check.py --flagged           # show unresolved contradictions
  python consistency_check.py --critical          # show only critical contradictions
  python consistency_check.py --limit 200         # process only 200 entities
"""

import json
import os
import re
import asyncio
import sqlite3
import glob
import random
import struct
import argparse
from pathlib import Path
from datetime import datetime
from collections import defaultdict

from rich.console import Console
from rich.table import Table
from rich.progress import Progress

from constants import ANTHROPIC_API_KEY, DATA_DIR, DB_PATH, CONCURRENCY, REPOS

# ── Config ────────────────────────────────────────────────────────────────────
CONSISTENCY_DB_PATH = os.getenv("CONSISTENCY_DB_PATH", "consistency.db")
HAIKU_MODEL = "claude-haiku-4-5-20251001"
SONNET_MODEL = "claude-sonnet-4-6"
SEMANTIC_NEIGHBORS = 20
CHECKPOINT_SIZE = 30

console = Console()

# ── Known world rules (always checked against) ───────────────────────────────
WORLD_RULES = [
    {
        "rule": "no_city_police",
        "description": "No city police exist. Arcturus Civil Security is the closest equivalent. Meridian PD dissolved in 2208. There are no Metro Police, GLMZ Police, or any other municipal law enforcement.",
        "violation_type": "lore",
        "severity": "critical",
    },
    {
        "rule": "quanta_currency",
        "description": "The currency of GLMZ is Φ (Quanta). Dollar signs ($), 'credits', or other generic currency names are incorrect.",
        "violation_type": "lore",
        "severity": "moderate",
    },
    {
        "rule": "iowan_behemoths_not_alive",
        "description": "Iowan Behemoths are autonomous machines, NOT synthetic life. They are not alive, not sentient, not sapient. Never describe them as living entities or beings.",
        "violation_type": "lore",
        "severity": "critical",
    },
    {
        "rule": "city_name_glmz",
        "description": "The city is called GLMZ. Do not call it 'Meridian City', 'Meridian 88', or 'New Meridian'. Only the specific Iowan Behemoth machine is allowed to be called 'Meridian 88'.",
        "violation_type": "lore",
        "severity": "moderate",
    },
]

# ── Prompts ───────────────────────────────────────────────────────────────────
WORLD_RULES_BLOCK = "\n".join(
    f"RULE ({r['rule']}): {r['description']}" for r in WORLD_RULES
)

CONTRADICTION_SYSTEM = f"""You are a worldbuilding consistency checker for GLMZ (year 2200), a near-future cyberpunk city.

You will be given two or more worldbuilding entity descriptions. Your job is to find SPECIFIC contradictions between them.

Types of contradictions to detect:
- factual: Basic facts that conflict (manufacturer names, tier levels, relationships)
- timeline: Different dates or sequences for the same event
- ownership: Entity A says owned/made by Corp X, Entity B says Corp Y
- geography: Different district or location claims for the same place or entity
- lore: Violates established world canon (see rules below)

WORLD CANON RULES (check every entity against these regardless of what else is present):
{WORLD_RULES_BLOCK}

IMPORTANT GUIDELINES:
- Only report GENUINE contradictions, not just differences in detail level
- Do not flag missing information as a contradiction
- Each contradiction must cite the EXACT text from each entity that conflicts
- If there are no contradictions, return an empty array
- Be specific: vague "these might conflict" entries are useless

Return ONLY a JSON array. Each object has:
  entity_a_claim: exact quote or paraphrase from first entity
  entity_b_claim: exact quote or paraphrase from second entity (or "WORLD RULE" for canon violations)
  contradiction_type: one of factual|timeline|ownership|geography|lore
  severity: minor|moderate|critical
  suggested_resolution: one sentence describing how to fix it

If no contradictions found, return: []"""


WORLD_RULE_CHECK_SYSTEM = f"""You are checking a single worldbuilding entity for violations of known world canon rules.

WORLD CANON RULES:
{WORLD_RULES_BLOCK}

Check the provided entity for ANY violation of these rules. Return ONLY a JSON array of violations found.
Each object has:
  rule_violated: the rule name (e.g. "no_city_police")
  entity_claim: exact quote or paraphrase from the entity that violates the rule
  world_rule_claim: the correct canon rule being violated
  contradiction_type: always "lore"
  severity: minor|moderate|critical
  suggested_resolution: one sentence fix

If no violations found, return: []"""


# ── Database ──────────────────────────────────────────────────────────────────
def init_consistency_db():
    """Create consistency.db tables if they don't exist."""
    conn = sqlite3.connect(CONSISTENCY_DB_PATH)
    c = conn.cursor()

    c.execute("""
        CREATE TABLE IF NOT EXISTS contradictions (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            entity_a_file TEXT NOT NULL,
            entity_a_name TEXT NOT NULL,
            entity_b_file TEXT NOT NULL,
            entity_b_name TEXT NOT NULL,
            contradiction_type TEXT NOT NULL,
            entity_a_claim TEXT NOT NULL,
            entity_b_claim TEXT NOT NULL,
            severity TEXT NOT NULL DEFAULT 'minor',
            suggested_resolution TEXT,
            resolved INTEGER DEFAULT 0,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        )
    """)

    c.execute("""
        CREATE TABLE IF NOT EXISTS check_log (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            entity_file TEXT NOT NULL,
            entities_checked_against INTEGER NOT NULL,
            contradictions_found INTEGER NOT NULL,
            timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        )
    """)

    c.execute("CREATE INDEX IF NOT EXISTS idx_contra_entity_a ON contradictions(entity_a_file)")
    c.execute("CREATE INDEX IF NOT EXISTS idx_contra_entity_b ON contradictions(entity_b_file)")
    c.execute("CREATE INDEX IF NOT EXISTS idx_contra_severity ON contradictions(severity)")
    c.execute("CREATE INDEX IF NOT EXISTS idx_contra_resolved ON contradictions(resolved)")
    c.execute("CREATE INDEX IF NOT EXISTS idx_check_log_file ON check_log(entity_file)")

    conn.commit()
    conn.close()


def get_consistency_connection():
    return sqlite3.connect(CONSISTENCY_DB_PATH)


# ── File helpers ──────────────────────────────────────────────────────────────
def get_json_files(repo=None, limit=None):
    """Get all JSON entity files, optionally filtered by repo and limited."""
    base = Path(DATA_DIR)
    pattern = base / repo / "*.json" if repo else base / "**" / "*.json"

    skip_names = {"subsidiary_index.json", "trivia.json", "tts_rules.json"}
    files = []
    for fp in glob.glob(str(pattern), recursive=True):
        fname = os.path.basename(fp)
        if fname in skip_names:
            continue
        if "/graph/" in fp.replace("\\", "/"):
            continue
        files.append(fp)

    files = sorted(files)
    if limit:
        files = files[:limit]
    return files


def load_entity(filepath):
    """Load a JSON entity file. Returns (entity_dict, entity_name) or (None, None)."""
    try:
        with open(filepath, "r", encoding="utf-8") as f:
            entity = json.load(f)

        if not isinstance(entity, dict):
            return None, None

        entity_name = entity.get("name", entity.get("title", entity.get("quote", os.path.basename(filepath))))
        return entity, entity_name

    except Exception:
        return None, None


def entity_to_text(entity, filepath, max_chars=2500):
    """Convert an entity dict to a compact text block for the prompt."""
    name = entity.get("name", entity.get("title", os.path.basename(filepath)))
    repo = Path(filepath).parent.name

    # Build a summary of key fields
    lines = [f"Entity: {name}", f"Repo: {repo}"]

    priority_fields = [
        "description", "body", "summary", "type", "category",
        "manufacturer", "owner", "affiliation", "parent_corponation",
        "location", "district", "headquarters",
        "tier_availability", "tier", "cost", "price",
        "founded", "year", "date",
        "species", "classification",
    ]

    for field in priority_fields:
        val = entity.get(field)
        if val and isinstance(val, (str, int, float)):
            lines.append(f"{field}: {val}")

    # Include related_entities if present
    related = entity.get("related_entities", [])
    if related and isinstance(related, list):
        lines.append(f"related_entities: {', '.join(str(r) for r in related[:10])}")

    text = "\n".join(lines)
    return text[:max_chars]


# ── Already-checked pairs ─────────────────────────────────────────────────────
def get_checked_files():
    """Return set of files already logged in check_log."""
    conn = get_consistency_connection()
    c = conn.cursor()
    c.execute("SELECT entity_file FROM check_log")
    result = {row[0] for row in c.fetchall()}
    conn.close()
    return result


def pair_already_logged(file_a, file_b):
    """Check if (a,b) or (b,a) contradiction pair already exists."""
    conn = get_consistency_connection()
    c = conn.cursor()
    c.execute(
        "SELECT COUNT(*) FROM contradictions WHERE (entity_a_file=? AND entity_b_file=?) OR (entity_a_file=? AND entity_b_file=?)",
        (file_a, file_b, file_b, file_a),
    )
    count = c.fetchone()[0]
    conn.close()
    return count > 0


# ── Parse contradiction response ──────────────────────────────────────────────
def parse_contradiction_json(text):
    """Parse Claude's contradiction list. Returns list of dicts or []."""
    if "```" in text:
        match = re.search(r'\[[\s\S]*\]', text)
        if match:
            text = match.group(0)
        else:
            text = text.replace("```json", "").replace("```", "").strip()

    stripped = text.strip()
    if not stripped.startswith("["):
        match = re.search(r'\[[\s\S]*\]', stripped)
        if match:
            stripped = match.group(0)

    try:
        data = json.loads(stripped)
        return data if isinstance(data, list) else []
    except json.JSONDecodeError:
        try:
            match = re.search(r'\[[\s\S]*?\]', stripped)
            if match:
                data = json.loads(match.group(0))
                return data if isinstance(data, list) else []
        except Exception:
            pass
        return []


# ── Store contradictions ──────────────────────────────────────────────────────
def store_contradictions(conn, file_a, name_a, file_b, name_b, contradictions):
    """Insert contradiction rows and update check_log."""
    c = conn.cursor()
    count = 0

    valid_types = {"factual", "timeline", "ownership", "geography", "lore"}
    valid_severities = {"minor", "moderate", "critical"}

    for item in contradictions:
        if not isinstance(item, dict):
            continue

        # Normalize fields with fallbacks
        contra_type = item.get("contradiction_type", "factual")
        if contra_type not in valid_types:
            contra_type = "factual"

        severity = item.get("severity", "minor")
        if severity not in valid_severities:
            severity = "minor"

        claim_a = str(item.get("entity_a_claim", item.get("entity_claim", "")))[:1000]
        claim_b = str(item.get("entity_b_claim", item.get("world_rule_claim", "")))[:1000]
        resolution = str(item.get("suggested_resolution", ""))[:500]

        if not claim_a or not claim_b:
            continue

        # Use rule-violation form for world-rule checks
        if "rule_violated" in item:
            name_b_display = f"WORLD RULE: {item.get('rule_violated', 'canon')}"
        else:
            name_b_display = name_b

        c.execute(
            """INSERT INTO contradictions
               (entity_a_file, entity_a_name, entity_b_file, entity_b_name,
                contradiction_type, entity_a_claim, entity_b_claim,
                severity, suggested_resolution)
               VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)""",
            (file_a, name_a, file_b, name_b_display,
             contra_type, claim_a, claim_b, severity, resolution),
        )
        count += 1

    return count


def log_check(conn, entity_file, entities_checked_against, contradictions_found):
    conn.cursor().execute(
        "INSERT INTO check_log (entity_file, entities_checked_against, contradictions_found) VALUES (?, ?, ?)",
        (entity_file, entities_checked_against, contradictions_found),
    )
    conn.commit()


# ── Async API calls ───────────────────────────────────────────────────────────
async def check_pair_via_api(text_a, text_b, name_a, name_b, client, semaphore, use_sonnet=False):
    """Ask Claude to find contradictions between two entity descriptions."""
    async with semaphore:
        model = SONNET_MODEL if use_sonnet else HAIKU_MODEL

        user_content = (
            f"ENTITY A: {name_a}\n{text_a}\n\n"
            f"---\n\n"
            f"ENTITY B: {name_b}\n{text_b}\n\n"
            "Find all contradictions between these two entities."
        )

        max_retries = 3
        for attempt in range(max_retries):
            try:
                response = await client.messages.create(
                    model=model,
                    max_tokens=1024,
                    system=CONTRADICTION_SYSTEM,
                    messages=[{"role": "user", "content": user_content}],
                )
                return parse_contradiction_json(response.content[0].text.strip())

            except Exception as e:
                err = str(e).lower()
                if "overloaded" in err or "rate" in err:
                    wait = (attempt + 1) * 10
                    console.print(f"  [yellow]Rate limited ({model}), waiting {wait}s...[/yellow]")
                    await asyncio.sleep(wait)
                    continue
                console.print(f"  [red]API error: {e}[/red]")
                return []

    return []


async def check_world_rules_via_api(text_a, name_a, client, semaphore):
    """Check a single entity against world canon rules."""
    async with semaphore:
        user_content = f"ENTITY: {name_a}\n{text_a}\n\nCheck for world canon violations."

        max_retries = 3
        for attempt in range(max_retries):
            try:
                response = await client.messages.create(
                    model=HAIKU_MODEL,
                    max_tokens=512,
                    system=WORLD_RULE_CHECK_SYSTEM,
                    messages=[{"role": "user", "content": user_content}],
                )
                return parse_contradiction_json(response.content[0].text.strip())

            except Exception as e:
                err = str(e).lower()
                if "overloaded" in err or "rate" in err:
                    wait = (attempt + 1) * 10
                    await asyncio.sleep(wait)
                    continue
                return []

    return []


# ── Strategy helpers ──────────────────────────────────────────────────────────
def group_by_repo(files):
    """Group file list by repo (parent directory name)."""
    groups = defaultdict(list)
    for fp in files:
        groups[Path(fp).parent.name].append(fp)
    return groups


def find_named_pairs(files):
    """
    Find pairs where entity A's text contains entity B's name.
    Returns list of (file_a, file_b) tuples.
    """
    # Build name -> file mapping
    name_to_file = {}
    for fp in files:
        entity, name = load_entity(fp)
        if entity and name:
            name_to_file[name.lower()] = fp

    pairs = set()

    for fp in files:
        entity, name = load_entity(fp)
        if not entity:
            continue

        # Serialize entity fields to text for name scanning
        text = json.dumps(entity, ensure_ascii=False).lower()

        for other_name, other_fp in name_to_file.items():
            if other_fp == fp:
                continue
            # Must be a reasonably specific name (skip 1-2 word generic matches)
            if len(other_name) < 6:
                continue
            if other_name in text:
                key = tuple(sorted([fp, other_fp]))
                pairs.add(key)

    return list(pairs)


def load_embeddings_from_facts_db():
    """
    Load triple embeddings from facts.db grouped by source_file.
    Returns dict: source_file -> averaged embedding vector (as list of floats).
    Returns empty dict if facts.db not available or has no embeddings.
    """
    if not os.path.exists(DB_PATH):
        return {}

    try:
        conn = sqlite3.connect(DB_PATH)
        c = conn.cursor()
        c.execute("SELECT source_file, embedding FROM triples WHERE embedding IS NOT NULL LIMIT 50000")
        rows = c.fetchall()
        conn.close()
    except Exception:
        return {}

    if not rows:
        return {}

    file_embeddings = defaultdict(list)
    for source_file, emb_blob in rows:
        if emb_blob is None:
            continue
        try:
            # Embeddings are stored as raw float32 bytes (from sentence-transformers)
            dim = len(emb_blob) // 4
            vec = list(struct.unpack(f"{dim}f", emb_blob))
            file_embeddings[source_file].append(vec)
        except Exception:
            continue

    # Average all triple embeddings per file to get file-level embedding
    averaged = {}
    for fp, vecs in file_embeddings.items():
        if not vecs:
            continue
        dim = len(vecs[0])
        avg = [sum(v[i] for v in vecs) / len(vecs) for i in range(dim)]
        averaged[fp] = avg

    return averaged


def cosine_similarity(a, b):
    """Compute cosine similarity between two float vectors."""
    dot = sum(x * y for x, y in zip(a, b))
    mag_a = sum(x * x for x in a) ** 0.5
    mag_b = sum(x * x for x in b) ** 0.5
    if mag_a == 0 or mag_b == 0:
        return 0.0
    return dot / (mag_a * mag_b)


def find_semantic_neighbors(files, embeddings, n=SEMANTIC_NEIGHBORS):
    """
    For each file, find n most semantically similar files using embeddings.
    Returns dict: filepath -> list of similar filepaths.
    """
    # Only files that have embeddings
    embedded_files = [f for f in files if f in embeddings]

    neighbors = {}
    for fp in embedded_files:
        vec_a = embeddings[fp]
        scores = []
        for other_fp in embedded_files:
            if other_fp == fp:
                continue
            vec_b = embeddings[other_fp]
            if len(vec_a) != len(vec_b):
                continue
            sim = cosine_similarity(vec_a, vec_b)
            scores.append((sim, other_fp))

        scores.sort(reverse=True)
        neighbors[fp] = [fp2 for _, fp2 in scores[:n]]

    return neighbors


# ── Main strategies ───────────────────────────────────────────────────────────
async def run_local_strategy(files, client, semaphore, conn, checked_files):
    """Check all entities within each repo against each other."""
    groups = group_by_repo(files)
    total_checked = 0
    total_contradictions = 0

    console.rule("[bold]Strategy: Local (within-repo)[/bold]")

    for repo_name, repo_files in groups.items():
        if len(repo_files) < 2:
            continue

        console.print(f"  Repo [cyan]{repo_name}[/cyan]: {len(repo_files)} entities")

        # For large repos, sample to keep it tractable
        if len(repo_files) > 100:
            repo_files = random.sample(repo_files, 100)

        # Generate all pairs within this repo
        pairs = []
        for i, fa in enumerate(repo_files):
            for fb in repo_files[i + 1:]:
                if not pair_already_logged(fa, fb):
                    pairs.append((fa, fb))

        if not pairs:
            console.print(f"    [dim]All pairs already checked.[/dim]")
            continue

        coros = []
        for fa, fb in pairs:
            entity_a, name_a = load_entity(fa)
            entity_b, name_b = load_entity(fb)
            if entity_a is None or entity_b is None:
                continue

            text_a = entity_to_text(entity_a, fa)
            text_b = entity_to_text(entity_b, fb)
            coros.append((fa, name_a, fb, name_b, text_a, text_b))

        with Progress() as progress:
            task = progress.add_task(f"  {repo_name} pairs...", total=len(coros))

            for i in range(0, len(coros), CHECKPOINT_SIZE):
                batch = coros[i: i + CHECKPOINT_SIZE]
                tasks = [
                    check_pair_via_api(text_a, text_b, name_a, name_b, client, semaphore)
                    for (fa, name_a, fb, name_b, text_a, text_b) in batch
                ]

                results = await asyncio.gather(*tasks)

                for (fa, name_a, fb, name_b, _, _), contradictions in zip(batch, results):
                    count = store_contradictions(conn, fa, name_a, fb, name_b, contradictions)
                    total_contradictions += count
                    log_check(conn, fa, 1, count)
                    total_checked += 1
                    progress.update(task, advance=1)

    console.print(f"  [green]Local strategy: {total_checked} pairs checked, {total_contradictions} contradictions found.[/green]")
    return total_contradictions


async def run_named_strategy(files, client, semaphore, conn):
    """Check entities that reference each other by name."""
    console.rule("[bold]Strategy: Named References[/bold]")

    console.print("  Building named reference index...")
    pairs = find_named_pairs(files)

    # Filter already-checked pairs
    pairs = [(a, b) for a, b in pairs if not pair_already_logged(a, b)]

    console.print(f"  Named pairs to check: {len(pairs)}")
    if not pairs:
        console.print("  [yellow]No new named pairs found.[/yellow]")
        return 0

    total_contradictions = 0
    prep = []
    for fa, fb in pairs:
        entity_a, name_a = load_entity(fa)
        entity_b, name_b = load_entity(fb)
        if entity_a is None or entity_b is None:
            continue
        text_a = entity_to_text(entity_a, fa)
        text_b = entity_to_text(entity_b, fb)
        prep.append((fa, name_a, fb, name_b, text_a, text_b))

    with Progress() as progress:
        task = progress.add_task("Named pairs...", total=len(prep))

        for i in range(0, len(prep), CHECKPOINT_SIZE):
            batch = prep[i: i + CHECKPOINT_SIZE]
            tasks = [
                check_pair_via_api(text_a, text_b, name_a, name_b, client, semaphore)
                for (fa, name_a, fb, name_b, text_a, text_b) in batch
            ]

            results = await asyncio.gather(*tasks)

            for (fa, name_a, fb, name_b, _, _), contradictions in zip(batch, results):
                # Named references get Sonnet escalation for critical findings
                critical = [c for c in contradictions if c.get("severity") == "critical"]
                if critical:
                    extra = await check_pair_via_api(
                        entity_to_text(load_entity(fa)[0], fa),
                        entity_to_text(load_entity(fb)[0], fb),
                        name_a, name_b, client, semaphore, use_sonnet=True
                    )
                    contradictions = extra or contradictions

                count = store_contradictions(conn, fa, name_a, fb, name_b, contradictions)
                total_contradictions += count
                log_check(conn, fa, 1, count)
                progress.update(task, advance=1)

    console.print(f"  [green]Named strategy: {len(prep)} pairs checked, {total_contradictions} contradictions found.[/green]")
    return total_contradictions


async def run_semantic_strategy(files, client, semaphore, conn):
    """Use embeddings from facts.db to find similar entities and check them."""
    console.rule("[bold]Strategy: Semantic Neighbors (embedding-nearest)[/bold]")

    console.print("  Loading embeddings from facts.db...")
    embeddings = load_embeddings_from_facts_db()

    if not embeddings:
        console.print("  [yellow]No embeddings found in facts.db. Run the fact pipeline first to generate embeddings.[/yellow]")
        console.print("  [dim]Falling back to random sampling across repos...[/dim]")
        # Graceful fallback: random cross-repo pairs
        cross_pairs = []
        groups = group_by_repo(files)
        repo_names = list(groups.keys())
        for _ in range(min(200, len(files))):
            if len(repo_names) < 2:
                break
            repo_a, repo_b = random.sample(repo_names, 2)
            if not groups[repo_a] or not groups[repo_b]:
                continue
            fa = random.choice(groups[repo_a])
            fb = random.choice(groups[repo_b])
            cross_pairs.append((fa, fb))
        embeddings = None
    else:
        console.print(f"  Embeddings loaded for {len(embeddings)} files.")
        neighbors = find_semantic_neighbors(files, embeddings, n=SEMANTIC_NEIGHBORS)
        cross_pairs = []
        for fa, neighbor_list in neighbors.items():
            for fb in neighbor_list:
                if not pair_already_logged(fa, fb):
                    cross_pairs.append((fa, fb))

    cross_pairs = list({tuple(sorted(p)) for p in cross_pairs})  # deduplicate
    cross_pairs = [p for p in cross_pairs if not pair_already_logged(p[0], p[1])]

    console.print(f"  Semantic pairs to check: {len(cross_pairs)}")
    if not cross_pairs:
        console.print("  [yellow]No new semantic pairs to check.[/yellow]")
        return 0

    total_contradictions = 0
    prep = []
    for fa, fb in cross_pairs:
        entity_a, name_a = load_entity(fa)
        entity_b, name_b = load_entity(fb)
        if entity_a is None or entity_b is None:
            continue
        text_a = entity_to_text(entity_a, fa)
        text_b = entity_to_text(entity_b, fb)
        prep.append((fa, name_a, fb, name_b, text_a, text_b))

    with Progress() as progress:
        task = progress.add_task("Semantic pairs...", total=len(prep))

        for i in range(0, len(prep), CHECKPOINT_SIZE):
            batch = prep[i: i + CHECKPOINT_SIZE]
            tasks = [
                check_pair_via_api(text_a, text_b, name_a, name_b, client, semaphore)
                for (fa, name_a, fb, name_b, text_a, text_b) in batch
            ]

            results = await asyncio.gather(*tasks)

            for (fa, name_a, fb, name_b, _, _), contradictions in zip(batch, results):
                count = store_contradictions(conn, fa, name_a, fb, name_b, contradictions)
                total_contradictions += count
                log_check(conn, fa, 1, count)
                progress.update(task, advance=1)

    console.print(f"  [green]Semantic strategy: {len(prep)} pairs checked, {total_contradictions} contradictions found.[/green]")
    return total_contradictions


async def run_world_rules_check(files, client, semaphore, conn):
    """Check every entity individually against the hardcoded world canon rules."""
    console.rule("[bold]World Canon Rule Checks[/bold]")

    checked_files = get_checked_files()
    # Filter to files that haven't been world-rule-checked yet
    # We track this by looking for check_log entries where entity_b_name contains "WORLD RULE"
    conn2 = get_consistency_connection()
    c2 = conn2.cursor()
    c2.execute("SELECT DISTINCT entity_a_file FROM contradictions WHERE entity_b_name LIKE 'WORLD RULE%'")
    already_world_checked = {row[0] for row in c2.fetchall()}
    conn2.close()

    to_check = [f for f in files if f not in already_world_checked]
    console.print(f"  Entities to world-rule-check: {len(to_check)}")

    if not to_check:
        console.print("  [yellow]All entities already world-rule-checked.[/yellow]")
        return 0

    total_violations = 0
    prep = []
    for fp in to_check:
        entity, name = load_entity(fp)
        if entity is None:
            continue
        prep.append((fp, name, entity_to_text(entity, fp)))

    world_rule_placeholder_file = "WORLD_RULES"
    world_rule_placeholder_name = "World Canon Rules"

    with Progress() as progress:
        task = progress.add_task("World rule checks...", total=len(prep))

        for i in range(0, len(prep), CHECKPOINT_SIZE):
            batch = prep[i: i + CHECKPOINT_SIZE]
            tasks = [
                check_world_rules_via_api(text, name, client, semaphore)
                for (fp, name, text) in batch
            ]

            results = await asyncio.gather(*tasks)

            for (fp, name, _), violations in zip(batch, results):
                count = store_contradictions(
                    conn, fp, name,
                    world_rule_placeholder_file, world_rule_placeholder_name,
                    violations
                )
                total_violations += count
                log_check(conn, fp, 0, count)
                progress.update(task, advance=1)

    console.print(f"  [green]World rule check: {len(prep)} entities checked, {total_violations} violations found.[/green]")
    return total_violations


# ── Main runner ───────────────────────────────────────────────────────────────
def run_consistency_check(strategy=None, repo=None, limit=None, concurrency=None):
    """Entry point. strategy=None means run all three."""
    asyncio.run(_run_consistency_async(strategy, repo, limit, concurrency))


async def _run_consistency_async(strategy=None, repo=None, limit=None, concurrency=None):
    import anthropic

    init_consistency_db()

    files = get_json_files(repo=repo, limit=limit)
    actual_concurrency = concurrency or min(CONCURRENCY, 10)

    console.print("[bold red]Street Samurai -- Consistency Check Pipeline[/bold red]")
    console.print(f"  Files      : {len(files)}")
    console.print(f"  Strategy   : {strategy or 'all (local + named + semantic + world rules)'}")
    console.print(f"  Repo filter: {repo or 'all'}")
    console.print(f"  Concurrency: {actual_concurrency}")
    console.print()

    client = anthropic.AsyncAnthropic(api_key=ANTHROPIC_API_KEY)
    semaphore = asyncio.Semaphore(actual_concurrency)

    conn = get_consistency_connection()
    total = 0

    run_all = strategy is None

    if run_all or strategy == "local":
        checked_files = get_checked_files()
        total += await run_local_strategy(files, client, semaphore, conn, checked_files)

    if run_all or strategy == "named":
        total += await run_named_strategy(files, client, semaphore, conn)

    if run_all or strategy == "semantic":
        total += await run_semantic_strategy(files, client, semaphore, conn)

    # World rules always run unless a specific non-world strategy is selected
    if run_all:
        total += await run_world_rules_check(files, client, semaphore, conn)

    conn.close()

    console.print(f"\n[bold green]Consistency check complete! Total contradictions found: {total}[/bold green]")
    show_consistency_stats()


# ── Stats & view modes ────────────────────────────────────────────────────────
def show_consistency_stats():
    """Show consistency.db statistics dashboard."""
    conn = get_consistency_connection()
    c = conn.cursor()

    stats = {}

    c.execute("SELECT COUNT(*) FROM contradictions WHERE resolved = 0")
    stats["Unresolved contradictions"] = c.fetchone()[0]

    c.execute("SELECT COUNT(*) FROM contradictions WHERE severity = 'critical' AND resolved = 0")
    stats["Critical (unresolved)"] = c.fetchone()[0]

    c.execute("SELECT COUNT(*) FROM contradictions WHERE severity = 'moderate' AND resolved = 0")
    stats["Moderate (unresolved)"] = c.fetchone()[0]

    c.execute("SELECT COUNT(*) FROM contradictions WHERE severity = 'minor' AND resolved = 0")
    stats["Minor (unresolved)"] = c.fetchone()[0]

    c.execute("SELECT COUNT(*) FROM contradictions WHERE resolved = 1")
    stats["Resolved"] = c.fetchone()[0]

    c.execute("SELECT COUNT(*) FROM check_log")
    stats["Entity checks logged"] = c.fetchone()[0]

    c.execute("SELECT COUNT(DISTINCT contradiction_type) FROM contradictions")
    stats["Contradiction types seen"] = c.fetchone()[0]

    # Most-contradicted entities
    c.execute("""
        SELECT entity_a_name, COUNT(*) as cnt
        FROM contradictions WHERE resolved = 0
        GROUP BY entity_a_name
        ORDER BY cnt DESC
        LIMIT 3
    """)
    top = c.fetchall()
    if top:
        stats["Most flagged entity"] = f"{top[0][0]} ({top[0][1]})"

    conn.close()

    table = Table(title="Consistency Check Statistics")
    table.add_column("Metric", style="bold")
    table.add_column("Value", justify="right", style="cyan")
    for k, v in stats.items():
        table.add_row(k, str(v))
    console.print(table)


def show_flagged(critical_only=False, limit=50):
    """Display unresolved contradictions."""
    conn = get_consistency_connection()
    c = conn.cursor()

    query = "SELECT entity_a_name, entity_b_name, contradiction_type, severity, entity_a_claim, entity_b_claim, suggested_resolution FROM contradictions WHERE resolved = 0"
    if critical_only:
        query += " AND severity = 'critical'"
    query += " ORDER BY CASE severity WHEN 'critical' THEN 0 WHEN 'moderate' THEN 1 ELSE 2 END, created_at DESC LIMIT ?"

    c.execute(query, (limit,))
    rows = c.fetchall()
    conn.close()

    if not rows:
        console.print("[green]No unresolved contradictions found.[/green]")
        return

    title = f"{'Critical ' if critical_only else ''}Unresolved Contradictions (top {limit})"
    table = Table(title=title, show_lines=True)
    table.add_column("Entity A", style="bold", max_width=25)
    table.add_column("Entity B", style="bold", max_width=25)
    table.add_column("Type", style="cyan", max_width=12)
    table.add_column("Sev", max_width=8)
    table.add_column("Claim A", max_width=40)
    table.add_column("Claim B", max_width=40)
    table.add_column("Fix", style="dim", max_width=40)

    for name_a, name_b, ctype, severity, claim_a, claim_b, resolution in rows:
        sev_style = "red" if severity == "critical" else "yellow" if severity == "moderate" else "dim"
        table.add_row(
            str(name_a)[:25],
            str(name_b)[:25],
            str(ctype)[:12],
            f"[{sev_style}]{severity}[/{sev_style}]",
            str(claim_a)[:120],
            str(claim_b)[:120],
            str(resolution or "")[:80],
        )

    console.print(table)


# ── CLI ───────────────────────────────────────────────────────────────────────
def main():
    parser = argparse.ArgumentParser(description="Cross-entity consistency checker for GLMZ worldbuilding")

    # What to check
    parser.add_argument("--strategy", type=str, choices=["local", "named", "semantic"],
                        help="Checking strategy (default: all three + world rules)")
    parser.add_argument("--repo", type=str, help="Only check entities from this repo")
    parser.add_argument("--limit", type=int, help="Limit number of entities to process")
    parser.add_argument("--concurrency", type=int, help="Parallel API calls (default: 10)")

    # View modes
    parser.add_argument("--flagged", action="store_true", help="Show all unresolved contradictions")
    parser.add_argument("--critical", action="store_true", help="Show only critical contradictions")
    parser.add_argument("--stats", action="store_true", help="Show statistics dashboard only")

    args = parser.parse_args()

    if args.stats:
        init_consistency_db()
        show_consistency_stats()
        return

    if args.flagged:
        init_consistency_db()
        show_flagged(critical_only=False)
        return

    if args.critical:
        init_consistency_db()
        show_flagged(critical_only=True)
        return

    run_consistency_check(
        strategy=args.strategy,
        repo=args.repo,
        limit=args.limit,
        concurrency=args.concurrency,
    )


if __name__ == "__main__":
    main()
