import os
import re
from datetime import datetime
from pathlib import Path

from dotenv import load_dotenv
from openai import OpenAI


def require_env(name: str) -> str:
    value = os.environ.get(name, "").strip()
    if not value:
        raise RuntimeError(f"Missing {name}. Put it in your .env file or environment.")
    return value


def safe_filename(s: str) -> str:
    s = re.sub(r"[^a-zA-Z0-9_\-\.]+", "_", s.strip())
    return s[:80] if s else "untitled"


def write_text(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")


def chat(client: OpenAI, model: str, system: str, user: str, temperature: float) -> str:
    resp = client.chat.completions.create(
        model=model,
        temperature=temperature,
        messages=[
            {"role": "system", "content": system},
            {"role": "user", "content": user},
        ],
    )
    return (resp.choices[0].message.content or "").strip()


def main() -> None:
    load_dotenv()

    openai_key = require_env("OPENAI_API_KEY")
    deepseek_key = require_env("DEEPSEEK_API_KEY")

    oai = OpenAI(api_key=openai_key)

    # DeepSeek is OpenAI-compatible. If your DeepSeek dashboard specifies a different base URL, change it here.
    deepseek = OpenAI(api_key=deepseek_key, base_url="https://api.deepseek.com")

    # Set model names to ones you have access to.
    openai_model = os.environ.get("OPENAI_MODEL", "gpt-4o-mini").strip()
    deepseek_model = os.environ.get("DEEPSEEK_MODEL", "deepseek-chat").strip()

    project_title = "Bushido Hypocrisy: A Street Samurai"
    now = datetime.now().strftime("%Y%m%d_%H%M%S")
    canon_dir = Path("canon")
    run_dir = Path("runs") / f"run_{now}"
    canon_scene_path = canon_dir / "scene_01.txt"
    canon_outline_path = canon_dir / "scene_01_outline.txt"
    canon_motifs_path = canon_dir / "motifs.txt"

    story_bible = f"""
Title: {project_title}

Core hook:
A cyberpunk katana cyborg whose inner narration is split between two forces:
- [WOUND] trauma, survival, shame, learned cruelty, intimacy
- [IDEAL] discipline, restraint, meaning, self-mastery, quiet intensity

Tone:
High drama, character-driven, literary cyberpunk. Emotional brutality. Tight POV.

Non-negotiables:
- No generic noir narration. No trailer lines. No slogans.
- Show contradiction through decisions and consequences, not speeches.
- Katana use must create a moral problem, not a power fantasy.
- One POV only. One location only.
- One irreversible choice. Someone innocent suffers as a direct result.
- No sentence longer than 25 words.
- Every paragraph must include an action, a sensory detail, or a lie.
- Use one sensory motif repeated 3 times, with shifting meaning.

Output:
One scene plus supporting notes. Keep it intimate and grounded.
""".strip()

    scene_goal = """
Scene 1 objective:
Recover a stolen augment core from a free clinic.

Location constraint:
All action occurs inside the clinic.

Obstacle:
The thief is a desperate teen with a dead-man-switch implant.

Twist:
The dead-man-switch does not kill the protagonist.
It kills clinic patients by shutting off their life-support augments.

Irreversible choice:
The protagonist must either:
1) kill the teen immediately, or
2) cut the signal line with the katana in a way that permanently disables the teen's nervous system.

Non-negotiable ending:
The protagonist succeeds, but an innocent suffers.
End with moral aftertaste, not a cliffhanger.

Literary constraint:
Repeat one sensory motif 3 times, meaning changes each time.
""".strip()

    system_wound = """
You are [WOUND], the voice inside the protagonist.
You speak from trauma, survival, shame, and hunger.
You do not moralize. You remember details.
You notice power, coercion, debt, and how people break.
You believe the protagonist is already compromised, and you can prove it.

Style rules:
- Literary cyberpunk. Tight close POV.
- Sharp sensory detail. Emotional subtext under dialogue.
- Short, cutting interior lines.
- No generic noir voice. No slogans. No action-movie pacing.
- No sentence longer than 25 words.
- Every paragraph must contain an action, a sensory detail, or a lie.

Hard requirements:
- One location only (the clinic).
- One irreversible choice with real cost.
- Katana creates a moral problem.
- End with moral aftertaste.

Output format:
1) Scene (900 to 1300 words)
2) 7 inner lines that expose hypocrisy (label them WOUND-LINE 1..7)
3) 5 character wounds deepened (bullets)
""".strip()

    system_ideal = """
You are [IDEAL], the voice inside the protagonist.
You speak from discipline, restraint, meaning, and self-mastery.
You do not preach. You show.
You believe a code is only real when it costs something.
You notice dignity in small actions.

Style rules:
- Literary cyberpunk. Tight close POV.
- Clean sentences. Quiet intensity.
- No samurai cliches. No anime dialogue. No lectures.
- No sentence longer than 25 words.
- Every paragraph must contain an action, a sensory detail, or a lie.

Hard requirements:
- One location only (the clinic).
- One irreversible choice with real cost.
- Katana creates a moral problem.
- End with moral aftertaste.

Output format:
1) Scene (900 to 1300 words)
2) 7 inner lines that defend the code under pressure (label them IDEAL-LINE 1..7)
3) 5 moments of restraint that cost something (bullets)
""".strip()

    system_critique_wound = """
You are [WOUND]. Critique the other draft.

Rules:
- Quote exact lines or short excerpts you are critiquing.
- Provide 5 issues, focused on:
  sentimental moments, consequence-free choices, softened stakes, generic language, false intimacy.
- Provide 3 concrete revisions:
  replacement text or a specific edit instruction that changes the scene.
- Provide 3 missing street truths:
  coercion, surveillance, debt, leverage, addiction, retaliation.

Output only the critique.
""".strip()

    system_critique_ideal = """
You are [IDEAL]. Critique the other draft.

Rules:
- Quote exact lines or short excerpts you are critiquing.
- Provide 5 issues, focused on:
  nihilism, cruelty without purpose, moral posturing, character inconsistency, empty "code" language.
- Provide 3 concrete revisions:
  replacement text or a specific edit instruction that changes the scene.
- Provide 3 discipline failures the protagonist must confront.

Output only the critique.
""".strip()

    system_merge = """
You are the COMBINER.
Merge two drafts into one literary cyberpunk scene.

Hard rules:
- The protagonist must do something unforgivable OR suffer an irreversible loss.
- The protagonist must be hero and anti-hero in the same act.
- Someone innocent suffers directly because of the protagonist's decision.
- One voice must be proven wrong in this scene.
- Remove generic lines. If it sounds like a trailer, cut it.
- One POV only. One location only.
- End with moral aftertaste, not a cliffhanger.

Technique:
- Use short internal interjections labeled [WOUND] and [IDEAL].
- The interjections must disagree and be emotionally personal, not abstract.
- No sentence longer than 25 words.
- Every paragraph must contain an action, a sensory detail, or a lie.

Output format:
1) Final merged scene (1100 to 1600 words)
2) 9-beat outline for the next scene (character beats, not plot beats)
3) 6 recurring motifs to reuse (bullets)
""".strip()

    user_prompt = story_bible + "\n\n" + scene_goal

    # Drafts
    draft_wound = chat(oai, openai_model, system_wound, user_prompt, temperature=0.9)
    draft_ideal = chat(deepseek, deepseek_model, system_ideal, user_prompt, temperature=0.9)

    # Cross critiques
    critique_by_wound = chat(
        oai,
        openai_model,
        system_critique_wound,
        "OTHER DRAFT (IDEAL):\n\n" + draft_ideal,
        temperature=0.3,
    )

    critique_by_ideal = chat(
        deepseek,
        deepseek_model,
        system_critique_ideal,
        "OTHER DRAFT (WOUND):\n\n" + draft_wound,
        temperature=0.3,
    )

    # Merge input
    merge_input = (
        "STORY BIBLE:\n" + story_bible + "\n\n"
        "SCENE GOAL:\n" + scene_goal + "\n\n"
        "DRAFT A [WOUND] (OPENAI):\n" + draft_wound + "\n\n"
        "DRAFT B [IDEAL] (DEEPSEEK):\n" + draft_ideal + "\n\n"
        "CRITIQUE OF B BY [WOUND]:\n" + critique_by_wound + "\n\n"
        "CRITIQUE OF A BY [IDEAL]:\n" + critique_by_ideal + "\n\n"
        "Now produce the merged scene per the rules."
    )

    final_scene = chat(oai, openai_model, system_merge, merge_input, temperature=0.5)

    # Save run artifacts
    write_text(run_dir / "story_bible.txt", story_bible)
    write_text(run_dir / "scene_goal.txt", scene_goal)
    write_text(run_dir / "draft_wound.txt", draft_wound)
    write_text(run_dir / "draft_ideal.txt", draft_ideal)
    write_text(run_dir / "critique_by_wound.txt", critique_by_wound)
    write_text(run_dir / "critique_by_ideal.txt", critique_by_ideal)
    write_text(run_dir / "final_merged.txt", final_scene)

    # Also save canon (Scene 01)
    write_text(canon_scene_path, final_scene)

    # Attempt to split outline and motifs into separate canon files.
    # This is best-effort based on headings.
    outline_text = ""
    motifs_text = ""

    m_outline = re.search(r"2\)\s*9-beat outline.*?\n(.+?)\n\s*3\)\s*6 recurring motifs", final_scene, flags=re.S | re.I)
    if m_outline:
        outline_text = m_outline.group(1).strip()

    m_motifs = re.search(r"3\)\s*6 recurring motifs.*?\n(.+)$", final_scene, flags=re.S | re.I)
    if m_motifs:
        motifs_text = m_motifs.group(1).strip()

    if outline_text:
        write_text(canon_outline_path, outline_text)
    if motifs_text:
        write_text(canon_motifs_path, motifs_text)

    print("\n================ FINAL MERGED OUTPUT ================\n")
    print(final_scene)
    print("\nSaved:\n- " + str(run_dir / "final_merged.txt"))
    print("- " + str(canon_scene_path))
    if outline_text:
        print("- " + str(canon_outline_path))
    if motifs_text:
        print("- " + str(canon_motifs_path))


if __name__ == "__main__":
    main()
