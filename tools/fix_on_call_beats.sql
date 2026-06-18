-- On Call (the-number-that-works-019ed367) — Surgical beat rewrites
-- Targeting floor-draggers: 9 (72.5), 13 (69.5), 14 (63.3), 19 (70.0) + structural fix Beat 20
-- Applies reader feedback: secondary character texture, transit psychological weight, noir close
-- 2026-06-18

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ── Beat 9 (72.5) — Imo and Corvid: secondary character texture ─────────────

UPDATE Beats SET
  Text = N'The alternate-week window has been open for forty days. He finds this out by asking, in the places where operators ask. Three operators used the number in the current window. He is the third.

He spends most of a day finding the other two.

The first is a woman named Imo Asante-Cavalcanti who works disruption logistics — she routes cargo around blockages, a job that sounds administrative and is not. She used the number three weeks ago to move a pharmaceutical consignment through a corridor embargoed for eighteen days. She answers his question before he finishes asking it.

"A person," she says. "Very careful. Knows the city the way you know a city when you''ve lived in it your whole life and paid close attention. Someone who''s been at this a long time and has built something real."

"You sound certain."

"The discount," she says. "An AI doesn''t care if the rate is fair. A crew doesn''t care either — they''re running a margin. A person decides what''s fair and charges that."

She looks at him then, in a way that takes his measure without seeming to. "What are you trying to figure out?"

"Whether the number still works," he says.

She lets that go without answering, which he notes.

The second operator is a man named Corvid Osei-Larsson who does network-access work — the operator you hire when a door is a digital one. He used the number five weeks ago for an access route through a sector that had been rezoned. He is already doing something else when Elias arrives, and divides his attention between them in a way that implies the conversation is not complicated.

"What''s Sparrow?"

"A rotating crew," he says, not looking up. "Two people, maybe three, sharing the handle. You can tell by the response style — there are at least two different modes the text comes back in, and they''re not the same writer. Someone coordinates the intake; someone else does the routing. It''s clean, but it''s seamed." He glances over. "Why? You having trouble reaching them?"

"No," Elias says. "The number works."

Corvid shrugs and goes back to what he was doing. The question does not interest him. He has already decided.

Elias thanks them both.

He writes both answers down, on opposite pages of the notepad, and looks at them across the gutter.

Then he writes a third answer, in smaller letters at the bottom of the right-hand page: A system. Running since 2189. No person does this for forty years without leaving a face. No crew survives four corridor events without a single documented overlap.

The third is the most complete explanation. That is what worries him about it.',
  UpdatedAt = GETUTCDATE()
WHERE Id = '019ED369-6B31-761A-9398-95DC0AE53C1B';
GO

-- ── Beat 13 (69.5) — Walk to Pulse station: consequence not inventory ────────

UPDATE Beats SET
  Text = N'The street is wider than he remembers streets being. He has been looking at it from the forty-second floor for forty-four days, which has made it abstract — a line among lines, a feature of the grid. From down here the street is the whole situation.

He picks the route that stays on the wide streets. He has known what route he would take for eleven days. The pavement is older than the buildings above it, stone worn into the specific pattern of how this block gets used, and his footfall enters that pattern and he lets it.

The modded population is denser at street level than it reads from above. A man with forearm modifications that shift his center of gravity slightly forward. A woman whose optical work shows as a faint subdermal bioluminescence when she turns her head under the building shadows. The city at ground level runs itself on visible maintenance rather than invisible management, and he can see all of it: the junction boxes, the service corridors, the scaffolding on a residential tower being extended by three floors by workers in harnesses, the pavement cracked and resealed in three different generations of compound.

He is aware, walking it, that this is what the mass driver looks like from the inside: a working system that does not advertise itself. The maintenance payments have been clearing for thirty years. The facility is intact. The solar circuit has never been switched off. He checked the grid records. The grid has no idea what it is running.

At the corner of Kedzie and 44th, the volt rats. He hears them before he sees them — dry shifting in the housing, small claws on metal. He stops for a moment. He listens. Then he keeps moving.

The Zone 4 Pulse station entrance is eleven blocks from his building. He counts them as he covers them. He finds his pod.

He gets on.',
  UpdatedAt = GETUTCDATE()
WHERE Id = '019ED80B-FED0-7EA1-903D-9914EDF70AD9';
GO

-- ── Beat 14 (63.3) — Pod sealed: psychological weight of enclosure ───────────

UPDATE Beats SET
  Text = N'The pod door seals with the specific sound of a pressure equalization — not loud, just complete. A quality of air changes: the outside air goes away. The inside air is recycled, climate-controlled, neutral. He has not been in a sealed moving container in four years, and his body registers the seal before his mind does.

He breathes out slowly. He has the notepad.

The window side shows the platform sliding away, and then the tunnel wall begins: smooth concrete cycling past at a speed that should feel fast and doesn''t, lit at intervals in the yellow of service lighting and then darkness between. He watches it pass.

He opens the notepad. Not to review — he has reviewed it twenty times. He opens it because the notepad is what he uses to make a moving situation hold still, and the situation is moving.

He reads through it the way you read a manifest for a job you''ve already shipped: looking for anything that isn''t where it should be. The three answers. The credits. The invoice lines. Them, circled. The 2218 audit format. The eleven minutes. On the fourth page, in the margin, a question he wrote at three in the morning two weeks ago: Who routes the maintenance payments? Not Sparrow. Something routes them to Sparrow. Something that can see a ground station on the East African coast and has decided it should keep running.

He closes the notepad.

For the first forty minutes it is tunnel wall, lit at intervals, passing. He counts intervals for a while and stops counting. He is in a tube moving through the earth. The tube knows where it is going. That has to be enough.

On the elevated section north of the city, the pod breaks surface for forty seconds. GLMZ resolves: the Spine''s cargo tubes, the gray line of the lake to the east, the northern residential belt stacked ferrocement against a sky going white with cloud. He looks at it through the window. The pod descends. It is gone.

The Tunis transfer is twenty-three minutes in a connector station with open ceilings and a climate drier than anything he has felt in years. He changes pods. He eats something standing up because he has not eaten since this morning. The continent outside the tunnel wall is no longer the one he left. He knows this. He is still working on registering it.',
  UpdatedAt = GETUTCDATE()
WHERE Id = '019ED80C-423B-7E3D-B099-4DABD3080EDA';
GO

-- ── Beat 19 (70.0) — Return transit: carrying the weight of knowing ──────────

UPDATE Beats SET
  Text = N'The return transit takes the same 90 minutes in the other direction, and it is not the same.

On the outbound he had been managing something: the station, the sealed air, the world at ground level. He had allocated attention to all of it because there was a reason to. The loop had been open.

On the return the loop is closed. He is just on a pod. He eats at the Tunis connector — flatbread, correct and hot — standing at the platform rail, watching the departure board cycle through a list of cities he has never been to and, now that he has been to one, could theoretically go to. He does not take out the notepad. There is nothing in the notepad he needs to add.

He has been outside his apartment for nineteen hours.

He knows what Sparrow is. He is the only living person who knows. The maintenance payments are still routing to an account that moves money through seventy years of shell companies to a ground station that a caretaker named Tadesse maintains under a contract from an entity that, to the best of Tadesse''s knowledge, has never introduced itself. Whatever routes those payments now knows someone used the uplink. Elias Macias, GLMZ, Cordon Freight exit records, ticket for Mombasa Elevated, one bag, booked on the fourth morning. It knows.

He had thought he would feel afraid, knowing this. He doesn''t, specifically. He feels the way he felt when he understood the geometry of the third ascending object: like he has been correct, in private, for long enough that the world catching up is not a relief so much as a confirmation of the method.

The GLMZ pod breaks surface on the elevated section north of the city. There is the lake: the gray line going east, the Spine running north along the lakeshore, and the ferrocement stack of his building visible from the track at the right angle, forty-second floor lit. He watches his building from the outside. He has never had this angle before.

He goes up the elevator. The apartment smells of ferrocement. He sets the bag down.

He goes to the window. He looks at the street.',
  UpdatedAt = GETUTCDATE()
WHERE Id = '019ED80D-7E94-766C-8AD3-B6B734E83ED9';
GO

-- ── Beat 20 (92.5 prose, structural fix) — Ending: noir dread not quiet wonder

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

There is also the question of who is still routing the maintenance payments, and what they do with the information that someone found the uplink. He has thought about this. He does not have a satisfying answer, which means the loop is not fully closed, which means he will be thinking about it until it is.

A Null Crow settles on the parapet rail ten meters away. It stays for thirty seconds and then it goes, and he doesn''t watch where.

He keeps the number in his neuretics in the partition he set up seven years ago for personal use, the one that has never been audited. He names it after a bird.',
  UpdatedAt = GETUTCDATE()
WHERE Id = '019ED80D-E0D9-7770-B2FD-8DDBFADE5805';
GO

PRINT 'Beat rewrites applied.';
GO
