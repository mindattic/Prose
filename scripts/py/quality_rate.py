"""
Quality Rating Pipeline -- Chorus of LLMs

Rates every worldbuilding entity on a 0.0-1.0 interest score across four
dimensions: specificity, connectivity, originality, and story_potential.

HOW IT WORKS:
  1. Load a random sample of 20 other entities as comparison context
  2. Send target entity + context to Claude Haiku for rating
  3. Store all four dimension scores + weighted interest_score in quality.db
  4. Support multiple rounds -- final score is the mean across rounds to reduce noise

WEIGHTED SCORING:
  interest_score = specificity×0.3 + connectivity×0.2 + originality×0.3 + story_potential×0.2

USAGE:
  python quality_rate.py                    # rate all entities, 3 rounds
  python quality_rate.py --rounds 5         # 5 rounds for higher confidence
  python quality_rate.py --repo people      # only rate one repo
  python quality_rate.py --limit 100        # test with 100 files
  python quality_rate.py --skip-existing    # only rate entities not yet in DB
  python quality_rate.py --phase query      # show stats dashboard only
  python quality_rate.py --bottom 20        # show 20 lowest-scored entities
  python quality_rate.py --top 20           # show 20 highest-scored entities
  python quality_rate.py --export           # write interest_score back to source JSON files
"""

import json
import os
import re
import asyncio
import sqlite3
import glob
import random
import argparse
from pathlib import Path
from datetime import datetime

from rich.console import Console
from rich.table import Table
from rich.progress import Progress

from constants import ANTHROPIC_API_KEY, DATA_DIR, CONCURRENCY, REPOS

# ── Config ────────────────────────────────────────────────────────────────────
QUALITY_DB_PATH = os.getenv("QUALITY_DB_PATH", "quality.db")
QUALITY_MODEL = "claude-haiku-4-5-20251001"
CONTEXT_SAMPLE_SIZE = 20
CHECKPOINT_SIZE = 50

console = Console()

# ── System prompt ─────────────────────────────────────────────────────────────
RATING_SYSTEM_PROMPT = """You are a worldbuilding quality evaluator for a near-future cyberpunk world called GLMZ (year 2200). Rate the entity on four 0.0-1.0 dimensions. Be critical — most entities should score 0.3-0.7. Only exceptional entities score above 0.85. Generic or vague entries score below 0.3.

Dimensions:
- specificity: Is it specific and grounded, or vague and generic? (0=generic filler, 1=vivid and particular)
- connectivity: Does it reference or relate to other entities in the world? (0=isolated, 1=richly connected)
- originality: Is it surprising or does it feel like default worldbuilding? (0=cliché, 1=unexpected and fresh)
- story_potential: How much story could grow from this entity? (0=dead end, 1=many compelling threads)

Return ONLY a JSON object with exactly these keys: specificity, connectivity, originality, story_potential, specificity_rationale, connectivity_rationale, originality_rationale, story_potential_rationale

Each score is a float between 0.0 and 1.0. Each rationale is a single sentence.

Example:
{
  "specificity": 0.72,
  "connectivity": 0.45,
  "originality": 0.81,
  "story_potential": 0.63,
  "specificity_rationale": "Names a specific district, weapon model, and cultural ritual rather than generic descriptors.",
  "connectivity_rationale": "References one corponation but has no other named world entities.",
  "originality_rationale": "The concept of debt inherited through genetic modification is unexpected and world-specific.",
  "story_potential_rationale": "The inherited debt angle opens several conflict threads but backstory limits forward motion."
}"""


# ── Database ──────────────────────────────────────────────────────────────────
def init_quality_db():
    """Create quality.db tables if they don't exist."""
    conn = sqlite3.connect(QUALITY_DB_PATH)
    c = conn.cursor()

    c.execute("""
        CREATE TABLE IF NOT EXISTS quality_scores (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            entity_id TEXT NOT NULL,
            entity_name TEXT NOT NULL,
            source_file TEXT NOT NULL,
            source_repo TEXT NOT NULL,
            specificity REAL NOT NULL,
            connectivity REAL NOT NULL,
            originality REAL NOT NULL,
            story_potential REAL NOT NULL,
            interest_score REAL NOT NULL,
            model_used TEXT NOT NULL,
            round INTEGER NOT NULL DEFAULT 1,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        )
    """)

    c.execute("CREATE INDEX IF NOT EXISTS idx_quality_entity ON quality_scores(entity_id)")
    c.execute("CREATE INDEX IF NOT EXISTS idx_quality_score ON quality_scores(interest_score)")
    c.execute("CREATE INDEX IF NOT EXISTS idx_quality_repo ON quality_scores(source_repo)")

    conn.commit()
    conn.close()


def get_quality_connection():
    return sqlite3.connect(QUALITY_DB_PATH)


# ── File helpers ──────────────────────────────────────────────────────────────
def get_json_files(repo=None):
    """Get all JSON entity files, optionally filtered by repo."""
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

    return sorted(files)


def load_entity(filepath):
    """Load a JSON entity file, returning (entity_dict, entity_id, entity_name, source_repo)."""
    try:
        with open(filepath, "r", encoding="utf-8") as f:
            entity = json.load(f)

        if not isinstance(entity, dict):
            return None, None, None, None

        source_repo = Path(filepath).parent.name
        entity_id = entity.get("id", os.path.basename(filepath).replace(".json", ""))
        entity_name = entity.get("name", entity.get("title", entity.get("quote", entity_id)))
        return entity, entity_id, entity_name, source_repo

    except Exception:
        return None, None, None, None


def build_compact_summary(entity, max_chars=300):
    """Build a compact one-paragraph summary of an entity for context."""
    name = entity.get("name", entity.get("title", entity.get("quote", "Unknown")))
    repo = Path(entity.get("source_file", "")).parent.name if "source_file" in entity else ""
    desc = entity.get("description", entity.get("body", entity.get("summary", "")))

    if isinstance(desc, list):
        desc = " ".join(str(d) for d in desc)

    desc = str(desc)[:200] if desc else "(no description)"
    return f"{name}: {desc}"[:max_chars]


# ── Already-processed check ───────────────────────────────────────────────────
def get_already_rated(target_round):
    """Return set of (entity_id, round) already in quality.db."""
    conn = get_quality_connection()
    c = conn.cursor()
    c.execute("SELECT entity_id, round FROM quality_scores")
    result = {(row[0], row[1]) for row in c.fetchall()}
    conn.close()
    return result


# ── Context sampler ───────────────────────────────────────────────────────────
def build_context_block(all_files, current_file, sample_size=CONTEXT_SAMPLE_SIZE):
    """Sample random entities from nearby and adjacent repos for comparison context."""
    current_repo = Path(current_file).parent.name

    # Prefer same-repo files, then pull from others
    same_repo = [f for f in all_files if Path(f).parent.name == current_repo and f != current_file]
    other_repo = [f for f in all_files if Path(f).parent.name != current_repo]

    sample_pool = same_repo + other_repo
    sample = random.sample(sample_pool, min(sample_size, len(sample_pool)))

    summaries = []
    for fp in sample:
        entity, _, _, _ = load_entity(fp)
        if entity:
            summaries.append(build_compact_summary(entity))

    return "\n".join(f"- {s}" for s in summaries)


# ── JSON response parser ──────────────────────────────────────────────────────
def parse_rating_json(text):
    """Parse Claude's rating response. Returns dict or None on failure."""
    # Strip markdown fences
    if "```" in text:
        match = re.search(r'\{[\s\S]*\}', text)
        if match:
            text = match.group(0)
        else:
            text = text.replace("```json", "").replace("```", "").strip()

    if not text.strip().startswith("{"):
        match = re.search(r'\{[\s\S]*\}', text)
        if match:
            text = match.group(0)

    try:
        data = json.loads(text)
        required = {"specificity", "connectivity", "originality", "story_potential"}
        if required.issubset(data.keys()):
            return data
    except json.JSONDecodeError:
        pass

    return None


# ── Compute weighted interest score ──────────────────────────────────────────
def compute_interest_score(specificity, connectivity, originality, story_potential):
    """Weighted average: specificity×0.3 + connectivity×0.2 + originality×0.3 + story_potential×0.2"""
    return (
        specificity * 0.3
        + connectivity * 0.2
        + originality * 0.3
        + story_potential * 0.2
    )


# ── Async API call ────────────────────────────────────────────────────────────
async def rate_entity_via_api(entity, context_block, client, semaphore):
    """Call Claude Haiku to rate a single entity. Returns parsed rating dict or None."""
    async with semaphore:
        # Trim large descriptions to keep prompts lean
        trimmed = dict(entity)
        desc = trimmed.get("description", trimmed.get("body", ""))
        if isinstance(desc, str) and len(desc) > 2000:
            trimmed["description"] = desc[:2000] + "..."

        # Strip large blob fields that add noise
        for drop in ("id", "tags", "embedding", "image_url"):
            trimmed.pop(drop, None)

        user_content = (
            f"{json.dumps(trimmed, indent=2, ensure_ascii=False)}\n\n"
            f"Context (other entities from this world for comparison):\n{context_block}"
        )

        max_retries = 3
        for attempt in range(max_retries):
            try:
                response = await client.messages.create(
                    model=QUALITY_MODEL,
                    max_tokens=512,
                    system=RATING_SYSTEM_PROMPT,
                    messages=[{"role": "user", "content": user_content}],
                )
                text = response.content[0].text.strip()
                return parse_rating_json(text)

            except Exception as e:
                err = str(e).lower()
                if "overloaded" in err or "rate" in err:
                    wait = (attempt + 1) * 10
                    console.print(f"  [yellow]Rate limited, waiting {wait}s...[/yellow]")
                    await asyncio.sleep(wait)
                    continue
                console.print(f"  [red]API error during rating: {e}[/red]")
                return None

    return None


# ── Store a single rating row ─────────────────────────────────────────────────
def store_rating(conn, entity_id, entity_name, source_file, source_repo, rating, round_num):
    """Insert one quality_scores row."""
    s = float(max(0.0, min(1.0, rating.get("specificity", 0.5))))
    c_val = float(max(0.0, min(1.0, rating.get("connectivity", 0.5))))
    o = float(max(0.0, min(1.0, rating.get("originality", 0.5))))
    sp = float(max(0.0, min(1.0, rating.get("story_potential", 0.5))))
    interest = compute_interest_score(s, c_val, o, sp)

    conn.cursor().execute(
        """INSERT INTO quality_scores
           (entity_id, entity_name, source_file, source_repo,
            specificity, connectivity, originality, story_potential,
            interest_score, model_used, round)
           VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)""",
        (entity_id, entity_name, source_file, source_repo,
         s, c_val, o, sp, interest, QUALITY_MODEL, round_num),
    )
    conn.commit()


# ── Per-file async worker ─────────────────────────────────────────────────────
async def process_file(filepath, all_files, client, semaphore, round_num):
    """Load entity, build context, call API, return (filepath, entity_id, entity_name, source_repo, rating)."""
    entity, entity_id, entity_name, source_repo = load_entity(filepath)
    if entity is None:
        return filepath, None, None, None, None

    context_block = build_context_block(all_files, filepath)
    rating = await rate_entity_via_api(entity, context_block, client, semaphore)
    return filepath, entity_id, entity_name, source_repo, rating


# ── Main rating loop ──────────────────────────────────────────────────────────
def run_rating(rounds=3, repo=None, limit=None, skip_existing=False, concurrency=None):
    """Entry point for the rating pipeline."""
    asyncio.run(_run_rating_async(rounds, repo, limit, skip_existing, concurrency))


async def _run_rating_async(rounds=3, repo=None, limit=None, skip_existing=False, concurrency=None):
    import anthropic

    init_quality_db()

    all_files = get_json_files(repo=None)   # always load all for context sampling
    target_files = get_json_files(repo=repo)
    if limit:
        target_files = target_files[:limit]

    actual_concurrency = concurrency or min(CONCURRENCY, 10)

    console.print("[bold red]Street Samurai -- Quality Rating Pipeline[/bold red]")
    console.print(f"  Target files : {len(target_files)}")
    console.print(f"  Rounds       : {rounds}")
    console.print(f"  Repo filter  : {repo or 'all'}")
    console.print(f"  Model        : {QUALITY_MODEL}")
    console.print(f"  Concurrency  : {actual_concurrency}")
    console.print()

    client = anthropic.AsyncAnthropic(api_key=ANTHROPIC_API_KEY)
    semaphore = asyncio.Semaphore(actual_concurrency)

    total_rated = 0

    for round_num in range(1, rounds + 1):
        console.rule(f"[bold]Round {round_num} of {rounds}[/bold]")

        # Resume support: skip (entity_id, round) combos already in DB
        already_rated = get_already_rated(round_num)

        # Build list of (filepath, entity_id) candidates needing work this round
        candidates = []
        for fp in target_files:
            _, entity_id, _, _ = load_entity(fp)
            if entity_id is None:
                continue
            if skip_existing and any(eid == entity_id for eid, _ in already_rated):
                continue
            if (entity_id, round_num) in already_rated:
                continue
            candidates.append(fp)

        console.print(f"  Files to rate this round: {len(candidates)}")
        if not candidates:
            console.print("  [yellow]All files already rated for this round.[/yellow]")
            continue

        conn = get_quality_connection()

        with Progress() as progress:
            task = progress.add_task(f"Round {round_num}...", total=len(candidates))
            files_done = 0

            for i in range(0, len(candidates), CHECKPOINT_SIZE):
                batch = candidates[i: i + CHECKPOINT_SIZE]
                coros = [process_file(fp, all_files, client, semaphore, round_num) for fp in batch]

                for coro in asyncio.as_completed(coros):
                    filepath, entity_id, entity_name, source_repo, rating = await coro

                    if rating and entity_id:
                        store_rating(conn, entity_id, entity_name, filepath, source_repo, rating, round_num)
                        total_rated += 1

                    progress.update(task, advance=1)

                conn.commit()
                files_done += len(batch)

                if files_done % 100 < CHECKPOINT_SIZE:
                    ts = datetime.now().strftime("%Y-%m-%d %I:%M:%S%p")
                    print(f"{ts}    Round {round_num}: {files_done}/{len(candidates)} rated...")

        conn.close()
        console.print(f"  [green]Round {round_num} complete.[/green]")

    console.print(f"\n[bold green]Rating complete! Total ratings stored: {total_rated}[/bold green]")
    show_quality_stats()


# ── Export mode ───────────────────────────────────────────────────────────────
def run_export(repo=None):
    """Write mean interest_score back to source JSON files as top-level field."""
    conn = get_quality_connection()
    c = conn.cursor()

    query = """
        SELECT entity_id, source_file, AVG(interest_score) as mean_score
        FROM quality_scores
    """
    params = []
    if repo:
        query += " WHERE source_repo = ?"
        params.append(repo)
    query += " GROUP BY entity_id, source_file"

    c.execute(query, params)
    rows = c.fetchall()
    conn.close()

    if not rows:
        console.print("[yellow]No quality scores found. Run rating first.[/yellow]")
        return

    updated = 0
    missing = 0

    console.print(f"[bold]Exporting {len(rows)} scores to source JSON files...[/bold]")

    for entity_id, source_file, mean_score in rows:
        if not os.path.exists(source_file):
            missing += 1
            continue
        try:
            with open(source_file, "r", encoding="utf-8") as f:
                data = json.load(f)

            if not isinstance(data, dict):
                continue

            data["interest_score"] = round(mean_score, 4)

            with open(source_file, "w", encoding="utf-8") as f:
                json.dump(data, f, indent=2, ensure_ascii=False)

            updated += 1

        except Exception as e:
            console.print(f"  [red]Error updating {source_file}: {e}[/red]")

    console.print(f"[green]Export complete: {updated} files updated, {missing} files not found.[/green]")


# ── Stats dashboard ───────────────────────────────────────────────────────────
def show_quality_stats():
    """Show quality.db statistics dashboard."""
    conn = get_quality_connection()
    c = conn.cursor()

    stats = {}

    c.execute("SELECT COUNT(DISTINCT entity_id) FROM quality_scores")
    stats["Entities rated"] = c.fetchone()[0]

    c.execute("SELECT COUNT(*) FROM quality_scores")
    stats["Total rating rows"] = c.fetchone()[0]

    c.execute("SELECT MAX(round) FROM quality_scores")
    max_round = c.fetchone()[0]
    stats["Rounds completed"] = max_round or 0

    c.execute("SELECT AVG(interest_score) FROM quality_scores")
    avg = c.fetchone()[0]
    stats["Mean interest score"] = f"{avg:.3f}" if avg else "N/A"

    c.execute("SELECT COUNT(DISTINCT entity_id) FROM quality_scores WHERE interest_score >= 0.8")
    stats["Entities scoring ≥0.8"] = c.fetchone()[0]

    c.execute("SELECT COUNT(DISTINCT entity_id) FROM quality_scores WHERE interest_score < 0.3")
    stats["Entities scoring <0.3"] = c.fetchone()[0]

    c.execute("SELECT COUNT(DISTINCT source_repo) FROM quality_scores")
    stats["Repos covered"] = c.fetchone()[0]

    conn.close()

    table = Table(title="Quality Rating Statistics")
    table.add_column("Metric", style="bold")
    table.add_column("Value", justify="right", style="cyan")
    for k, v in stats.items():
        table.add_row(k, str(v))
    console.print(table)


def show_top_bottom(n=20, mode="top"):
    """Show top or bottom N entities by mean interest score."""
    conn = get_quality_connection()
    c = conn.cursor()

    order = "DESC" if mode == "top" else "ASC"
    c.execute(f"""
        SELECT entity_name, source_repo,
               AVG(specificity), AVG(connectivity), AVG(originality),
               AVG(story_potential), AVG(interest_score),
               COUNT(*) as rounds
        FROM quality_scores
        GROUP BY entity_id
        ORDER BY AVG(interest_score) {order}
        LIMIT ?
    """, (n,))
    rows = c.fetchall()
    conn.close()

    if not rows:
        console.print("[yellow]No scores found.[/yellow]")
        return

    title = f"{'Top' if mode == 'top' else 'Bottom'} {n} Entities by Interest Score"
    table = Table(title=title)
    table.add_column("Entity", style="bold", max_width=35)
    table.add_column("Repo", style="dim", max_width=18)
    table.add_column("Spec", justify="right")
    table.add_column("Conn", justify="right")
    table.add_column("Orig", justify="right")
    table.add_column("Story", justify="right")
    table.add_column("Interest", justify="right", style="cyan")
    table.add_column("Rnds", justify="right")

    for name, repo, spec, conn_val, orig, story, interest, rounds in rows:
        score_style = "green" if interest >= 0.7 else "yellow" if interest >= 0.4 else "red"
        table.add_row(
            str(name)[:35],
            str(repo)[:18],
            f"{spec:.2f}",
            f"{conn_val:.2f}",
            f"{orig:.2f}",
            f"{story:.2f}",
            f"[{score_style}]{interest:.3f}[/{score_style}]",
            str(rounds),
        )

    console.print(table)


# ── CLI ───────────────────────────────────────────────────────────────────────
def main():
    parser = argparse.ArgumentParser(description="Rate worldbuilding entity quality (0.0-1.0)")

    # What to rate
    parser.add_argument("--repo", type=str, help="Only rate entities from this repo")
    parser.add_argument("--limit", type=int, help="Limit number of entities to rate")
    parser.add_argument("--rounds", type=int, default=3, help="Number of rating rounds (default: 3)")
    parser.add_argument("--skip-existing", action="store_true", help="Skip entities already in DB (any round)")
    parser.add_argument("--concurrency", type=int, help="Parallel API calls (default: 10)")

    # View modes
    parser.add_argument("--phase", type=str, choices=["query"], help="Run a single phase (query=stats only)")
    parser.add_argument("--top", type=int, metavar="N", help="Show top N entities by score")
    parser.add_argument("--bottom", type=int, metavar="N", help="Show bottom N entities by score")

    # Export
    parser.add_argument("--export", action="store_true", help="Write mean interest_score back to source JSON files")

    args = parser.parse_args()

    # View-only modes
    if args.phase == "query":
        init_quality_db()
        show_quality_stats()
        return

    if args.top:
        init_quality_db()
        show_top_bottom(args.top, mode="top")
        return

    if args.bottom:
        init_quality_db()
        show_top_bottom(args.bottom, mode="bottom")
        return

    if args.export:
        init_quality_db()
        run_export(repo=args.repo)
        return

    # Full rating run
    run_rating(
        rounds=args.rounds,
        repo=args.repo,
        limit=args.limit,
        skip_existing=args.skip_existing,
        concurrency=args.concurrency,
    )


if __name__ == "__main__":
    main()
