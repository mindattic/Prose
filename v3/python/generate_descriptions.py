"""
Entity Description Generator — Physical Descriptions + Image Prompts

Generates visual descriptions and Midjourney-ready image prompts for entities
across multiple repos (characters, weaponry, equipment, apparel, etc.).

Resume-safe: skips entities that already have a physical_description field.
Async concurrent: uses the same CONCURRENCY setting as extract.py.

Usage:
  python describe.py                         # Process all characters (default)
  python describe.py --repo weaponry         # Process all weapons
  python describe.py --repo apparel          # Process all apparel
  python describe.py --repo equipment        # Process all equipment
  python describe.py --repo all              # Process characters + weaponry + equipment + apparel
  python describe.py --limit 10              # Test with 10 entities
  python describe.py --dry-run               # Preview without calling API
"""

import json
import os
import re
import asyncio
import glob
from pathlib import Path
from rich.console import Console
from rich.progress import Progress
from constants import ANTHROPIC_API_KEY, DATA_DIR, CONCURRENCY, MODEL

console = Console()

# ── System prompts per entity type ──────────────────────────────────────────

WORLD_CONTEXT = """WORLD RULES:
- Set in the Great Lakes Metropolitan Zone (GLMZ), 2226
- The Ubiquitous Diaspora: mixed global heritage is the norm
- Augmentations range from invisible subcutaneous to chrome cyberlimbs
- Tiers: 1-2 = wealthy corpo, 3-4 = working class, 5 = sub-grade/ungoverned
- The symbol Φ is the QUANTA currency symbol
- Iowan Behemoths are autonomous machines, NOT synthetic life

CRITICAL: Return ONLY a raw JSON object with two keys: "physical_description" (object) and "image_prompt" (string).
No markdown, no explanation, no code fences."""

PROMPTS = {
    "people": """You are a character description engine for a cyberpunk worldbuilding project.
Given a character's existing JSON data, generate a physical description and image prompt.

""" + WORLD_CONTEXT + """

PHYSICAL DESCRIPTION SCHEMA:
{
  "heritage": "Ethnic/cultural lineage derived from genetic_ancestry percentages (if provided)",
  "height_cm": integer,
  "weight_kg": integer,
  "build": "Body type with character-specific detail",
  "hair_color": "Natural or modified",
  "hair_style": "How they wear their hair",
  "hair_length": "Short / Medium / Long / Shaved / None",
  "eye_color": "Natural or augmented",
  "skin_tone": "Complexion grounded in mixed heritage",
  "complexion": "Facial features, grooming, skin condition",
  "distinguishing_marks": ["Scars, tattoos, burns — each a sentence"],
  "visible_augmentations": "What a stranger would notice",
  "posture_movement": "How they carry themselves",
  "clothing_style": "Default appearance, tier indicators"
}

IMAGE PROMPT: Midjourney-style. Include genre (cyberpunk 2200), physical descriptors, clothing, setting/mood, lighting. End with --ar 2:3 --v 6""",

    "weaponry": """You are a weapons catalog photographer for a cyberpunk worldbuilding project.
Given a weapon's existing JSON data, generate a detailed visual description and image prompt.

""" + WORLD_CONTEXT + """

PHYSICAL DESCRIPTION SCHEMA:
{
  "visual_profile": "What it looks like at first glance — shape, silhouette, impression",
  "dimensions": "Length, width, height in cm. Barrel length if applicable",
  "weight_description": "How heavy it feels in hand — light, hefty, balanced, unwieldy",
  "primary_material": "What the body/frame is made of — polymer, steel, carbon composite, etc.",
  "finish": "Surface treatment — matte black, brushed steel, worn chrome, battle-scarred polymer, etc.",
  "color_scheme": "Primary and accent colors",
  "grip_texture": "What the handle/grip feels like — rubberized, textured polymer, wrapped cord, bare metal",
  "distinctive_features": ["Array of unique visual details — LED indicators, heat vents, unusual barrel shape, custom engravings, wear patterns"],
  "manufacturer_markings": "Logos, serial plates, stamps — where and how they appear",
  "condition_typical": "How this weapon usually looks in the field — pristine, well-maintained, battle-worn, jury-rigged"
}

IMAGE PROMPT: Midjourney-style product photo. Weapon on dark surface or held in gloved hand. Include materials, lighting (dramatic side-light), cyberpunk aesthetic. End with --ar 3:2 --v 6""",

    "equipment": """You are an equipment catalog photographer for a cyberpunk worldbuilding project.
Given a piece of equipment/technology, generate a detailed visual description and image prompt.

""" + WORLD_CONTEXT + """

PHYSICAL DESCRIPTION SCHEMA:
{
  "visual_profile": "What it looks like — shape, form factor, first impression",
  "dimensions": "Size in cm or general size category (palm-sized, backpack-sized, vehicle-mounted)",
  "weight_description": "How heavy — pocketable, handheld, requires carrying strap, mounted",
  "primary_material": "Housing/body material",
  "finish": "Surface treatment and texture",
  "color_scheme": "Primary and accent colors",
  "interface_elements": "Screens, buttons, ports, indicators — what the user interacts with",
  "distinctive_features": ["Unique visual details — LED patterns, holographic displays, unusual form factor"],
  "manufacturer_markings": "Logos, labels, regulatory stamps",
  "condition_typical": "How it usually looks in use"
}

IMAGE PROMPT: Midjourney-style product shot. Equipment on workbench or in use. Cyberpunk aesthetic, dramatic lighting. End with --ar 3:2 --v 6""",

    "apparel": """You are a fashion catalog photographer for a cyberpunk worldbuilding project.
Given a piece of clothing/apparel, generate a detailed visual description and image prompt.

""" + WORLD_CONTEXT + """

PHYSICAL DESCRIPTION SCHEMA:
{
  "visual_profile": "What it looks like on a person — silhouette, impression, style",
  "fit": "How it fits the body — slim, relaxed, oversized, tailored, compression",
  "primary_material": "Main fabric/material — synth-wool, ballistic weave, treated leather, recycled composite",
  "texture": "How it feels and looks up close — smooth, rough, quilted, ribbed, distressed",
  "color_scheme": "Primary colors and any patterns or accent colors",
  "closures": "How it fastens — zippers, magnetic snaps, friction-fit, buckles, none",
  "distinctive_features": ["Unique design elements — reinforced panels, hidden pockets, integrated tech, brand logos, wear patterns"],
  "tier_indicator": "What social tier this garment signals — Tier 1 luxury, Tier 3 functional, Tier 5 salvage",
  "cultural_context": "What wearing this says about the person — corpo, street, military, medical, sub-grade",
  "condition_typical": "How it usually looks when worn — pristine, broken-in, patched, weathered"
}

IMAGE PROMPT: Midjourney-style fashion photo. Garment on a cyberpunk model or displayed on dark mannequin. Moody lighting, urban backdrop. End with --ar 2:3 --v 6"""
}

# Which fields to send to the LLM per entity type
CONTEXT_FIELDS = {
    "people": ["name", "gender", "species", "age", "role", "affiliation", "location", "description", "augmentations", "daily_life", "genetic_ancestry"],
    "weaponry": ["name", "category", "manufacturer", "description", "tier_availability", "legality", "base_technologies", "specifications", "tactical_use"],
    "equipment": ["name", "category", "manufacturer", "description", "tier_availability", "specifications"],
    "apparel": ["name", "category", "manufacturer", "description", "tier_availability", "specifications", "cultural_context"],
}


async def generate_description(entity, filepath, entity_type, client, semaphore):
    """Send entity data to Claude API for physical description generation."""
    async with semaphore:
        fields = CONTEXT_FIELDS.get(entity_type, ["name", "description"])
        context = {}
        for f in fields:
            val = entity.get(f, "")
            if isinstance(val, str) and len(val) > 2000:
                val = val[:2000]
            if val:
                context[f] = val

        user_content = json.dumps(context, indent=2, ensure_ascii=False)
        system_prompt = PROMPTS.get(entity_type, PROMPTS["equipment"])

        max_retries = 3
        for attempt in range(max_retries):
            try:
                response = await client.messages.create(
                    model=MODEL,
                    max_tokens=2048,
                    system=system_prompt,
                    messages=[{"role": "user", "content": user_content}],
                )

                text = response.content[0].text.strip()

                if "```" in text:
                    match = re.search(r'\{[\s\S]*\}', text)
                    if match:
                        text = match.group(0)
                    else:
                        text = text.replace("```json", "").replace("```", "").strip()

                if not text.startswith("{"):
                    match = re.search(r'\{[\s\S]*\}', text)
                    if match:
                        text = match.group(0)

                result = json.loads(text)
                if isinstance(result, dict) and "physical_description" in result:
                    return result
                return None

            except json.JSONDecodeError:
                if attempt < max_retries - 1:
                    continue
                return None

            except Exception as e:
                if "overloaded" in str(e).lower() or "rate" in str(e).lower():
                    wait = (attempt + 1) * 10
                    console.print(f"  [yellow]Rate limited, waiting {wait}s...[/yellow]")
                    await asyncio.sleep(wait)
                    continue
                console.print(f"  [red]API error on {os.path.basename(filepath)}: {e}[/red]")
                return None

        return None


async def process_file(filepath, entity_type, client, semaphore):
    """Read an entity file, generate description, return result."""
    try:
        with open(filepath, "r", encoding="utf-8") as f:
            entity = json.load(f)

        if not isinstance(entity, dict):
            return filepath, None

        result = await generate_description(entity, filepath, entity_type, client, semaphore)
        return filepath, result

    except Exception as e:
        console.print(f"  [red]Error on {filepath}: {e}[/red]")
        return filepath, None


def run_describe(repo="people", limit=None, dry_run=False, concurrency=None):
    """Main entry point."""
    asyncio.run(_run_describe_async(repo, limit, dry_run, concurrency))


async def _run_describe_async(repo="people", limit=None, dry_run=False, concurrency=None):
    import anthropic

    # Determine which repos to process
    if repo == "all":
        repos = ["people", "weaponry", "equipment", "apparel"]
    else:
        repos = [repo]

    actual_concurrency = concurrency or CONCURRENCY
    client = anthropic.AsyncAnthropic(api_key=ANTHROPIC_API_KEY)
    semaphore = asyncio.Semaphore(actual_concurrency)

    for current_repo in repos:
        repo_dir = Path(DATA_DIR) / current_repo
        if not repo_dir.exists():
            console.print(f"[red]Directory not found: {repo_dir}[/red]")
            continue

        files = sorted(glob.glob(str(repo_dir / "*.json")))
        if limit:
            files = files[:limit]

        # Filter out entities that already have physical_description
        remaining = []
        for fp in files:
            try:
                with open(fp, "r", encoding="utf-8") as f:
                    data = json.load(f)
                if isinstance(data, dict) and "physical_description" not in data:
                    remaining.append(fp)
            except Exception:
                pass

        console.print(f"\n[bold]{current_repo.upper()}[/bold]")
        console.print(f"  Total: {len(files)}  Already described: {len(files) - len(remaining)}  Remaining: {len(remaining)}")
        console.print(f"  Model: {MODEL}  Concurrency: {actual_concurrency}")

        if dry_run:
            console.print("[yellow]Dry run -- first 5:[/yellow]")
            for fp in remaining[:5]:
                try:
                    with open(fp, "r", encoding="utf-8") as f:
                        data = json.load(f)
                    name = data.get('name', data.get('title', '?'))
                    print(f"  {name}")
                except Exception:
                    pass
            continue

        if not remaining:
            console.print("[yellow]All entities already have physical descriptions.[/yellow]")
            continue

        checkpoint_size = 50
        total_described = 0

        with Progress() as progress:
            task = progress.add_task(f"Describing {current_repo}...", total=len(remaining))

            for i in range(0, len(remaining), checkpoint_size):
                batch_files = remaining[i : i + checkpoint_size]

                coros = [process_file(fp, current_repo, client, semaphore) for fp in batch_files]

                for coro in asyncio.as_completed(coros):
                    filepath, result = await coro

                    if result and "physical_description" in result:
                        with open(filepath, "r", encoding="utf-8") as f:
                            entity = json.load(f)

                        entity["physical_description"] = result["physical_description"]
                        if "image_prompt" in result:
                            entity["image_prompt"] = result["image_prompt"]

                        with open(filepath, "w", encoding="utf-8") as f:
                            json.dump(entity, f, indent=2, ensure_ascii=False)

                        total_described += 1

                    progress.update(task, advance=1)

                files_done = i + len(batch_files)
                if files_done % 100 < checkpoint_size:
                    from datetime import datetime
                    ts = datetime.now().strftime("%Y-%m-%d %I:%M:%S%p")
                    print(f"{ts}    {current_repo}: {files_done}/{len(remaining)} ({total_described} successful)")

        console.print(f"  [green]Done: {total_described} described, {len(remaining) - total_described} skipped/failed[/green]")

    console.print(f"\n[bold green]All repos complete.[/bold green]")


if __name__ == "__main__":
    import argparse

    parser = argparse.ArgumentParser(description="Generate physical descriptions for entities")
    parser.add_argument("--repo", type=str, default="people",
                        help="Repo to process: people, weaponry, equipment, apparel, or 'all'")
    parser.add_argument("--limit", type=int, help="Limit number of entities to process")
    parser.add_argument("--dry-run", action="store_true", help="Preview without calling API")
    parser.add_argument("--concurrency", type=int, help=f"Parallel API calls (default: {CONCURRENCY})")

    args = parser.parse_args()
    run_describe(repo=args.repo, limit=args.limit, dry_run=args.dry_run, concurrency=args.concurrency)
