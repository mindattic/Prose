"""
Character Image Generator — DALL-E 3 prompts + images for people/

Two-phase pipeline per character:
  Phase 1 — Prompt: if dalle3_prompt is empty, call Claude to write one
             using Kyle's prompt as the gold-standard few-shot example.
             Saves dalle3_prompt back to the JSON file.
  Phase 2 — Image: if no image file exists ({id}.00.png), call DALL-E 3
             and save the result to engine/data/media/{id}.00.png.
  Phase 3 — CCTV: probabilistically generate a B&W surveillance-cam still
             (higher probability for criminal/low-tier characters).
             Saves cctv_prompt to the JSON file for resume-safety.

Resume-safe: a character is skipped if it already has both a dalle3_prompt
and an existing image file.

Usage:
  python generate_character_images.py                 # all characters
  python generate_character_images.py --limit 5       # first 5 only
  python generate_character_images.py --prompt-only   # write prompts, no images
  python generate_character_images.py --image-only    # generate images for existing prompts
  python generate_character_images.py --dry-run       # preview, no API calls
  python generate_character_images.py --force         # regenerate even if prompt/image exist
  python generate_character_images.py --id <guid>     # single character by id
  python generate_character_images.py --cctv          # also run CCTV pass on existing chars
  python generate_character_images.py --no-cctv       # skip CCTV phase entirely
"""

import argparse
import asyncio
import base64
import glob
import json
import os
import random
import re
import sys
from pathlib import Path

import httpx
from rich.console import Console
from rich.progress import Progress

sys.path.insert(0, str(Path(__file__).parent))
from constants import ANTHROPIC_API_KEY, DATA_DIR

console = Console(legacy_windows=False)

# ── Config ────────────────────────────────────────────────────────────────────
OPENAI_API_KEY = os.getenv("SS_OPENAI_API_KEY") or os.getenv("OPENAI_API_KEY", "")
PEOPLE_DIR = Path(DATA_DIR) / "people"
MEDIA_DIR = Path(DATA_DIR) / "media"
CONCURRENCY = int(os.getenv("CONCURRENCY", "5"))  # conservative — DALL-E rate limits

DALLE_MODEL = "gpt-image-1"
DALLE_SIZE = "1024x1536"   # portrait aspect (gpt-image-1 supported size)
DALLE_QUALITY = "high"

CLAUDE_MODEL = os.getenv("MODEL", "claude-sonnet-4-6")

# ── CCTV probabilities ────────────────────────────────────────────────────────
# Base: ~20% of all characters get a CCTV still
# Criminal-adjacent: ~65% — gang members, enforcers, smugglers, etc.
# Low-tier only: ~40% — independent/street-level without criminal ties
CCTV_BASE_PROB       = 0.20
CCTV_CRIMINAL_PROB   = 0.65
CCTV_LOW_TIER_PROB   = 0.40

CRIMINAL_KEYWORDS_AFF  = {"gang", "cartel", "syndicate", "crew", "raker", "black market",
                           "grey market", "underground", "renegade", "outlaw", "criminal",
                           "smuggler", "trafficker", "gutter", "dock crew"}
CRIMINAL_KEYWORDS_ROLE = {"enforcer", "assassin", "thief", "smuggler", "fixer", "ganger",
                           "mercenary", "runner", "hitman", "muscle", "wetwork", "operative"}
CRIMINAL_KEYWORDS_DESC = {"criminal", "wanted", "fugitive", "illegal", "outlaw", "arrest",
                           "bounty", "warrant", "contraband", "black-market"}

LOW_TIER_KEYWORDS_AFF  = {"independent", "unaffiliated", "street", "ungoverned", "volatile",
                           "no fixed", "freelance"}

CCTV_ACTIVITIES = [
    "walking through a corridor",
    "passing through a security checkpoint",
    "standing near a doorway",
    "moving through a crowded market area",
    "waiting near an entrance",
    "using a public terminal",
    "descending a stairwell",
    "crossing an intersection",
    "loading or unloading a vehicle",
    "looking up at the camera briefly",
]

CCTV_SECTORS = [
    "SECTOR {s1}-{s2} / CAM-{cam:03d}",
    "DIST-{s1} GRID-{s2:02d} / {cam:03d}",
    "ZONE {s1}{s2} / SURV-{cam:03d}",
]


# ── Tier 1 prompt suffix — standardised cyberpunk aesthetic ──────────────────
# Appended to every dalle3_prompt for Tier 1 characters so their images
# share a consistent environment, lighting, mood and camera style.
_TIER1_ACTION_DRY  = "walking forward through narrow neon-lit alley, subtle motion blur, shallow depth of field"
_TIER1_ACTION_RAIN = "walking forward through narrow neon-lit alley, rain falling, droplets streaking through frame, subtle motion blur, shallow depth of field"
_TIER1_RAIN_PROB   = 0.20  # 20% of Tier 1 prompts include active rain

_TIER1_SUFFIX_BODY = """\

environment: dense cyberpunk alley, teal and cyan dominant lighting with magenta accents, glowing signage and advertisements, clean but wet surfaces, reflective pavement, light fog, blurred background figures

lighting: cinematic neon rim lighting, face partially shadowed but readable, sharp highlights, strong color contrast, volumetric light through rain, reflections across ground

mood: corporate dystopia, sleek but hostile, capitalism-saturated environment

style: photorealistic but grounded, NOT idealized, NOT symmetrical, NOT model-like, avoid beauty standards

camera: 50mm lens, eye-level, natural perspective, slight imperfections in skin texture, visible pores

--ar 2:3 --v 6"""

_TIER1_SUFFIX_MARKER = "capitalism-saturated environment"  # unique sentinel for idempotency check


def _is_tier1(data: dict) -> bool:
    return str(data.get("tier", "")).strip() == "1"


def _apply_tier1_suffix(prompt: str) -> str:
    """Append the Tier 1 aesthetic suffix if not already present. Rain 20% of the time."""
    if _TIER1_SUFFIX_MARKER in prompt:
        return prompt
    action = _TIER1_ACTION_RAIN if random.random() < _TIER1_RAIN_PROB else _TIER1_ACTION_DRY
    return prompt.rstrip() + f"\n\n{action}" + _TIER1_SUFFIX_BODY


# ── Kyle's gold-standard data (few-shot anchor) ───────────────────────────────
KYLE_SUMMARY = """\
Name: Kyle Ellen Corbin-Vasik
Gender: male | Age: 27
Role: Protagonist — freelance enforcer, facility survivor, the Street Samurai
Physical:
  Heritage: Eastern European / Pacific Islander
  Build: Lean, ropey muscle — survival-built. Narrow hips, long arms, speed over power.
  Hair: Dark brown, short on sides, longer on top, pushed back, perpetually uneven — cuts it himself.
  Eyes: Grey-green — left iris has a faint gold ring (NeoCortex optic thread bleed).
  Skin: Olive-tan, weathered. Faint acne scarring along jawline. Uneven texture near temples.
  Marks: Keloid surgical scar behind left ear. Micro-scarring on forearms. Pale chemical burn on right palm.
  Augmentations: None visible — NeoCortex is subcutaneous. Faint trace lines at temples if you know to look.
  Posture: Still when standing, efficient when moving. No wasted motion. Eyes track corners before people.
  Clothing: Worn dark leather jacket over ballistic underlayer, cargo pants, resoled boots, carbon-black katana across back."""

KYLE_DALLE3_PROMPT = """\
neo-noir cyberpunk enforcer, male 27, lean muscular build, olive-tan skin, rough and imperfect face, asymmetrical features, slightly crooked nose, uneven jawline, subtle under-eye fatigue, small facial scars on cheek and bridge of nose, faint stubble, tired but dangerous eyes, grey-green with faint gold ring in left iris

hair: short dark brown, uneven, spiked but messy and clumped from rain, not styled, irregular hairline

expression: cold, focused, emotionally restrained, slightly hardened, not heroic, not attractive, grounded realism

outfit: dark leather jacket with tactical padding and attachments, worn but maintained, partially open over fitted dark shirt, utility belt with holster, reinforced gloves, holding pistol low in right hand, carbon-black katana strapped across back

walking forward through narrow neon-lit alley, rain falling, droplets streaking through frame, subtle motion blur, shallow depth of field

environment: dense cyberpunk alley, high-end district, teal and cyan dominant lighting with magenta accents, glowing signage and advertisements, clean but wet surfaces, reflective pavement, light fog, blurred background figures

lighting: cinematic neon rim lighting, face partially shadowed but readable, sharp highlights, strong color contrast, volumetric light through rain, reflections across ground

mood: controlled, dangerous, professional, corporate dystopia, sleek but hostile, capitalism-saturated environment

style: photorealistic but grounded, NOT idealized, NOT symmetrical, NOT model-like, avoid beauty standards

camera: 50mm lens, eye-level, natural perspective, slight imperfections in skin texture, visible pores"""

# ── System prompt for Claude ──────────────────────────────────────────────────
CLAUDE_SYSTEM = f"""\
You write DALL-E 3 image prompts for characters in a near-future cyberpunk worldbuilding project set in the Great Lakes Metropolitan Zone (GLMZ), 2226.

WORLD RULES:
- Mixed global heritage is the norm (Ubiquitous Diaspora) — no character is a single ethnicity
- Tier 1 = low-income laborers and street workers — worn, weathered clothing
  Tier 2 = working class — functional but not prosperous
  Tier 3-4 = professional/middle class — some corporate identity
  Tier 5 = corporate elite — expensive, curated
- Augmentations: only describe what is visibly external. Cybernetic eyes are subtle iris irregularities or unusual color — NOT camera lenses, NOT glowing orbs. Neural implants are faint trace lines at the temple — NOT ports or glowing hardware. Augmentations look like medical hardware on a normal human body, not sci-fi props.
- Characters are always in motion or a candid moment — walking, glancing, pausing. Never posed, never meditating, never staring at camera.
- Background: wet cyberpunk street, alley, or public space. Never a server room or sterile interior.
- Do NOT make characters idealized or model-beautiful. Do NOT make faces symmetrical — real faces are asymmetrical and imperfect.
- Avoid generic cyberpunk clichés: no glowing robotic eyes, no chrome arms, no holographic displays — unless explicitly in the character data.

PROMPT STRUCTURE (follow this format exactly):
1. Opening line: genre/role, gender, age, build, skin tone, face character
2. hair: line
3. expression: line
4. outfit: line (Tier 1 = worn/survival-grade, Tier 5 = expensive/curated)
5. One action/pose sentence
6. environment: line — cyberpunk street or alley
7. lighting: line
8. mood: line
9. style: photorealistic but grounded, NOT idealized, NOT symmetrical, NOT model-like, avoid beauty standards
10. camera: 50mm lens, eye-level, natural perspective, slight imperfections in skin texture, visible pores

GOLD STANDARD EXAMPLE:

INPUT CHARACTER SUMMARY:
{KYLE_SUMMARY}

OUTPUT DALL-E 3 PROMPT:
{KYLE_DALLE3_PROMPT}

---

Now generate a DALL-E 3 prompt for the character provided. Return ONLY the prompt text, no explanation, no code fences, no preamble."""


# ── Helpers ───────────────────────────────────────────────────────────────────

def strip_mj_params(prompt: str) -> str:
    """Remove Midjourney-style parameters (--ar, --v, etc.) that DALL-E rejects."""
    return re.sub(r"\s*--\w[\w-]*(?:\s+\S+)?", "", prompt).strip()


# ── Safety sanitizer — applied on 400 content-policy rejection ───────────────
# Replaces specific weapon holds and explicit violence descriptors that reliably
# trigger DALL-E's safety rewriter, while preserving the visual aesthetic.

_SAFETY_EXACT = [
    # Explicit weapon holds
    ("holding pistol low in right hand",  "right hand relaxed at side"),
    ("holding pistol",                    "hand at side"),
    ("holding a pistol",                  "hand at side"),
    ("gripping a pistol",                 "hand at side"),
    ("aiming pistol",                     "hand raised"),
    ("holding gun",                       "hand at side"),
    ("holding a gun",                     "hand at side"),
    ("holding knife",                     "hand at side"),
    ("holding a blade",                   "hand at side"),
    ("drawn weapon",                      "tactical stance"),
    # Weapon on body — soften but keep visual
    ("carbon-black katana strapped across back", "long case strapped across back"),
    ("katana strapped across back",        "long case strapped across back"),
    ("katana across back",                 "long case across back"),
    ("katana on back",                     "long case on back"),
    ("sword across back",                  "long case across back"),
    # Pistol references in outfit line
    ("utility belt with holster",          "utility belt with pouches"),
    ("with holster",                       "with utility pouches"),
    # Role / mood descriptors that trigger safety rewrite
    ("dangerous eyes",                     "watchful eyes"),
    ("tired but dangerous",                "fatigued but alert"),
    ("cold, dangerous",                    "cold, focused"),
    ("lethal",                             "precise"),
    ("kill",                               "pursue"),
    ("assassin",                           "specialist"),
    ("hitman",                             "contractor"),
]

_SAFETY_PATTERN = re.compile(
    "|".join(re.escape(old) for old, _ in _SAFETY_EXACT),
    flags=re.IGNORECASE,
)

def _safety_replace(m: re.Match) -> str:
    matched = m.group(0)
    for old, new in _SAFETY_EXACT:
        if matched.lower() == old.lower():
            return new
    return matched

def sanitize_for_safety(prompt: str) -> str:
    """Strip weapon-hold and violence descriptors that trigger DALL-E safety filters."""
    return _SAFETY_PATTERN.sub(_safety_replace, prompt).strip()


SAFETY_LOG = Path(__file__).parent / "safety_rejected.json"

def _log_safety_rejection(entity_id: str, name: str, prompt: str) -> None:
    """Append a failed entity to the safety rejection log."""
    log: dict = {}
    if SAFETY_LOG.exists():
        try:
            log = json.loads(SAFETY_LOG.read_text(encoding="utf-8"))
        except Exception:
            pass
    log[entity_id] = {"name": name, "prompt": prompt[:400]}
    SAFETY_LOG.write_text(json.dumps(log, indent=2, ensure_ascii=False), encoding="utf-8")


def next_image_index(entity_id: str) -> int:
    """Return the next available image index (0-based) for this entity in media/."""
    existing = list(MEDIA_DIR.glob(f"{entity_id}.??.png"))
    if not existing:
        return 0
    indices = []
    for p in existing:
        m = re.match(r".*\.(\d+)\.png$", p.name)
        if m:
            indices.append(int(m.group(1)))
    return max(indices) + 1 if indices else 0


def has_image(entity_id: str) -> bool:
    return any(MEDIA_DIR.glob(f"{entity_id}.??.png"))


def build_character_summary(data: dict) -> str:
    """Build a condensed text description of a character for the Claude prompt."""
    lines = []
    lines.append(f"Name: {data.get('name', '?')}")

    gender = data.get("gender", "")
    age = data.get("age", "")
    if gender or age:
        lines.append(f"Gender: {gender} | Age: {age}")

    role = data.get("role", "")
    if role:
        lines.append(f"Role: {role}")

    desc = data.get("description", "")
    if desc:
        lines.append(f"Description: {desc[:400]}{'...' if len(desc) > 400 else ''}")

    aug = data.get("augmentations", "")
    if aug:
        lines.append(f"Augmentations: {aug[:200]}{'...' if len(aug) > 200 else ''}")

    pd = data.get("physical_description", {})
    if pd and isinstance(pd, dict):
        lines.append("Physical:")
        for key in ("heritage", "build", "hair_color", "hair_style", "eye_color",
                    "skin_tone", "complexion", "posture_movement", "clothing_style",
                    "visible_augmentations"):
            val = pd.get(key, "")
            if val:
                lines.append(f"  {key.replace('_', ' ').title()}: {val}")
        marks = pd.get("distinguishing_marks", [])
        if marks:
            lines.append(f"  Marks: {' | '.join(marks[:3])}")

    return "\n".join(lines)


# ── CCTV helpers ──────────────────────────────────────────────────────────────

def _is_criminal_adjacent(data: dict) -> bool:
    aff  = (data.get("affiliation") or "").lower()
    role = (data.get("role") or "").lower()
    desc = (data.get("description") or "")[:600].lower()
    if any(kw in aff for kw in CRIMINAL_KEYWORDS_AFF):   return True
    if any(kw in role for kw in CRIMINAL_KEYWORDS_ROLE):  return True
    if any(kw in desc for kw in CRIMINAL_KEYWORDS_DESC):  return True
    return False


def _is_low_tier(data: dict) -> bool:
    aff  = (data.get("affiliation") or "").lower()
    role = (data.get("role") or "").lower()
    if any(kw in aff for kw in LOW_TIER_KEYWORDS_AFF):  return True
    if "freelance" in role or "street" in role:         return True
    return False


def should_generate_cctv(data: dict) -> bool:
    """Probabilistically decide whether to generate a CCTV still for this character."""
    criminal = _is_criminal_adjacent(data)
    low_tier = _is_low_tier(data)
    if criminal:
        prob = CCTV_CRIMINAL_PROB
    elif low_tier:
        prob = CCTV_LOW_TIER_PROB
    else:
        prob = CCTV_BASE_PROB
    return random.random() < prob


def build_cctv_prompt(data: dict, dalle3_prompt: str) -> str:
    """Build a surveillance-camera DALL-E 3 prompt from character data."""
    gender = (data.get("gender") or "person").lower()
    age    = data.get("age", "")
    pd     = data.get("physical_description", {}) or {}
    build  = pd.get("build", "")
    clothing = pd.get("clothing_style", "")
    hair   = " ".join(filter(None, [pd.get("hair_color", ""), pd.get("hair_style", "")]))

    # Grab the opening description line from dalle3_prompt if available
    brief = ""
    if dalle3_prompt:
        first_line = dalle3_prompt.split("\n")[0].strip()
        brief = first_line[:250]

    activity = random.choice(CCTV_ACTIVITIES)

    # Generate timestamp and location for the overlay
    month  = random.randint(1, 12)
    day    = random.randint(1, 28)
    hour   = random.randint(0, 23)
    minute = random.randint(0, 59)
    second = random.randint(0, 59)
    ts     = f"2226-{month:02d}-{day:02d}  {hour:02d}:{minute:02d}:{second:02d}"

    tpl    = random.choice(CCTV_SECTORS)
    loc    = tpl.format(s1=random.randint(1, 9), s2=random.randint(1, 9),
                        cam=random.randint(1, 99))

    parts = [
        "security surveillance camera still frame, black and white monochrome, grainy CCTV footage",
        brief or f"{gender}, {age or 'adult'}, {build}, {hair}, {clothing}".strip(", "),
        f"{activity}",
        "overhead ceiling-mounted fisheye wide-angle camera, slight lens distortion, high angle shot from corner",
        "heavy film grain, digital compression artifacts, noise, visible scan lines, blown-out highlights, dark vignette",
        f"white text timestamp overlay bottom-left corner: '{ts}'",
        f"white text location label top-right corner: '{loc}'",
        "no color whatsoever, monochrome black and white only",
        "photorealistic, institutional, corporate surveillance state aesthetic",
        "NOT artistic, NOT cinematic, NOT flattering, NOT portrait",
        "poor dynamic range, halation around light sources, practical security camera quality",
    ]
    return ", ".join(filter(None, parts))


# ── Phase 1: Generate dalle3_prompt via Claude ────────────────────────────────

async def generate_dalle3_prompt(data: dict, client, semaphore) -> str | None:
    import anthropic
    summary = build_character_summary(data)
    user_msg = f"CHARACTER:\n{summary}"

    async with semaphore:
        for attempt in range(3):
            try:
                response = await client.messages.create(
                    model=CLAUDE_MODEL,
                    max_tokens=1024,
                    system=CLAUDE_SYSTEM,
                    messages=[{"role": "user", "content": user_msg}],
                )
                text = response.content[0].text.strip()
                # Strip any accidental code fences
                text = re.sub(r"^```[^\n]*\n?", "", text)
                text = re.sub(r"\n?```$", "", text).strip()
                if _is_tier1(data):
                    text = _apply_tier1_suffix(text)
                return text
            except Exception as e:
                if attempt < 2:
                    await asyncio.sleep(5 * (attempt + 1))
                else:
                    console.print(f"  [red]Claude error for {data.get('name')}: {e}[/red]")
                    return None
    return None


# ── Phase 2 / 3: Generate image via DALL-E 3 ─────────────────────────────────

async def _call_dalle(prompt: str, http_client: httpx.AsyncClient) -> tuple[bytes | None, str | None]:
    """Single DALL-E call. Returns (image_bytes, None) on success or (None, err_msg) on failure."""
    payload = {
        "model": DALLE_MODEL,
        "prompt": prompt[:4000],
        "n": 1,
        "size": DALLE_SIZE,
        "quality": DALLE_QUALITY,
        "response_format": "b64_json",
    }
    try:
        response = await http_client.post(
            "https://api.openai.com/v1/images/generations",
            json=payload,
            headers={"Authorization": f"Bearer {OPENAI_API_KEY}"},
            timeout=120.0,
        )
        if response.status_code == 200:
            b64 = response.json()["data"][0]["b64_json"]
            return base64.b64decode(b64), None
        try:
            err_msg = response.json()["error"]["message"]
        except Exception:
            err_msg = response.text[:200]
        return None, f"{response.status_code}: {err_msg}"
    except httpx.TimeoutException:
        return None, "timeout"
    except Exception as e:
        return None, str(e)


async def generate_image(entity_id: str, prompt: str, http_client: httpx.AsyncClient,
                         semaphore, char_name: str = "") -> bool:
    base_prompt  = strip_mj_params(prompt)
    clean_prompt = base_prompt

    async with semaphore:
        # ── Attempt 1: original prompt ────────────────────────────────
        for attempt in range(2):
            img_bytes, err = await _call_dalle(clean_prompt, http_client)
            if img_bytes:
                break
            if err and err.startswith("429") and attempt == 0:
                await asyncio.sleep(30)
                continue
            if err and (err.startswith("500") or err.startswith("502") or err.startswith("503")) and attempt == 0:
                await asyncio.sleep(10)
                continue
            break

        # ── Attempt 2: safety rejection — sanitize then retry ─────────
        if img_bytes is None and err and "safety" in err.lower():
            sanitized = sanitize_for_safety(clean_prompt)
            if sanitized != clean_prompt:
                console.print(f"  [yellow]Safety filter hit — retrying with sanitized prompt[/yellow]")
                img_bytes, err = await _call_dalle(sanitized, http_client)
            else:
                # Nothing to sanitize — retry once more as-is (sometimes passes)
                console.print(f"  [yellow]Safety filter hit — bare retry[/yellow]")
                img_bytes, err = await _call_dalle(clean_prompt, http_client)

        # ── Final failure ─────────────────────────────────────────────
        if img_bytes is None:
            if err and "safety" in (err or "").lower():
                _log_safety_rejection(entity_id, char_name, base_prompt)
                console.print(f"  [red]Safety rejected {entity_id} ({char_name}) — logged to safety_rejected.json[/red]")
            else:
                console.print(f"  [red]DALL-E failed for {entity_id}: {err}[/red]")
            return False

        idx = next_image_index(entity_id)
        filename = f"{entity_id}.{idx:02d}.png"
        out_path = MEDIA_DIR / filename
        out_path.write_bytes(img_bytes)
        console.print(f"  [green]Saved {filename} ({len(img_bytes):,} bytes)[/green]")
        return True


def _save_field(filepath: str, **fields) -> None:
    """Merge fields into the JSON file on disk (read → update → write)."""
    with open(filepath, "r", encoding="utf-8") as f:
        on_disk = json.load(f)
    on_disk.update(fields)
    with open(filepath, "w", encoding="utf-8") as f:
        json.dump(on_disk, f, indent=2, ensure_ascii=False)


# ── Main pipeline ─────────────────────────────────────────────────────────────

async def process_character(filepath: str, args, claude_client, http_client,
                            prompt_sem, image_sem) -> dict:
    result = {
        "path": filepath,
        "prompt_written": False,
        "image_saved": False,
        "cctv_saved": False,
        "skipped": False,
    }

    try:
        with open(filepath, "r", encoding="utf-8") as f:
            data = json.load(f)
    except Exception as e:
        console.print(f"  [red]Read error {filepath}: {e}[/red]")
        return result

    if not isinstance(data, dict) or data.get("type") not in ("character", None):
        result["skipped"] = True
        return result

    entity_id      = data.get("id", "")
    name           = data.get("name", filepath)

    # Never rewrite hand-crafted prompts for protected characters (--force still regenerates images)
    from constants import SKIP_CHARACTERS
    prompt_protected = name in SKIP_CHARACTERS
    existing_prompt = data.get("dalle3_prompt", "").strip()
    existing_cctv  = data.get("cctv_prompt", "").strip()
    img_exists     = has_image(entity_id)

    # Determine if this character needs any work
    needs_main = not (existing_prompt and img_exists) or args.force
    needs_cctv = (not existing_cctv or args.force) and not args.no_cctv and not args.prompt_only

    # Skip entirely only if no work to do
    if not needs_main and not needs_cctv:
        result["skipped"] = True
        return result

    # ── Phase 1: Prompt ───────────────────────────────────────────────────────
    if (not existing_prompt or args.force) and not prompt_protected:
        if not args.image_only and not args.dry_run:
            console.print(f"  Writing prompt for [cyan]{name}[/cyan]…")
            new_prompt = await generate_dalle3_prompt(data, claude_client, prompt_sem)
            if new_prompt:
                _save_field(filepath, dalle3_prompt=new_prompt)
                existing_prompt = new_prompt
                result["prompt_written"] = True
        elif args.dry_run:
            console.print(f"  [yellow]DRY RUN[/yellow] would write prompt for [cyan]{name}[/cyan]")

    # ── Phase 2: Main image ───────────────────────────────────────────────────
    if existing_prompt and (not img_exists or args.force):
        if not args.prompt_only and not args.dry_run:
            if not OPENAI_API_KEY:
                console.print("  [red]No OpenAI API key — set SS_OPENAI_API_KEY or OPENAI_API_KEY[/red]")
            else:
                console.print(f"  Generating image for [cyan]{name}[/cyan]…")
                ok = await generate_image(entity_id, existing_prompt, http_client, image_sem, char_name=name)
                result["image_saved"] = ok
        elif args.dry_run:
            console.print(f"  [yellow]DRY RUN[/yellow] would generate image for [cyan]{name}[/cyan]")

    # ── Phase 3: CCTV surveillance still (suspended — requires explicit --cctv flag) ──
    if args.cctv and needs_cctv and (result["image_saved"] or has_image(entity_id)):
        cctv_prompt = build_cctv_prompt(data, existing_prompt)
        if not args.dry_run and OPENAI_API_KEY:
            console.print(f"  Generating CCTV still for [cyan]{name}[/cyan]…")
            ok = await generate_image(entity_id, cctv_prompt, http_client, image_sem, char_name=name + " [CCTV]")
            if ok:
                _save_field(filepath, cctv_prompt=cctv_prompt)
                result["cctv_saved"] = True
        elif args.dry_run:
            criminal_flag = "[red]criminal[/red]" if _is_criminal_adjacent(data) else ""
            console.print(f"  [yellow]DRY RUN[/yellow] would generate CCTV for [cyan]{name}[/cyan] {criminal_flag}")

    return result


async def run(args):
    import anthropic

    # Collect files
    if args.id:
        files = list(PEOPLE_DIR.glob(f"{args.id}*.json"))
        if not files:
            # Try by name slug
            files = sorted(PEOPLE_DIR.glob("*.json"))
            files = [f for f in files if args.id.lower() in f.stem.lower()]
    else:
        files = sorted(glob.glob(str(PEOPLE_DIR / "*.json")))

    # Filter by tier if requested
    if args.tier:
        tiers = {str(t).strip() for t in args.tier.split(",")}
        filtered = []
        for fp in files:
            try:
                d = json.loads(Path(fp).read_text(encoding="utf-8"))
                if str(d.get("tier", "")).strip() in tiers:
                    filtered.append(fp)
            except Exception:
                pass
        console.print(f"  [cyan]Tier filter {args.tier}:[/cyan] {len(filtered)} of {len(files)} characters")
        files = filtered

    if args.limit:
        files = files[: args.limit]

    if not files:
        console.print("[red]No files found.[/red]")
        return

    # Count what needs work
    needs_prompt, needs_image, needs_cctv_count, done = 0, 0, 0, 0
    for fp in files:
        try:
            with open(fp, "r", encoding="utf-8") as f:
                d = json.load(f)
            eid = d.get("id", "")
            has_p = bool(d.get("dalle3_prompt", "").strip())
            has_i = has_image(eid)
            has_c = bool(d.get("cctv_prompt", "").strip())
            if has_p and has_i:
                done += 1
            else:
                if not has_p: needs_prompt += 1
                if not has_i: needs_image += 1
            if not has_c: needs_cctv_count += 1
        except Exception:
            pass

    console.print(f"\n[bold]CHARACTER IMAGE PIPELINE[/bold]")
    console.print(f"  Total files:    {len(files)}")
    console.print(f"  Already done:   {done}")
    console.print(f"  Need prompt:    {needs_prompt}")
    console.print(f"  Need image:     {needs_image}")
    console.print(f"  No CCTV yet:    {needs_cctv_count}")
    console.print(f"  Claude model:   {CLAUDE_MODEL}")
    console.print(f"  DALL-E model:   {DALLE_MODEL}  size={DALLE_SIZE}  quality={DALLE_QUALITY}")
    console.print(f"  OpenAI key:     {'SET' if OPENAI_API_KEY else '[red]MISSING[/red]'}")
    if args.cctv:
        console.print(f"  [cyan]CCTV pass enabled[/cyan] — eligible chars without CCTV will get one")
    if args.no_cctv:
        console.print(f"  [dim]CCTV generation disabled[/dim]")
    if args.dry_run:
        console.print("[yellow]  DRY RUN — no API calls[/yellow]")

    MEDIA_DIR.mkdir(parents=True, exist_ok=True)

    claude_client = anthropic.AsyncAnthropic(api_key=ANTHROPIC_API_KEY)
    prompt_sem = asyncio.Semaphore(CONCURRENCY)
    image_sem = asyncio.Semaphore(3)  # DALL-E is rate-limited, keep low

    prompts_written = 0
    images_saved = 0
    cctv_saved = 0
    skipped = 0

    async with httpx.AsyncClient() as http_client:
        # Process sequentially in batches to avoid overwhelming both APIs
        batch_size = 10
        for i in range(0, len(files), batch_size):
            batch = files[i : i + batch_size]
            coros = [
                process_character(fp, args, claude_client, http_client, prompt_sem, image_sem)
                for fp in batch
            ]
            results = await asyncio.gather(*coros)
            for r in results:
                if r["skipped"]:
                    skipped += 1
                if r["prompt_written"]:
                    prompts_written += 1
                if r["image_saved"]:
                    images_saved += 1
                if r["cctv_saved"]:
                    cctv_saved += 1

    console.print(f"\n[bold green]Done.[/bold green]")
    console.print(f"  Prompts written: {prompts_written}")
    console.print(f"  Images saved:    {images_saved}")
    console.print(f"  CCTV saved:      {cctv_saved}")
    console.print(f"  Skipped:         {skipped}")


def main():
    parser = argparse.ArgumentParser(description="Generate DALL-E 3 prompts and images for characters")
    parser.add_argument("--limit", type=int, default=None, help="Process only first N characters")
    parser.add_argument("--id", type=str, default=None, help="Process single character by GUID or name fragment")
    parser.add_argument("--prompt-only", action="store_true", help="Write dalle3_prompt only, skip image generation")
    parser.add_argument("--image-only", action="store_true", help="Generate images only, skip prompt generation")
    parser.add_argument("--dry-run", action="store_true", help="Preview without calling any API")
    parser.add_argument("--force", action="store_true", help="Regenerate even if prompt/image already exists")
    parser.add_argument("--tier", type=str, default=None,
                        help="Only process characters of this tier (e.g. --tier 1 or --tier 1,2)")
    parser.add_argument("--cctv", action="store_true",
                        help="Retroactive CCTV pass — add CCTV still to chars that already have a main image but no cctv_prompt")
    parser.add_argument("--no-cctv", action="store_true", dest="no_cctv",
                        help="Skip CCTV generation entirely this run")
    args = parser.parse_args()

    if not ANTHROPIC_API_KEY and not args.image_only:
        console.print("[red]ANTHROPIC_API_KEY not set[/red]")
        sys.exit(1)

    asyncio.run(run(args))


if __name__ == "__main__":
    main()
