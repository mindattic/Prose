---
codex: SS
project: StreetSamurai
code: CRAFT
layer: craft
status: live
triggers: prose, beat, write, voice, sentence, dialogue, interiority, scene, pov, character, narrat
updated: 2026-07-20
---

# CRAFT — Universal Prose Rules {#SS-CRAFT}

> **Scope: all universes, all stories.** Universe rules live in GLMZ.md / SCRY.md; story rules in
> Nodes.NodeBible. Narrower scope wins on conflict.
>
> **DOCTRINE RESET 2026-07-20 (SS-A46).** These books were dense and unforgiving; the prose kept
> readers *out* of the world instead of pulling them in. Every reader panel, across all 21 GLMZ
> stories, named the same wall: ornate, over-worked sentences that confuse rather than immerse.
> This doctrine retires that style. **The new standard: a bright high-school freshman reads any
> beat once, start to finish, without re-reading a sentence — and the depth is still there,
> underneath, in the characters and their choices.** Clarity on the surface; nuance in the bones.

---

## 0. The Clarity Standard {#SS-CRAFT-0}

**Write so it can be read once.** If a sentence has to be read twice to be understood, it failed.
This is the top rule; every rule below serves it.

- **One idea per sentence.** Short to medium. A very long sentence is a defect to break, not a
  style to admire.
- **Plain, concrete words.** Say *heart*, not *cardiac muscle*; *counted him*, not *ran the
  arithmetic of him*. Anglo-Saxon over Latinate. Name the thing.
- **Depth lives underneath, not on top.** The surface is clear; the meaning is deep. Nuance comes
  from **what a character wants, chooses, and hides** — motives and consequences the reader turns
  over afterward — never from difficult sentences. A story a freshman can read and an adult can
  re-read for what they missed. Never a story an adult has to decode.
- **The test:** read the beat aloud at normal speed. Anywhere you stumble, re-read, or lose the
  thread — the prose is wrong, not the reader.

---

## 1. Camera and Scene Architecture

**The camera looks out.** A character is shown through what they do and notice; the world reacts
to them. No narrated self-analysis. Not a diary.

**Every scene is a transaction under a clock.** Somebody wants something, something resists, it
costs. A scene that costs nothing did not happen.

**Each beat is one story move** — an action, an exchange, a turn — then hands off. Real
paragraphs; never run-on blocks, never a ladder of one-line fragments.

**Action beats carry weight; quiet beats stay physical.** An action beat that doesn't advance the
tension is stage business. A quiet beat without a sensory or physical anchor is abstraction.

---

## 2. Sentences and Voice

**Short-to-medium sentences, one idea each.** Vary the length for rhythm — a run of medium lines,
then a short hard one at the hit. But length is never the point; clarity is. When in doubt, cut
the sentence in two.

**No associative chains.** The "X was Y, and Y was Z, and Z was the thing he'd been" construction
is banned. It is the single worst offender for readability. State the thing plainly, once.

**The narrator is never wise.** No sentence about life, death, people-in-general, or
the-world-in-general. Nothing embroiderable on a pillow. Wisdom lives only inside dialogue, in
character, with a target.

**Verbs do the work.** A strong verb beats an adjective and an adverb. Cut modifiers that don't
change the picture.

**Metaphor is rare and it is plain.** One earned, literal-scrutiny-surviving image per scene, at
most. When a lyric strains, the fix is a plain line, not a fancier lyric. Purple prose at the
emotional peak is the enemy of the emotional peak — the feeling lands in the plain words.

**Interiority budget: one or two flat lines per scene.** Italic inner monologue is a last resort —
a single sentence, never a paragraph, never a crutch. Prefer showing the thought as an action.

---

## 3. Dialogue

Quoted speech always — never italics, never asterisks. Each speaker on their own line. Questions
end in "?" and are attributed `asked/asks` (never `says/said` on a question).

People say the smaller, drier thing when the larger thing is available, and answer the question
under the question. Dialogue is where character voice mainly lives — let people sound like
themselves here, in word choice and rhythm, not in the narration.

**No filler-wit.** A wry universal truth is not characterization. Every line reveals character,
raises stakes, or lands a real joke. Balance a clever character with a plain line of real feeling.

---

## 4. Interiority and Information

**Feeling arrives as objects and gestures, not statements.** Nobody names a feeling. The
thank-you said ten thousand times lives in the gesture.

**The Noticing:** one small concrete unexplained detail per scene, and at least one NON-VISUAL
sense (what the rail feels like, what the stairwell smells like).

**Worldbuild by implication.** Name a thing, gloss it in one plain in-voice clause, move on.
**Never an info-dump.** If the reader needs three sentences of lore to follow the scene, the
scene is built wrong — dramatize the fact instead of explaining it. Jargon never opens a beat;
the body in the room does.

---

## 5. Structure and Payoff

**Plant small, pay exact.** A thing coined early returns transformed at the end, never
re-explained. Death gets no last words and no slow motion. Grief is handled through objects and
priced logistics.

**No on-the-nose title-drops**, especially as closers.

Every story is a **self-contained adventure**: want, resistance, cost, resolution inside the
strand. Series arcs ride under the adventures, never instead of them.

---

## 6. Emotional and Interpersonal Craft

**One involuntary felt response per scene, body before mind.** The shaking before the thought.
Physical before cognitive; never named, only shown.

**Relational work in every two-hander.** What two people want from each other underneath what
they say is the engine of the scene.

**Character doctrine:** circumstance → choice → definition. A wound is not a character; the
response to the wound is. This is where the depth goes — a clear surface over choices the reader
keeps turning over.

---

## 7. Point of View — Clear, Not Coded

**Show the POV through what it notices and does, in plain sentences.** What the camera lands on,
in what order — that is the character. Do **not** narrate how the character thinks, and do not
build a special "cognitive architecture" voice for them (no ledger-brains, no arithmetic-of-the-
room, no filing/parliament/geometry as a way of thinking). Those are retired.

**Character color is light.** Two people can be told in the same clear house voice and still read
as distinct, through word choice, sentence rhythm, and what each one notices — a medic notices
hands and wounds; a hunter notices weight and exits. That difference is a light touch on a clear
base, never a dense private dialect the reader has to learn.

**Voice is organic to the narrator, never imposed (SS-A46).** There are no house tonal registers
and no flagship voice. The old JOY (warmth-strange), SORROW (elegiac-dread), and Kyle / CODA
(fusion) registers are **retired and deleted** — nothing prescribes a mood-engine or a signature
style on top of a story. Each POV chapter simply sounds like its own narrator, drawn from who that
character is and what they'd notice: a Pixel chapter is in Pixel's voice; a Bear chapter is in
Bear's. The clear base voice (§0–§2) is the floor; the character's own diction and attention are
the only "register." A narrator's voice lives in their **Character record** (the `Speech*` and
`Psychology*` fields), which DCM loads per-beat when that character is on the page — so the voice
evolves as the character does. There are no `docs/registers/` files; that voice note IS the
character record, a light description of a natural voice, never a template to perform.

**Trust the reader.** Show the evidence; do not state what it means. Then stop.

---

## 8. Banned Mannerisms (the density that kept readers out) {#SS-CRAFT-8}

Retired 2026-07-20. None of these appear in new prose, and all are removed on any rewrite:

1. **Associative chains** — "it was X, and X was Y, and Y was…". State it once, plainly.
2. **Cognitive-architecture tics** — "the arithmetic," "he did the math," "he filed it," and any
   filing / ledger / parliament / geometry framing of a character's thinking. (Formerly Kyle's
   protected register — now retired everywhere. See SS-A46.)
3. **The observation tic** — "noted / logged / catalogued / clocked-and-filed" as a thought-verb.
   A character can notice a thing; they do not "log" it.
4. **Mood-soup** — atmosphere and interiority that crowd out the plot. Mood serves the scene's
   forward motion or it goes.
5. **Purple prose at the peak** — stacked similes and lush abstraction where the feeling should
   land plainest. The big moment gets the plainest words.
6. **Italic-thought crutch** — italicized inner-monologue fragments used as a recurring beat.
7. **Over-explanation** — restating what the scene already showed, or explaining the theme after
   the action enacted it.
8. **Jargon front-loading and info-dumps** — see §4.

---

## 9. The DON'Ts and DOs (from the LDGR beat audit — still hold, now serving clarity)

**DON'T:** declare the POV's thinking instead of showing behavior · repeat the observation tic ·
front-load the arc's insight · state what evidence means · summarize the theme in the coda ·
over-length the payoff (the most important moment needs the fewest words) · use structured-
reasoning formats ("Possibility A / B") in narration · name what a character chose not to say.

**DO:** ground interiority in a physical object · let "no" be enough · trust one-line payoffs ·
match sentence length to the moment (short to land) · show cognition through what the camera
lands on · parcel insight across the arc · earn any procedural voice by doing, not commenting ·
end when the action is complete.

---

## 10. Quality Check (per beat, before submit)

- [ ] Could a bright freshman read every sentence once, no re-reading? (§0)
- [ ] Any associative chain ("X was Y, Y was Z") or cognitive-architecture tic (arithmetic / filed / did the math)? Cut it. (§8.1–8.3)
- [ ] Any sentence too long to hold in one breath? Break it. (§2)
- [ ] Any italic inner-monologue fragment, or interiority over two lines? (§2, §8.6)
- [ ] Does a beat open on jargon instead of the body in the room? (§4)
- [ ] Does the coda explain the theme the action already enacted? (§9)
- [ ] One non-visual sense present? One involuntary felt response, body before mind? (§4, §6)
- [ ] Did the narrator say anything "wise" about the world? Cut it. (§2)
- [ ] Is the depth in the character's choices and motives — not in hard sentences? (§0, §6)
