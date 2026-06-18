-- On Call — R3 beat fixes (2026-06-18)
-- Beat 9: revert to simpler Imo/Corvid (my dialogue additions regressed it)
-- Beat 7: Leandro call — give him one moment of genuine human decision
-- Beat 20: remove the explicit "there is also the question" paragraph (too on-the-nose)
--          keep "private partition, never been audited" final line (subtle dread)

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ── Beat 9 — revert to simpler Imo/Corvid (no back-and-forth dialogue) ───────

UPDATE Beats SET
  Text = N'The alternate-week window has been open for forty days. He finds this out by asking, in the places where operators ask. Three operators used the number in the current window. He is the third.

He spends most of a day finding the other two.

The first is a woman named Imo Asante-Cavalcanti who works disruption logistics — she routes cargo around blockages, a job that sounds administrative and is not. She used the number three weeks ago to move a pharmaceutical consignment through a corridor embargoed for eighteen days.

"What''s Sparrow?" Elias asks, when he has gotten to the point.

She considers this.

"A person," she says. "Very careful. Knows the city the way you know a city when you''ve lived in it your whole life and paid close attention. Someone who''s been at this a long time and has built something real."

The second operator is a man named Corvid Osei-Larsson who does network-access work — the operator you hire when a door is a digital one. He used the number five weeks ago for an access route through a sector that had been rezoned.

"What''s Sparrow?" Elias asks.

He doesn''t hesitate.

"A rotating crew," he says. "Two people, maybe three, sharing the handle. You can tell by the response style — there are at least two different modes the text comes back in, and they''re not the same writer. Someone coordinates the intake; someone else does the routing. It''s clean, but it''s seamed."

Elias thanks them both.

He writes both answers down, on opposite pages of the notepad, and looks at them across the gutter.

Then he writes a third answer, in smaller letters at the bottom of the right-hand page: A system. Running since 2189. No person does this for forty years without leaving a face. No crew survives four corridor events without a single documented overlap.

The third is the most complete explanation. That is what worries him about it.',
  UpdatedAt = GETUTCDATE()
WHERE Id = '019ED369-6B31-761A-9398-95DC0AE53C1B';
GO

-- ── Beat 7 — Leandro call: one moment of genuine human decision ──────────────

UPDATE Beats SET
  Text = N'Leandro Bautista-Henriksen calls at 10:00 on the morning of day two, his scheduled check-in about the reclassification paperwork. The call is normal for the first three minutes: he has filed the provisional category assignment, the Arcturus inquiry clock is running, they should have a response before the window closes, no issues with the Zone 9 transfer.

Then, in the same voice, at the same pace, as if it is the next item on a list:

"Are you the Cordon Freight filer? The 2213 disposition?"

A pause.

Elias says yes.

"I thought so." Leandro''s tone doesn''t change — it''s the tone of someone confirming something they already knew, checking a fact against memory. "They know about you. They''ve known for a while." Another beat; Elias can hear him deciding whether to say the next part. "I thought you should know that."

The line is quiet.

Elias says thank you.

"Of course," Leandro says. The kind of of course that closes a door: something owed, delivered, not to be referenced again. The call returns to the manifest clock, the Arcturus window, the rest of the list.

Elias catches the ambient noise behind Leandro''s voice in those last minutes: HVAC cycling, the Pulse thrumline two levels below the floor, the structural creak of a building designed in 2197 and running at 115% occupancy since 2218. He has always heard the Pulse as a background fact of the city — abstract, something other people ride. From up here it is just vibration in the walls.

Elias sits with the phrase after the call ends. They''ve known for a while. Not her. Not him. Not it. Them. Leandro Bautista-Henriksen works in an orbital transit facility and had reached for them as if it were the natural word for whatever Sparrow is — not a conscious choice, just the word that arrived first, and then he had heard himself use it and kept going.

He writes it in the notepad.

Then he writes the word them, with a question mark and a small circle around it, and looks at it for a while.',
  UpdatedAt = GETUTCDATE()
WHERE Id = '019ED368-E891-7D85-807B-DE547773AC19';
GO

-- ── Beat 20 — trim explicit paragraph, keep "private partition" ending ────────

UPDATE Beats SET
  Text = N'He dials the number on a Thursday evening, nine days after he returns. He is on the balcony. He has been on the balcony every evening since he got back — not because the outside is easier now, but because it is different in a way he wants to keep measuring.

The Spine''s cargo runs pulse to the north. The lake spreads east, the same gray line it has always been, carrying the light at this hour in a way that makes it look briefly like it goes somewhere. The air smells like lake water and factory rain and the green-penny note of volt rats in the junction box below, their dry shifting audible from here on quiet evenings. This is a quiet evening.

He had prepared a question. He has been refining the question for nine days: did you know who I was in 2218, or only later? When you packed the third piece in the 2218 format, had you been watching me for eight years, or did you find me in 2226 and reconstruct the format from the Cordon Freight records? He wants to know if he was chosen or discovered. He understands they may be the same thing.

The number rings.

It connects — not to a voice, but to the brief static of an open line, the sound of a space held rather than a space empty. A second, maybe two.

Then it doesn''t.

The line closes cleanly, no drop tone, no record in his call log. Whatever answered left no more trace than the first time he dialed it, and the time before that, and every other time it worked because it was an alternate week and someone needed a loop closed.

He stays on the balcony a while.

He hadn''t asked the first question. He knows, standing here, what kind of silence that was: a satellite three times the distance of the Moon that can read his intake forms and see him on his balcony and track the specific eleven-minute quality of how he closes a loop — and has no mouth. The open line is the closest thing it has to a hand. He thinks about the mass driver barrel aimed at the equatorial sky, four meters across, and the years of careful operations, and Tadesse maintaining equipment for something that has never introduced itself, and Siosaia deciding not to figure it out further, and Druh aligning the transfer documentation on his desk.

The number works, Druh had said. That''s the important part.

A Null Crow settles on the parapet rail ten meters away. It stays for thirty seconds and then it goes, and he doesn''t watch where.

He keeps the number in his neuretics in the partition he set up seven years ago for personal use, the one that has never been audited. He names it after a bird.',
  UpdatedAt = GETUTCDATE()
WHERE Id = '019ED80D-E0D9-7770-B2FD-8DDBFADE5805';
GO

PRINT 'R3 fixes applied.';
GO
