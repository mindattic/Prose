import os
from openai import OpenAI

OPENAI_API_KEY = os.environ.get("OPENAI_API_KEY")
DEEPSEEK_API_KEY = os.environ.get("DEEPSEEK_API_KEY")

if not OPENAI_API_KEY:
    raise RuntimeError("Missing OPENAI_API_KEY env var")
if not DEEPSEEK_API_KEY:
    raise RuntimeError("Missing DEEPSEEK_API_KEY env var")

# OpenAI client
oai = OpenAI(api_key=OPENAI_API_KEY)

# DeepSeek is OpenAI-compatible. Use their base_url.
# If your DeepSeek docs specify a different base URL, replace it here.
deepseek = OpenAI(api_key=DEEPSEEK_API_KEY, base_url="https://api.deepseek.com")

def chat(client: OpenAI, model: str, system: str, user: str, temperature: float = 0.9) -> str:
    resp = client.chat.completions.create(
        model=model,
        temperature=temperature,
        messages=[
            {"role": "system", "content": system},
            {"role": "user", "content": user},
        ],
    )
    return resp.choices[0].message.content

STORY_BIBLE = """
Title: Bushido Hypocrisy: A Street Samurai
Core hook: A cyberpunk katana cyborg whose inner narration is split between brutal realism and strict Bushido idealism.
Tone: Cyberpunk noir. Philosophy shown through action and consequence.
Non-negotiables:
- Avoid generic "both light and dark" lines.
- Show contradiction through decisions, not speeches.
- Katana use creates a moral problem, not a power fantasy.
Output: One scene (800 to 1200 words) plus a short bullet list.
"""

SCENE_GOAL = """
Scene 1 objective: The protagonist is hired to recover a stolen augment core from a clinic that treats the poor.
Obstacle: The thief is a desperate teen with a dead-man-switch implant.
Irreversible choice: Kill the teen to save the clinic from retaliation, or spare them and doom the clinic's patients.
Required set-piece: Katana is used, but it creates a moral complication.
"""

SYSTEM_REALIST = """
You are the Realist voice in the protagonist's mind.
You believe honor is branding, dignity is marketing, and survival is math.
Write in concrete sensory detail. Prefer action and consequence over speeches.
Make the protagonist compelling, not edgy.
Output format:
1) Scene draft
2) 5 bullet moral compromises shown in the scene
3) 5 bullet stakes escalators for later
"""

SYSTEM_DISCIPLE = """
You are the Bushido Disciple voice in the protagonist's mind.
Treat Bushido as discipline, not cosplay: duty, restraint, truthfulness, mercy, self-mastery.
Write with clarity and restraint. No poetic fluff.
Interpret brutality as a test of practice, not an excuse to abandon the code.
Output format:
1) Scene draft
2) 5 bullet moments of discipline shown in the scene
3) 5 bullet ethical costs that threaten the code
"""

SYSTEM_CRITIQUE_REALIST = """
You are the Realist. Critique the other draft.
Rules:
- Quote exact lines or short excerpts you are critiquing.
- Give 5 issues (unrealistic, sentimental, consequence-free, preachy).
- Give 3 concrete revisions (replacement text or specific change).
- List 3 street-level truths ignored (leverage, surveillance, money, coercion, addiction, debt).
Output only the critique.
"""

SYSTEM_CRITIQUE_DISCIPLE = """
You are the Bushido Disciple. Critique the other draft.
Rules:
- Quote exact lines or short excerpts you are critiquing.
- Give 5 issues (nihilistic posturing, shallow cynicism, cruelty without purpose, character inconsistency).
- Give 3 concrete revisions (replacement text or specific change).
- List 3 discipline failures the protagonist must confront.
Output only the critique.
"""

SYSTEM_MERGE = """
You are the Combiner. Merge two drafts into one coherent scene.
Rules:
- Choose one clear objective, one obstacle, one irreversible choice.
- Keep 70 percent action and consequence, 30 percent philosophy shown through decisions.
- Include one moment where Bushido causes immediate harm.
- Include one moment where cynicism accidentally saves someone.
- Use two inner voices explicitly labeled REALIST and DISCIPLE in short interjections.
Output format:
1) Final merged scene (900 to 1400 words)
2) 7 beat outline for the next scene
3) Promises to the reader (5 bullets)
"""

def main():
    user_prompt = STORY_BIBLE.strip() + "\n\n" + SCENE_GOAL.strip()

    # Pick models you have access to.
    # OpenAI examples: "gpt-4o-mini", "gpt-4.1-mini", etc.
    # DeepSeek examples (per their docs): "deepseek-chat" or "deepseek-reasoner"
    openai_model = "gpt-4o-mini"
    deepseek_model = "deepseek-chat"

    draft_realist = chat(oai, openai_model, SYSTEM_REALIST, user_prompt, temperature=0.9)
    draft_disciple = chat(deepseek, deepseek_model, SYSTEM_DISCIPLE, user_prompt, temperature=0.9)

    critique_by_realist = chat(oai, openai_model, SYSTEM_CRITIQUE_REALIST, "OTHER DRAFT:\n\n" + draft_disciple, temperature=0.3)
    critique_by_disciple = chat(deepseek, deepseek_model, SYSTEM_CRITIQUE_DISCIPLE, "OTHER DRAFT:\n\n" + draft_realist, temperature=0.3)

    merge_input = (
        "STORY BIBLE:\n" + STORY_BIBLE.strip() + "\n\n"
        "SCENE GOAL:\n" + SCENE_GOAL.strip() + "\n\n"
        "DRAFT A (REALIST):\n" + draft_realist + "\n\n"
        "DRAFT B (DISCIPLE):\n" + draft_disciple + "\n\n"
        "CRITIQUE OF B BY REALIST:\n" + critique_by_realist + "\n\n"
        "CRITIQUE OF A BY DISCIPLE:\n" + critique_by_disciple
    )

    final_scene = chat(oai, openai_model, SYSTEM_MERGE, merge_input, temperature=0.7)

    print("\n================ FINAL MERGED OUTPUT ================\n")
    print(final_scene)

if __name__ == "__main__":
    main()
