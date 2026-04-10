"""
Ancestry Harmonizer — Adjusts physical descriptions to match genetic ancestry

For each character with genetic_ancestry and physical_description:
1. Adjusts heritage-derived traits (heritage, skin_tone, complexion, eye_color)
   to accurately reflect the character's genetic ancestry percentages
2. Preserves the character's essence (build, augmentations, scars, clothing, posture)
3. Regenerates the image_prompt from the updated physical_description

Resume-safe: marks characters with ancestry_harmonized flag.
Skips Kyle Ellen Corbin-Vasik (protagonist).

Usage:
  python harmonize.py                    # Process all characters
  python harmonize.py --limit 10         # Test with 10
  python harmonize.py --dry-run          # Preview without changes
  python harmonize.py --force            # Re-process already harmonized
"""

import json
import os
import re
import asyncio
import glob
from pathlib import Path
from rich.console import Console
from rich.progress import Progress
from constants import ANTHROPIC_API_KEY, DATA_DIR, CONCURRENCY, MODEL, SKIP_CHARACTERS, NON_HUMAN_SPECIES

console = Console()

SYSTEM_PROMPT = """You are adjusting a cyberpunk character's physical description to match their actual genetic ancestry.

WORLD: Great Lakes Metropolitan Zone (GLMZ), 2226. The Ubiquitous Diaspora means everyone is mixed heritage from unexpected global combinations. Ancestry is INDEPENDENT of surname.

You will receive:
- The character's name
- Their genetic_ancestry (percentage breakdown by region)
- Their current physical_description

YOUR JOB — adjust heritage-derived traits:
1. "heritage" — rewrite to accurately describe the genetic mix shown in genetic_ancestry
2. "skin_tone" — adjust to reflect the realistic blending of their ancestry groups
3. "complexion" — adjust facial feature descriptions to reflect the ancestry mix. Keep non-heritage details (scars, skin condition, grooming, augmentation marks)
4. "eye_color" — adjust ONLY if currently described as a natural color. If augmented/cybernetic/implanted, keep as-is

PRESERVE EXACTLY (do not change these):
- height_cm, weight_kg, build
- hair_color, hair_style, hair_length (personal choices, not heritage-locked)
- visible_augmentations
- posture_movement
- clothing_style
- distinguishing_marks

Then generate a NEW image_prompt (Midjourney-style) from the UPDATED physical_description.
Include: cyberpunk 2200s, their actual physical traits, clothing, mood/setting, lighting.
End with --ar 2:3 --v 6

CRITICAL: Return ONLY a raw JSON object with two keys: "physical_description" (object) and "image_prompt" (string).
No markdown, no code fences, no explanation."""


async def harmonize_character(data, filepath, client, semaphore):
    """Send character data to Claude API for heritage harmonization."""
    async with semaphore:
        context = {
            "name": data.get("name", ""),
            "genetic_ancestry": data.get("genetic_ancestry", {}),
            "current_physical_description": data.get("physical_description", {}),
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
    """Read a character file, harmonize, return result."""
    try:
        with open(filepath, "r", encoding="utf-8") as f:
            data = json.load(f)

        if not isinstance(data, dict):
            return filepath, None

        result = await harmonize_character(data, filepath, client, semaphore)
        return filepath, result

    except Exception as e:
        console.print(f"  [red]Error on {filepath}: {e}[/red]")
        return filepath, None


async def run_harmonize(limit=None, dry_run=False, force=False, concurrency=None):
    import anthropic

    actual_concurrency = concurrency or CONCURRENCY
    client = anthropic.AsyncAnthropic(api_key=ANTHROPIC_API_KEY)
    semaphore = asyncio.Semaphore(actual_concurrency)

    char_dir = Path(DATA_DIR) / "people"
    files = sorted(glob.glob(str(char_dir / "*.json")))
    if limit:
        files = files[:limit]

    remaining = []
    for fp in files:
        try:
            with open(fp, "r", encoding="utf-8") as f:
                data = json.load(f)
            if not isinstance(data, dict):
                continue

            name = data.get("name", "")
            if name in SKIP_CHARACTERS:
                continue
            species = data.get("species", "human").lower().strip()
            if species in NON_HUMAN_SPECIES:
                continue
            if not data.get("genetic_ancestry"):
                continue
            if not data.get("physical_description"):
                continue
            if data.get("ancestry_harmonized") and not force:
                continue

            remaining.append(fp)
        except Exception:
            pass

    console.print(f"\n[bold]ANCESTRY HARMONIZATION[/bold]")
    console.print(f"  Eligible: {len(remaining)}  Concurrency: {actual_concurrency}  Model: {MODEL}")

    if dry_run:
        console.print("[yellow]Dry run -- first 5:[/yellow]")
        for fp in remaining[:5]:
            try:
                with open(fp, "r", encoding="utf-8") as f:
                    data = json.load(f)
                name = data.get("name", "?")
                ancestry = data.get("genetic_ancestry", {})
                top = sorted(ancestry.items(), key=lambda x: -x[1])[:3]
                top_str = ", ".join(f"{g} {p}%" for g, p in top)
                print(f"  {name}: {top_str}")
            except Exception:
                pass
        return

    if not remaining:
        console.print("[yellow]All characters already harmonized.[/yellow]")
        return

    checkpoint_size = 50
    total_done = 0

    with Progress() as progress:
        task = progress.add_task("Harmonizing...", total=len(remaining))

        for i in range(0, len(remaining), checkpoint_size):
            batch = remaining[i : i + checkpoint_size]
            coros = [process_file(fp, client, semaphore) for fp in batch]

            for coro in asyncio.as_completed(coros):
                filepath, result = await coro

                if result and "physical_description" in result:
                    with open(filepath, "r", encoding="utf-8") as f:
                        entity = json.load(f)

                    entity["physical_description"] = result["physical_description"]
                    if "image_prompt" in result:
                        entity["image_prompt"] = result["image_prompt"]
                    entity["ancestry_harmonized"] = True

                    with open(filepath, "w", encoding="utf-8") as f:
                        json.dump(entity, f, indent=2, ensure_ascii=False)

                    total_done += 1

                progress.update(task, advance=1)

            files_done = i + len(batch)
            if files_done % 100 < checkpoint_size:
                from datetime import datetime
                ts = datetime.now().strftime("%Y-%m-%d %I:%M:%S%p")
                print(f"{ts}    harmonize: {files_done}/{len(remaining)} ({total_done} successful)")

    console.print(f"  [green]Done: {total_done} harmonized, {len(remaining) - total_done} failed[/green]")


def main():
    import argparse
    parser = argparse.ArgumentParser(description="Harmonize physical descriptions with genetic ancestry")
    parser.add_argument("--limit", type=int, help="Limit number of characters")
    parser.add_argument("--dry-run", action="store_true", help="Preview without calling API")
    parser.add_argument("--force", action="store_true", help="Re-process already harmonized characters")
    parser.add_argument("--concurrency", type=int, help=f"Parallel API calls (default: {CONCURRENCY})")
    args = parser.parse_args()

    asyncio.run(run_harmonize(
        limit=args.limit,
        dry_run=args.dry_run,
        force=args.force,
        concurrency=args.concurrency,
    ))


if __name__ == "__main__":
    main()
