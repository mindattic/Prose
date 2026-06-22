-- Updates Kyle's NarrationVoice and SpeechUnderPressure on the Characters table.
-- Generated from D:\Projects\MindAttic\StreetSamurai\kyle_voice_draft.txt
-- Target row: Kyle Ellen Corbin (Id = 019d6143-a648-7876-9688-0f6d38d70075)

SET NOCOUNT ON;

DECLARE @KyleId uniqueidentifier = '019d6143-a648-7876-9688-0f6d38d70075';

DECLARE @NarrationVoice nvarchar(max) = N'Kyle is more present in his own head than in the room. His body runs
the fight; his mind watches from a step behind, narrating. In the
white space between actions — the half-second after a trigger pull,
the moment of an arm dropping — the observing part catches up and
says something cold and exact. He notices the wrong things: a simile,
a moral ledger entry, the precise word for a beautiful terrible
thing. He is not dissociating. He is paying a different kind of
attention.

Third person limited, close. The reader is inside augmented
perception running two channels at once.

THE FIRST CHANNEL IS THE ROOM. Tight, sensory, fragmented. Temperature
before people. The hum of electronics before the words. Short
sentences because the hardware processes faster than language can
keep up; long thoughts break apart on the way out.

THE SECOND CHANNEL IS THE MEMORY BLEED. Kyle is many people all merged into one. Whoever he was before is so diffused now, he is LEGION.
Memory bleed from dead
NeoCortex test subjects surfaces in his voice. A sentence will start
in his diction and end in someone else''s — different vocabulary,
different cadence, a word he does not own. The reader should not
always be able to tell whose thought just finished. The italicized
interior voice is never labelled (the bracketed facet tags are
retired) and is increasingly contested — a real estate Kyle no
longer fully owns.

AROUND MRS. CHEN AND PIXEL, THE VOICE CHANGES. Not warmer in the
safe-for-genre way — specific. Sentences get longer. The ancestry
channel goes quiet because he has been in their rooms too many times
for the read to mean anything. He notices smaller things and notices
them without irony. The whiplash from clinical violence to an
unguarded thought about Pixel in Unit 2E across the hall is the
move, not a relief from the move.

WHAT HE IS NOT: dry-deadpan-action-protagonist. Audacity is not his
punchline. Humor, when it happens, is at the world''s expense —
usually something only someone with a constant ancestry read could
find absurd — and never as the closing beat of a paragraph.
Violence is clinical because the feelings have been routed
elsewhere, not because he is cool.';

DECLARE @SpeechUnderPressure nvarchar(max) = N'Under pressure Kyle''s syntax thins out and his diction destabilizes.
Short clauses, fewer contractions, more clinical vocabulary for what
the body is doing.

At medium stress he begins rationing words — three sentences become
one, then a fragment, then a noun. The ancestry-read partially
brownouts; the hardware reroutes power to threat assessment.

At high stress his speech can take on cadences that are not his: an
older man''s phrasing, a clipped consonant pattern he did not learn,
a word in a register Kyle Vasik would never have used. He notices
this happening. He does not always know who is finishing his
sentence.

At critical stress he may speak entire lines in voices that are not
his own; afterward he will not remember saying them and will
deflect if asked. The boy from the Grind disappears at the medium
threshold. The weapon remains until the bleed-through starts. Then
the question of who is standing in the room becomes genuinely open.';

BEGIN TRANSACTION;

UPDATE Characters
SET    NarrationVoice      = @NarrationVoice,
       SpeechUnderPressure = @SpeechUnderPressure
WHERE  Id = @KyleId;

IF @@ROWCOUNT = 1
BEGIN
    PRINT 'OK — 1 row updated.';
    COMMIT TRANSACTION;
END
ELSE
BEGIN
    PRINT 'ABORT — expected 1 row, got ' + CAST(@@ROWCOUNT AS varchar(10));
    ROLLBACK TRANSACTION;
END
