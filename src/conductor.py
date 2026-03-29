import json

from .facet import Facet
from .character import Character
from . import llm


CONTEXT_ANALYZER_SYSTEM = """You are a narrative psychologist analyzing a story beat.
Given the current scene context, identify which psychological domains are active.

Return a JSON object with:
- "tags": a list of psychological trigger tags from this set:
  violence, betrayal, vulnerability, desperation, children_in_danger, poverty,
  coercion, debt, addiction, moral_choice, discipline_test, honor_at_stake,
  precision_required, restraint_under_pressure, teaching_moment, ritual,
  combat, near_death, hunger, desire, threat, adrenaline, cornered, pain,
  rationalization, denial, hypocrisy, self_deception, killing, enjoying_violence,
  moral_compromise, social_interaction, negotiation, reputation, being_watched,
  client_meeting, intimidation, composure_under_pressure, ethical_crossroads,
  memory_of_training, facing_death, protecting_innocent, meditation, dawn_or_dusk
- "dominant_emotion": the primary emotional state of the protagonist
- "stakes": what's at risk in this beat

Return ONLY valid JSON, no other text."""


BEAT_WRITER_TEMPLATE = """You are writing a single narrative beat for a cyberpunk literary fiction scene.

STORY CONTEXT:
{story_context}

SCENE SO FAR:
{scene_so_far}

CURRENT BEAT GOAL:
{beat_goal}

CHARACTER:
{character_summary}

YOUR ROLE:
You are the LEAD voice for this beat: {lead_facet_label} — {lead_facet_domain}

SUPPORTING VOICES (insert 1-2 short interior interjections from these):
{supporting_facets_desc}

CORE MEMORIES AVAILABLE TO YOU:
{core_memories}

LITERARY RULES:
- No sentence longer than 25 words.
- Every paragraph must contain an action, a sensory detail, or a lie.
- Use one sensory motif with shifting meaning if continuing from prior beats.
- No generic noir narration. No trailer lines. No slogans.
- Show contradiction through decisions and consequences, not speeches.
- Do NOT rush to end the scene. Develop this single beat fully.
- Write 200-400 words for this beat.

Write the beat now. Include labeled interjections from supporting facets like:
[WOUND] *interior line here*
[GHOST] *interior line here*

The prose style should reflect {lead_facet_label}'s voice: {lead_facet_tone}"""


def analyze_context(scene_context: str) -> dict:
    raw = llm.generate(
        system=CONTEXT_ANALYZER_SYSTEM,
        user=scene_context,
        temperature=0.2,
        max_tokens=512,
    )
    # Strip markdown fences if present
    cleaned = raw.strip()
    if cleaned.startswith("```"):
        cleaned = cleaned.split("\n", 1)[1] if "\n" in cleaned else cleaned[3:]
    if cleaned.endswith("```"):
        cleaned = cleaned[:-3]
    cleaned = cleaned.strip()
    return json.loads(cleaned)


def score_facets(character: Character, context_tags: list[str]) -> list[tuple[str, float]]:
    scores = []
    for name, facet in character.facets.items():
        score = facet.matches_context(context_tags)
        scores.append((name, score))
    scores.sort(key=lambda x: x[1], reverse=True)
    return scores


def select_facets(character: Character, context_tags: list[str]) -> tuple[Facet, list[Facet]]:
    scores = score_facets(character, context_tags)
    lead_name = scores[0][0]
    lead = character.get_facet(lead_name)
    supporting = []
    for name, score in scores[1:3]:
        if score > 0:
            supporting.append(character.get_facet(name))
    # Always have at least one supporting voice
    if not supporting and len(scores) > 1:
        supporting.append(character.get_facet(scores[1][0]))
    return lead, supporting


def generate_beat(
    character: Character,
    story_context: str,
    scene_so_far: str,
    beat_goal: str,
    lead_facet: Facet,
    supporting_facets: list[Facet],
) -> str:
    supporting_desc = "\n".join(
        f"- {f.label}: {f.domain} (tone: {f.voice_tone})"
        for f in supporting_facets
    )
    core_memories = "\n".join(f"- {m}" for m in lead_facet.core_memories)

    prompt = BEAT_WRITER_TEMPLATE.format(
        story_context=story_context,
        scene_so_far=scene_so_far if scene_so_far else "(Scene beginning — this is the first beat)",
        beat_goal=beat_goal,
        character_summary=character.summary(),
        lead_facet_label=lead_facet.label,
        lead_facet_domain=lead_facet.domain,
        lead_facet_tone=lead_facet.voice_tone,
        supporting_facets_desc=supporting_desc if supporting_desc else "(none)",
        core_memories=core_memories,
    )

    return llm.generate(
        system=lead_facet.system_prompt,
        user=prompt,
        model=lead_facet.model,
        temperature=lead_facet.temperature,
    )


def run_beat(
    character: Character,
    story_context: str,
    scene_so_far: str,
    beat_goal: str,
    force_lead: str | None = None,
) -> tuple[str, Facet, list[Facet], dict]:
    analysis = analyze_context(
        f"Story context: {story_context}\n\nScene so far: {scene_so_far}\n\nCurrent beat: {beat_goal}"
    )
    context_tags = analysis.get("tags", [])

    if force_lead and force_lead in character.facets:
        lead = character.get_facet(force_lead)
        scores = score_facets(character, context_tags)
        supporting = [
            character.get_facet(name)
            for name, score in scores[:3]
            if name != force_lead
        ][:2]
    else:
        lead, supporting = select_facets(character, context_tags)

    beat_text = generate_beat(
        character=character,
        story_context=story_context,
        scene_so_far=scene_so_far,
        beat_goal=beat_goal,
        lead_facet=lead,
        supporting_facets=supporting,
    )

    return beat_text, lead, supporting, analysis
