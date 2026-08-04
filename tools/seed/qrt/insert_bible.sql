SET QUOTED_IDENTIFIER ON;
GO
DELETE FROM NodeBibleSections WHERE NodeId='019FCA42-10A2-7AFF-9AA9-8E796D96B1E0' AND SectionType='BeatSpine';
INSERT INTO NodeBibleSections (Id, NodeId, SectionType, Content, UpdatedAt) VALUES ('72888920-680e-4fe5-8222-148585fa40b3', '019FCA42-10A2-7AFF-9AA9-8E796D96B1E0', 'BeatSpine', N'**Chapter 1 â€” First Overlap** (4 beats)
1. Establish Gordan, the shack, the discipline of his hobby, his solitary routine â€” technical texture, callsign, tower, the ritual of a session.
2. He finds a logbook entry in his own hand for a contact he has no memory of making â€” details too specific to fake (a dog barking in the background, a signal report he''d actually give).
3. More gaps surface. He second-guesses himself â€” fatigue, distraction, encroaching self-doubt about his own reliability as a witness to his own life.
4. Decision: rig a recorder to run every session, to catch himself in the act of not remembering.

**Chapter 2 â€” The Recordings Don''t Match** (5 beats)
5. Priya mentions a contact "last Tuesday" that Gordan has no log for â€” but Priya logged it, timestamped, with specific, correct detail.
6. Gordan checks his recorder from that exact window: dead air. No key clicks, no PTT engaged, radio cold â€” established as physically impossible under normal HF operation.
7. He sends the audio to Owen; his spectral analysis confirms the noise floor is wrong during the "phantom" transmission â€” too clean, like the room isn''t there.
8. Pattern recognition: it only happens on nights he''s monitoring passively â€” listening, not transmitting â€” never when he''s actively worked a contact himself.
9. Closing dread: he can''t prove a negative. The tape''s absence is the only evidence he has, and it points at him.

**Chapter 3 â€” It''s Getting Better** (5 beats)
10. Gordan designs a controlled experiment: watch a session live, in real time, with full attention, specifically to catch the phenomenon in the act.
11. First bait: he deliberately mis-keys his own callsign one digit off during a passive-listen night, then watches, live. Nothing happens while he watches â€” the first hint of the observer-effect, not yet understood.
12. He steps away for a moment â€” and Priya reports being worked by "Gordan," correct callsign, within days. Escalate: a phrase he''s never used on air.
13. The phrase surfaces on someone else''s log within days, on a night he wasn''t watching. Hypothesis forms: it can''t happen on a channel he''s actively watching.
14. Final, larger experiment: he changes his antenna setup mid-session (shifting his true harmonic signature) and deliberately keeps his attention off the waterfall for an evening. The new signature is matched almost immediately â€” live tracking, current, interpolating, not a copy of an old recording. His hypothesis holds for the radio â€” and that success is what dooms him, because it teaches him that watching equals safety.

**Chapter 4 â€” The Turn** (4 beats)
15. Gordan reaches out to Owen and Sal to formally corroborate, treating them as independent instruments the way he''d treat calibrated test equipment.
16. Owen describes a "conversation" with him that included a specific private detail â€” something Gordan has genuinely never said aloud on the air, something he only ever thought, once, sitting at the dead mic.
17. Sal, independently, corroborates a second, different private detail â€” same category: unspoken thought, never transmitted. Two independent witnesses rule out one unreliable narrator.
18. The pivot lands physically: it was never in the signal. Watching the radio only ever proved the radio was safe. There''s no antenna to change, no callsign to mis-key, no channel to watch, for a thing that was never using a channel to begin with.

**Chapter 5 â€” QRT** (4 beats)
19. Gordan goes fully dark â€” kills the rig''s power, boxes the logbooks, fights the habit of subvocalizing his own troubleshooting and mostly fails.
20. Time-skip, told economically through a few concrete images: dust on the tower base, an unheated shack, unopened contest bulletins.
21. A message arrives through a different channel entirely (a QSL card, ham culture''s paper confirmation of a contact) from Priya, thrilled to have "finally worked" Gordan last week â€” warm, specific, unwitting.
22. Closing beat: Gordan''s reaction, understated. He never touched the rig. Final image is small, cold, and procedural â€” no confrontation, no epilogue, no explanation.
', SYSUTCDATETIME());
GO
DELETE FROM NodeBibleSections WHERE NodeId='019FCA42-10A2-7AFF-9AA9-8E796D96B1E0' AND SectionType='ArcSummary';
INSERT INTO NodeBibleSections (Id, NodeId, SectionType, Content, UpdatedAt) VALUES ('af9359a2-002f-4006-b471-bfefd8ee9ddd', '019FCA42-10A2-7AFF-9AA9-8E796D96B1E0', 'ArcSummary', N'QRT is a five-chapter, cold/ambiguous standalone horror piece â€” the flagship book of the HORROR universe. Gordan Rosniak (KJ7ROS), an experienced amateur radio operator, discovers that something has spent months reconstructing his identity from pure pattern â€” his Morse fist, his rig''s exact harmonic signature, his verbal tics, his callsign discipline â€” well enough to be heard as him by other operators on a medium where sound is the only proof of identity that exists.

Five-movement escalation, each raising the stakes of what "proof of identity" means:
1. Written record can lie â€” logbook entries in his own hand for contacts he doesn''t remember.
2. Live testimony can lie â€” another operator hears him while his own recorder shows dead air.
3. The phenomenon tracks him live, not from a snapshot â€” controlled baiting experiments confirm it adapts within days to a changed callsign, a new phrase, a shifted antenna signature.
4. The leak was never the radio â€” independent corroboration from two different hams surfaces private, unspoken thoughts Gordan only ever had at a dead mic.
5. Total withdrawal fails â€” going fully dark for months doesn''t stop it, because it was never dependent on his participation at all.

Ending: cold/ambiguous. Gordan goes dark â€” no radio, no logs. A contact (Priya) reaches out months later, thrilled to have "finally worked" Gordan last week. Gordan never touched the rig.
', SYSUTCDATETIME());
GO
DELETE FROM NodeBibleSections WHERE NodeId='019FCA42-10A2-7AFF-9AA9-8E796D96B1E0' AND SectionType='Characters';
INSERT INTO NodeBibleSections (Id, NodeId, SectionType, Content, UpdatedAt) VALUES ('583b576f-8df2-485e-bca1-39f8c953ea56', '019FCA42-10A2-7AFF-9AA9-8E796D96B1E0', 'Characters', N'**Gordan Rosniak (KJ7ROS) â€” POV, sole narrator throughout.** General-class amateur radio operator, 47, remote IT contractor, lives at the edge of Aldergrove Flats, WA, with his husband Aimes and their two adopted sons (twins, 8). Full profile in his Character record (Psychology/Speech* fields) â€” DCM loads this automatically as the pinned dominant register whenever he is on page (SS-A46). His technical rigor is both what almost saves him and what blinds him: he tests the layer his expertise prepares him to test (the RF chain) and never suspects the one channel he''s never thought to guard â€” his own subvocalized troubleshooting, muttered at a dead mic out of habit, in the one room of a full house that''s still entirely his.

**Aimes Rosniak-Bishop** â€” Gordan''s husband, pediatric nurse. Warm, competent, genuinely doesn''t understand the hobby and has made peace with that. Present as household texture throughout, not a corroborating witness â€” he never hears the phenomenon himself. His function is to make Gordan''s isolation spatial (the shack) rather than domestic, which sharpens the horror: even surrounded by people who love him, Gordan still can''t say the real fear out loud.

**Min-jun and Ji-ho (the boys)** â€” twins, 8, adopted from South Korea as infants. Background presence only; no individual dialogue or POV. Use lightly â€” a cracked bedroom door, bedtime routine â€” to establish why the shack is Gordan''s one private hour, not evidence that he''s alone.

**Priya Standish (G7PRS)** â€” Portsmouth, England. Retired schoolteacher, standing weekly HF schedule with Gordan, four years running. Warm, steady, unhurried. Her corroboration in Ch2 (a contact she logged that Gordan has no memory of) is the first hard evidence. Her line in the closing beat of Ch5 ("finally worked you last week") is the book''s last word â€” deliver it in her established warm, unhurried register, not dramatized.

**Owen Bui (VE7OWB)** â€” Nanaimo, BC. Audio engineer, licensed 6 years, analyzes Gordan''s session recordings professionally. Provides the technical vocabulary that makes the Ch2 tape anomaly concrete and credible (noise floor wrong, "too clean," "like the room isn''t there"). In Ch4 he independently reports a private detail Gordan never transmitted â€” delivered as his own confusion/unease, not as exposition about what it means.

**Sal Ferraro (W2SGF)** â€” Vineland, NJ. Old-timer, 52 years licensed, reflexively skeptical ("I don''t go in for that"). His independent corroboration in Ch4 â€” a different private detail, same category â€” is what rules out "one unreliable witness" and lands the pivot. His reluctance to believe is precisely why his confirmation carries weight; don''t soften his skepticism to make him more sympathetic sooner.

**The Overlap (DB stub only â€” NEVER named or referenced by this label on the page).** No POV scenes, no dialogue that isn''t identical to something Gordan himself would say, no confirmed motive, origin, or mechanism. See Narrative Locks below and `docs/HORROR.md` Â§1/Â§3.
', SYSUTCDATETIME());
GO
DELETE FROM NodeBibleSections WHERE NodeId='019FCA42-10A2-7AFF-9AA9-8E796D96B1E0' AND SectionType='VoiceRegister';
INSERT INTO NodeBibleSections (Id, NodeId, SectionType, Content, UpdatedAt) VALUES ('915d6d9c-1bf7-40d7-a057-76b4ba8dbcac', '019FCA42-10A2-7AFF-9AA9-8E796D96B1E0', 'VoiceRegister', N'Single POV throughout: Gordan Rosniak, third-person limited (close/deep POV), present-tense-adjacent. His Character record (Speech*/Psychology* fields) is the authoritative register per SS-A46 and loads automatically whenever he''s on page â€” this section states the register''s narrative function, not its content (don''t duplicate his Character record''s fields here; read them).

Dread is filtered through procedure. Gordan reaches for a test, a log entry, a signal report before he reaches for a feeling â€” the gap between the technical response and the actual fear is where the horror lives. Under pressure he gets MORE technical, not less; he retreats into diagnostic language the way another narrator might retreat into silence or panic. Q-code shorthand (QSL, QRM, QRT) surfaces in his interior narration, not just his dialogue â€” it''s how he thinks, not just how he transmits.

Craft note per `docs/HORROR.md` Â§1: Gordan may construct a hypothesis (the observation-collapse theory) and narrate it with full technical confidence. The prose must never step outside his POV to confirm or deny that hypothesis as objective fact. When he''s wrong (the hypothesis only ever covered the RF channel, never the real leak), the reader should feel the wrongness land through consequence, not through authorial correction.
', SYSUTCDATETIME());
GO
DELETE FROM NodeBibleSections WHERE NodeId='019FCA42-10A2-7AFF-9AA9-8E796D96B1E0' AND SectionType='NarrativeLocks';
INSERT INTO NodeBibleSections (Id, NodeId, SectionType, Content, UpdatedAt) VALUES ('fa53aff3-9761-4d12-b8e2-981c2a3bb81d', '019FCA42-10A2-7AFF-9AA9-8E796D96B1E0', 'NarrativeLocks', N'- The Overlap is never named, taxonomized, or explained on the page. No scene is ever written from its point of view. It never has dialogue that isn''t identical to something Gordan himself would plausibly say (per `docs/HORROR.md` Â§1).
- Observer-effect lock (originated here, canonical per `docs/HORROR.md` Â§3): the phenomenon cannot surface on a channel Gordan is actively, attentively watching in real time. This is demonstrated mechanically in Chapter 3 (the baiting experiments) and never stated as confirmed objective fact â€” it remains Gordan''s working hypothesis.
- The Chapter 4 turn is a hard lock: the leak was never RF. Two independent corroborations (Owen, then Sal) each surface a private, unspoken thought Gordan only ever had at a dead mic, never transmitted. Do not let a later beat retcon this into "maybe he did say it once" â€” the whole pivot depends on these being genuinely unspoken.
- No invented technology. Amateur radio practice must be accurate throughout: valid callsign formats (US: KJ7ROS; UK: G7PRS; Canada: VE7OWB), correct Q-code usage, real PTT/key-click mechanics, plausible HF propagation behavior. Technical authenticity is the credibility engine for the ambiguity (`docs/HORROR.md` Â§5).
- Gordan survives to the end. No character dies in this book. The horror is epistemic and identity-based, not body-count-based.
- Ending is cold/ambiguous per `docs/HORROR.md` Â§4: no confrontation, no explanation, no epilogue processing what happened into meaning. The last beat is a small, concrete, procedural image, not a dramatized emotional climax.
- Chapter structure is fixed at 5 chapters mapping 1:1 to the brief''s 4 Acts + Ending; do not add, remove, split, or reorder chapters without updating `docs/planning/QRT-brief.md` Â§9 first.
- Gordan is married to Aimes Rosniak-Bishop with two adopted sons (Min-jun, Ji-ho â€” twins, not triplets). His isolation is spatial â€” the shack is the one room that''s his â€” never domestic. Do not revert to an earlier "lives alone" framing; do not give Aimes or the boys the phenomenon''s corroborating role (that stays with Owen and Sal, plus Priya for the written/live-testimony beats).
', SYSUTCDATETIME());
GO

