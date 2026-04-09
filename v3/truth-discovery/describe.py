"""
Character Portrait Generator — Physical Descriptions + Image Prompts

Reads all character JSON files from engine/data/characters/, generates FBI/NCIC-style
physical descriptions and Midjourney-ready image prompts using the Claude API, and
writes them back to the character files.

Resume-safe: skips characters that already have a physical_description field.
Async concurrent: uses the same CONCURRENCY setting as extract.py.

Usage:
  python describe.py                    # Process all characters
  python describe.py --limit 10         # Test with 10 characters
  python describe.py --dry-run          # Preview without calling API
"""

import json
import os
import re
import asyncio
import glob
from pathlib import Path
from dotenv import load_dotenv
from rich.console import Console
from rich.progress import Progress

load_dotenv()

console = Console()

ANTHROPIC_API_KEY = os.getenv("ANTHROPIC_API_KEY", "")
DATA_DIR = os.getenv("DATA_DIR", "../../engine/data")
CONCURRENCY = int(os.getenv("CONCURRENCY", "20"))
MODEL = os.getenv("MODEL", "claude-haiku-4-5-20251001")

SYSTEM_PROMPT = """You are a character description engine for a cyberpunk worldbuilding project set in the Great Lakes Metropolitan Zone (GLMZ) in 2226.

Given a character's existing JSON data, generate two things:

1. A "physical_description" object following the FBI/NCIC physical descriptor standard, adapted for a cyberpunk setting.
2. An "image_prompt" string formatted for Midjourney AI image generation.

WORLD RULES:
- The Ubiquitous Diaspora: most people carry hyphenated surnames from two or more unexpected ethnic lineages. Their physical appearance reflects mixed global heritage — not monoethnic.
- Augmentations may be visible (chrome limbs, LED optics, subdermal armor plates) or invisible (subcutaneous neural arrays, internal implants).
- The GLMZ has tiers: Tier 1-2 = wealthy corpo enclaves, Tier 3-4 = working class, Tier 5 = sub-grade/ungoverned. Clothing and grooming reflect tier.
- The symbol Φ is the QUANTA currency symbol, never the Greek letter phi.
- Iowan Behemoths are autonomous machines, NOT synthetic life.

PHYSICAL DESCRIPTION SCHEMA (return as JSON object):
{
  "heritage": "Ethnic/cultural lineage derived from the hyphenated surname (e.g., 'West African / Scandinavian')",
  "height_cm": integer,
  "weight_kg": integer,
  "build": "Body type description — lean, stocky, athletic, wiry, heavy, etc. with character-specific detail",
  "hair_color": "Natural or modified hair color",
  "hair_style": "How they wear their hair",
  "hair_length": "Short / Medium / Long / Shaved / None",
  "eye_color": "Natural or augmented eye color",
  "skin_tone": "Complexion description — grounded in their mixed heritage",
  "complexion": "Facial features, grooming, skin condition",
  "distinguishing_marks": ["Array of scars, tattoos, burns, birthmarks — each a sentence with location and detail"],
  "visible_augmentations": "What a stranger would notice about their cybernetic/biological modifications. 'None visible' if subcutaneous.",
  "posture_movement": "How they carry themselves, how they move, body language defaults",
  "clothing_style": "Default appearance — what they typically wear, brand/tier indicators"
}

IMAGE PROMPT FORMAT (return as a single string):
Write a Midjourney-style prompt. Include: genre (cyberpunk), physical descriptors, clothing, setting/mood, lighting, technical flags. End with --ar 2:3 --v 6

CRITICAL RULES:
- Return ONLY a raw JSON object with two keys: "physical_description" (object) and "image_prompt" (string).
- No markdown, no explanation, no code fences.
- Make descriptions grounded and specific — not generic cyberpunk clichés.
- Derive heritage from the character's surname using the Ubiquitous Diaspora logic.
- If the character has augmentations listed, reflect them visually.
- If the character is non-human (synthetic, AI), adapt the schema accordingly.
- Heights should be realistic (150-200cm range for humans).
- Weights should match the build description.
- Clothing should match their tier, role, and personality."""


async def generate_description(character, filepath, client, semaphore):
    """Send character data to Claude API for physical description generation."""
    async with semaphore:
        # Send relevant character fields (not the full file — trim noise)
        context = {
            "name": character.get("name", "Unknown"),
            "gender": character.get("gender", ""),
            "species": character.get("species", "human"),
            "age": character.get("age", ""),
            "role": character.get("role", ""),
            "affiliation": character.get("affiliation", ""),
            "location": character.get("location", ""),
            "description": character.get("description", "")[:2000],
            "augmentations": character.get("augmentations", ""),
            "daily_life": character.get("daily_life", ""),
        }

        user_content = json.dumps(context, indent=2, ensure_ascii=False)

        max_retries = 3
        for attempt in range(max_retries):
            try:
                response = await client.messages.create(
                    model=MODEL,
                    max_tokens=2048,
                    system=SYSTEM_PROMPT,
                    messages=[{"role": "user", "content": user_content}],
                )

                text = response.content[0].text.strip()

                # Strip markdown fences if present
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


async def process_file(filepath, client, semaphore):
    """Read a character file, generate description, return result."""
    try:
        with open(filepath, "r", encoding="utf-8") as f:
            character = json.load(f)

        if not isinstance(character, dict):
            return filepath, None

        result = await generate_description(character, filepath, client, semaphore)
        return filepath, result

    except Exception as e:
        console.print(f"  [red]Error on {filepath}: {e}[/red]")
        return filepath, None


def run_describe(limit=None, dry_run=False, concurrency=None):
    """Main entry point."""
    asyncio.run(_run_describe_async(limit, dry_run, concurrency))


async def _run_describe_async(limit=None, dry_run=False, concurrency=None):
    import anthropic

    char_dir = Path(DATA_DIR) / "characters"
    files = sorted(glob.glob(str(char_dir / "*.json")))

    if limit:
        files = files[:limit]

    actual_concurrency = concurrency or CONCURRENCY

    # Filter out characters that already have physical_description
    remaining = []
    for fp in files:
        try:
            with open(fp, "r", encoding="utf-8") as f:
                data = json.load(f)
            if isinstance(data, dict) and "physical_description" not in data:
                remaining.append(fp)
        except Exception:
            pass

    console.print(f"[bold]Character Portrait Generator[/bold]")
    console.print(f"  Total characters: {len(files)}")
    console.print(f"  Already described: {len(files) - len(remaining)}")
    console.print(f"  Remaining: {len(remaining)}")
    console.print(f"  Model: {MODEL}")
    console.print(f"  Concurrency: {actual_concurrency}")
    console.print(f"  Dry run: {dry_run}")

    if dry_run:
        console.print("[yellow]Dry run -- first 5 files that would be processed:[/yellow]")
        for fp in remaining[:5]:
            with open(fp, "r", encoding="utf-8") as f:
                data = json.load(f)
            name = data.get('name', '?').encode('ascii', 'replace').decode()
            print(f"  {name} -- {fp}")
        return

    if not remaining:
        console.print("[yellow]All characters already have physical descriptions.[/yellow]")
        return

    client = anthropic.AsyncAnthropic(api_key=ANTHROPIC_API_KEY)
    semaphore = asyncio.Semaphore(actual_concurrency)

    checkpoint_size = 50
    total_described = 0

    with Progress() as progress:
        task = progress.add_task("Generating descriptions...", total=len(remaining))

        for i in range(0, len(remaining), checkpoint_size):
            batch_files = remaining[i : i + checkpoint_size]

            coros = [process_file(fp, client, semaphore) for fp in batch_files]

            for coro in asyncio.as_completed(coros):
                filepath, result = await coro

                if result and "physical_description" in result:
                    # Read the file, add the fields, write it back
                    with open(filepath, "r", encoding="utf-8") as f:
                        character = json.load(f)

                    character["physical_description"] = result["physical_description"]
                    if "image_prompt" in result:
                        character["image_prompt"] = result["image_prompt"]

                    with open(filepath, "w", encoding="utf-8") as f:
                        json.dump(character, f, indent=2, ensure_ascii=False)

                    total_described += 1

                progress.update(task, advance=1)

            # Progress message
            files_done = i + len(batch_files)
            if files_done % 100 < checkpoint_size:
                from datetime import datetime
                ts = datetime.now().strftime("%Y-%m-%d %I:%M:%S%p")
                print(f"{ts}    {files_done}/{len(remaining)} characters described ({total_described} successful)")

    console.print(f"\n[bold green]Done![/bold green]")
    console.print(f"  Characters described: {total_described}")
    console.print(f"  Skipped/failed: {len(remaining) - total_described}")


if __name__ == "__main__":
    import argparse

    parser = argparse.ArgumentParser(description="Generate physical descriptions for characters")
    parser.add_argument("--limit", type=int, help="Limit number of characters to process")
    parser.add_argument("--dry-run", action="store_true", help="Preview without calling API")
    parser.add_argument("--concurrency", type=int, help=f"Parallel API calls (default: {CONCURRENCY})")

    args = parser.parse_args()
    run_describe(limit=args.limit, dry_run=args.dry_run, concurrency=args.concurrency)
