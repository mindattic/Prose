-- On Call (Book) — R2 fixes (2026-06-18)
-- Beat 3 (4150): Sparrow log A3 was too sparse — add orbital life around call event,
--                connect 2226.1-3 lot to recovery chain, mark log as first contact
-- Beat 4 (4151): trim Papyrus analogy by ~40%, cut "They watch/circles They" restatement
-- Beat 5 (NEW):  closing Sparrow log — same exchange from her POV, "we watch", she continues orbit

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ── Beat 3: Sparrow log A3 — full orbital context around the call ─────────────

UPDATE Beats SET
    Text = N'SPARROW / ORBITAL LOG 2226-04-09
Altitude: ~740,000 mi / Day 2 of current orbit

The contact window for Meridian 88 opens at 14:12 UTC.

She notes:

Zone 4, GLMZ: the recovery chain filed the 2226.1–3 lot as recovered and disposed on 2226-02-20. Weight reported: 2.1 tonnes, three pieces, consistent with manifest. The recovery agent designation on the filing is SPARROW/ACTIVE. The disbursement cleared on the fifteenth of March: 3,400 Φ, on schedule, to the same escrow it has cleared to since 2158. These are correct.

MERIDIAN-7: another deviation, twenty-two meters, filed. Third deviation this month. She has filed all three. The pattern suggests a subsurface drift along the Z4 eastern corridor; she notes it for the April survey cycle.

A shipment from the Tunis Pulse corridor is running fourteen minutes behind its manifest window. The delay is within tolerance. She notes it anyway.

The color of Lake Michigan at 14:58 UTC: green-gray, index 4.7. Slightly below the February reading. She records both values and waits for more data before noting a trend.

At 15:03 UTC, a GLMZ residential exchange calls her number.
At 15:04 UTC, the same exchange calls again.
At 15:09 UTC, the exchange calls a third time.

The third caller asks about a maintenance account.

She does not answer.

She notes: the caller''s exchange is consistent with high-floor residential, Spine district, GLMZ. The call originated in zone coordinates placing it in the northern ferrocement corridor. The number is not published. It passes between operators in the Zone 4 recovery chain on an informal basis. Siosaia Tuivaga-Brennan, Cordon Freight, used it once in 2224 for a Zone 4 routing discrepancy. The lot 2226.1–3 recovery was filed by a Cordon Freight operator. She does not know which one.

She notes this.

The contact window closes at 16:29 UTC. She continues her orbit.

Next window: 14 days.
Note: first contact from the recovery chain.',
    UpdatedAt = GETUTCDATE()
WHERE Number = 4150;
GO

-- ── Beat 4: trim Papyrus, cut the "They watch / circles They" restatement ─────

UPDATE Beats SET
    Text = N'Eight months. He has been on this for eight months.

He knows what the office thinks. He knows because Siosaia told him, in the direct way she delivers information she has decided to stop filtering: Elias, they think you have a problem with this file. They is the team lead and two of the senior operators who process Zone 4 alongside him. They think you keep opening it because you can''t let a gap go. A beat. I defended you. Another beat. I think they might be right.

He has a wall in his apartment — not literally a wall, because he does not print things, but the equivalent in his work files: a directory called EQUATORIAL_GROUNDSTATION_PARTNERS with forty-seven subdirectories, each a chain of shell companies he has traced by hand through corporate registry filings, bankruptcy records, and asset transfer documents going back to 2172. He has found the origin account for the maintenance disbursements. It is a holding company incorporated in 2086, decades before the Concordance, predating the GLMZ financial registry itself. He cannot find who owns it. He cannot find anyone who has ever owned it.

The 3,400 Φ clears on the fifteenth of every month. It has cleared for 808 consecutive months. It will clear again in six days.

He told Siosaia about the 808 months at the coffee machine last Tuesday. She looked at him for a moment. She said: Elias. She said it the way a doctor says a name. She said: this is the Papyrus thing.

He didn''t know what it was. She explained: a man, years ago, couldn''t stop being right about a font choice in a movie credit that nobody else cared about. He recorded a video. It achieved a strange immortality. The font was Papyrus. The problem was that he couldn''t stop.

You''re right about the escrow, Siosaia said. That''s the part that''s like the Papyrus thing.

He has thought about this. He does not think she is wrong. He also cannot close the file.

---

Today she gave him a number.

She did it at the end of the day, when the office was quieting, when she could have done it loudly and didn''t. She walked to his desk, put a small piece of folded paper next to his keyboard, and said: This is a number for a network operator who handles recovery corridor logistics. They''re called Sparrow. The number works on alternate weeks. A pause. I used it once, two years ago, for a job in Zone 4. The routing on the invoice looked off and I wanted a second opinion from someone outside Cordon. She looked at the paper, then at him. They know things about the Zone 4 corridor. I thought you might want to ask.

She went back to her desk. She did not say what she found when she asked. He did not ask her.

He called the number at 15:03. Then at 15:04 because the first call dropped before it connected. Then at 15:09 because the second call produced a long silence he couldn''t tell from dead air, and he hung up and redialed.

The third call connected. He gave the job number and asked about the maintenance account in the remittance routing.

What came back, fifteen seconds later, was a text: a routing analysis, three paragraphs, the shell company chain he had spent eight months building by hand laid out cleanly in 200 words, with three nodes he had not found, and at the bottom a single line — The escrow is current. The facility is maintained. Then: What do you need from the corridor?

He stared at this for a while.

He wrote back: How do you know the facility is maintained?

The response took longer. When it came back, it was two words: We watch.

He sits at his desk with the phone face-up on the blotter. His queue has forty-six items in it. He has not touched it since 15:03.

He writes in the notepad: From where?',
    UpdatedAt = GETUTCDATE()
WHERE Number = 4151;
GO

-- ── Beat 5: new closing Sparrow log — same exchange, her POV ─────────────────

DECLARE @strandId UNIQUEIDENTIFIER = '5B938A91-6EF7-4CF8-A9E5-257A690F4A88';
DECLARE @b5Id     UNIQUEIDENTIFIER = NEWID();
DECLARE @now      DATETIME2        = GETUTCDATE();
DECLARE @base     INT              = (SELECT ISNULL(MAX(Number), 0) FROM Beats);

INSERT INTO Beats (
    Id, Number, Text, Synopsis, Act, SceneType, Kind,
    Stale, WasCorrected, IsChapterStart, Version,
    CreatedAt, UpdatedAt
)
VALUES (
    @b5Id, @base + 1,
    N'SPARROW / ORBITAL LOG 2226-04-09 (ADDENDUM, 15:47 UTC)

At 15:32 UTC, the GLMZ exchange sent a text query: routing analysis, lot 2226.1–3, Zone 4 south corridor, Cordon Freight.

She replied with the analysis. She included three nodes the exchange had not found. She asked what was needed from the corridor.

At 15:41 UTC, the exchange asked: How do you know the facility is maintained?

She replied: We watch.

At 15:47 UTC, the exchange did not respond further.

The contact window closed at 16:29 UTC. She continued her orbit.',
    N'Sparrow logs the same exchange Elias just lived through — from orbit. She is the observer; he is "the GLMZ exchange." She answered, he went quiet, she continued her orbit. His question From where? hangs unanswered.',
    1, N'scene', N'prose', 0, 0, 0, 1, @now, @now
);

INSERT INTO StrandBeats (StrandId, BeatId, SortKey, IsEnabled)
VALUES (@strandId, @b5Id, 5.0, 1);

PRINT N'R2 applied: Beat 3 expanded, Beat 4 trimmed, Beat 5 inserted (number ' + CAST(@base + 1 AS NVARCHAR) + N').';
GO
