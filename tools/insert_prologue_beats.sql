SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
-- Insert 3 Prologue beats for grafted-into-war-019ece49
-- Sort keys -300, -200, -100 place them before the existing beats (100-700)
DECLARE @strandId UNIQUEIDENTIFIER = '019ECE49-0CCD-7F3F-AE5B-D789EFDDE304';
DECLARE @beat1Id UNIQUEIDENTIFIER = NEWID();
DECLARE @beat2Id UNIQUEIDENTIFIER = NEWID();
DECLARE @beat3Id UNIQUEIDENTIFIER = NEWID();
DECLARE @now DATETIME2 = GETUTCDATE();

-- BEAT 1: The Waking (Prologue, sort key -300)
INSERT INTO Beats (
    Id, Slug, Text, TextHash, BeatTitle, Synopsis, StructureRole,
    Act, SceneType, EmotionalTone, PaceHint,
    Stale, WasCorrected, CreatedAt, UpdatedAt, Number,
    IsChapterStart, Kind, Version
)
VALUES (
    @beat1Id,
    NULL,
    N'He came to himself in the manner of all his comings: not at once, but in accretion, one certainty arriving after the next until the ledger of his body was drawn up complete. Two legs. Two arms. A head. Fingers — the right hand had already closed itself around the grip of something he had not yet attended to, and he allowed it, because the body tended to be ahead of the mind in these matters and had not yet proven itself wrong.

The daemon spoke from its position in the dark interior of his skull: *Myrmidon-101. This is you now.*

He received this in the manner he received all such pronouncements: without enthusiasm, without resistance, with the equanimity of a man long accustomed to being handed identities he had not chosen. He rolled his shoulders in the small testing arc that the Aesir technicians logged as an orientation sequence and that he had come to regard as the body''s private greeting to itself — a gesture he could not have suppressed if he had tried, and had long ago stopped trying.

He was standing in a stone corridor. Torchlight moved against the wall to his left. His boots read the floor as grit and cold.

His name was not available to him. It had not been available for some time. He had reached for it on prior occasions and found the shelf empty, and he had stopped reaching. What persisted instead was a particular shape of competence: he knew which end of the thing in his right hand went forward. He knew to check the magazine before he moved. He knew the word for the feeling of cold moving stone and the word for the smell of old ash in a torch bracket, though he could not have told you where he had learned either.

These were the terms.',
    NULL,
    N'The Waking',
    N'M-101 comes to himself in a new Myrmidon shell — the familiar orientation sequence, the missing name, the daemon''s introduction.',
    N'prologue',
    1,
    N'narrative',
    N'solemn',
    N'slow',
    0,
    0,
    @now,
    @now,
    4063,
    1,
    N'prose',
    1
);

-- BEAT 2: The MP18 (Prologue, sort key -200)
INSERT INTO Beats (
    Id, Slug, Text, TextHash, BeatTitle, Synopsis, StructureRole,
    Act, SceneType, EmotionalTone, PaceHint,
    Stale, WasCorrected, CreatedAt, UpdatedAt, Number,
    IsChapterStart, Kind, Version
)
VALUES (
    @beat2Id,
    NULL,
    N'The weapon in his right hand was a Submachine Gun, Pattern 18 — this much the data plate on the receiver confirmed in the quartermaster''s hand:

```
DESIGNATION:  Submachine Gun, Pattern 18
SCRY ORIGIN:  Sphere 31 / Deutschland
              Maschinenpistole 18/I — Theodor Bergmann, Suhl
              Sphere date: 1918 CE
ALLOY SUB:    Local alloy, barrel jacket. Within spec.
```

*Sphere 31.* He turned the weapon over in his hands, feeling the balance of it, the particular weight of the snail-drum magazine against his palm. Someone in another world had designed this for a war he had never heard of. The Aesir had looked through the glass between spheres and copied it down. The wood was local birch. The rest had come from somewhere else entirely.

He seated the drum until it clicked and checked the action. The mechanism was clean. The daemon logged it as serviceable. He slung it.',
    NULL,
    N'The MP18',
    N'Soren examines the weapon, notes the Scry Origin data plate — Sphere 31, Deutschland — and moves on.',
    N'prologue',
    1,
    N'narrative',
    N'contemplative',
    N'measured',
    0,
    0,
    @now,
    @now,
    4064,
    0,
    N'prose',
    1
);

-- BEAT 3: K-13 (Prologue, sort key -100)
INSERT INTO Beats (
    Id, Slug, Text, TextHash, BeatTitle, Synopsis, StructureRole,
    Act, SceneType, EmotionalTone, PaceHint,
    Stale, WasCorrected, CreatedAt, UpdatedAt, Number,
    IsChapterStart, Kind, Version
)
VALUES (
    @beat3Id,
    NULL,
    N'The Marconi box was already on his hip — he found it by touch, without searching, with the ease of a man organized by other people who has learned to trust their arrangements. He unclipped it and pressed the receiver to the housing near his skull.

A voice: unhurried, precisely economic, carrying within it the implication of many things the speaker had decided not to say.

"M-101. Standing orders from K-13 are active. Embedded coordinates to follow. Rendezvous and proceed under her direction. She''s in the field. Over."

*K-13.*

The codename registered in the manner of all his nameless knowledge: without provenance, without arrival story, simply present the way a word is present in the mouth before the mind knows it has been summoned. He did not know how he knew it. The daemon had no record of a prior meeting. What he had instead was a conviction, assembled from materials whose origin he could not source, that the voice behind K-13 was worth moving toward.

He checked the drum once more. It was fine.

"M-101," he said into the receiver. "Copy. Over."

He moved out. The corridor was cold and long, and he had somewhere to be.',
    NULL,
    N'K-13',
    N'The Marconi box delivers standing orders from K-13; M-101 recognizes the codename without knowing why, and moves out.',
    N'prologue',
    1,
    N'narrative',
    N'purposeful',
    N'brisk',
    0,
    0,
    @now,
    @now,
    4065,
    0,
    N'prose',
    1
);

-- Link beats to strand
INSERT INTO StrandBeats (StrandId, BeatId, SortKey, IsEnabled)
VALUES
    (@strandId, @beat1Id, -300.0, 1),
    (@strandId, @beat2Id, -200.0, 1),
    (@strandId, @beat3Id, -100.0, 1);

-- Fix the strand synopsis to say "Soren" not "Jon Murphy"
UPDATE Strands
SET Synopsis = N'Soren survives the infusion, grafts to an airship turret, witnesses a conscience-killing, falls when the airship is hit, and wakes as Myrmidon-009 with orders to find Dame Lyra of House Ophiuchus.'
WHERE Id = @strandId;

SELECT 'Prologue beats inserted.' AS Result;
