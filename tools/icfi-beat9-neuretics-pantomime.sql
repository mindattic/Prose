SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

-- Beat 9: CJ tries neuretic contact; Wes pantomimes (no neuretics / ten fingers);
-- machine lurches when he holds up the fingers; he almost falls.
-- Also fixes voice description: "maybe twelve or thirteen" -> "maybe eleven or twelve"
-- (she guesses older than ten because his voice is roughed by sun; the ten fingers correct her)

UPDATE Beats
SET Text = REPLACE(
    REPLACE(Text,
        N'"Name''s Wes."',
        N'"Name''s Wes."

CJ tapped her temple twice. Standard contact request.

Ninety feet up, a pause. Then the boy tapped his own temple once — and then made a gesture she didn''t immediately parse: a flat hand pulled across the side of his head, quick, like clearing something away. Then he held both arms out from the edge of the Crown, fingers spread.

All ten.

The machine''s stride shifted.

One leg placement wrong — not badly, not a stumble, just a degree or two off the rhythm it had held for fifty miles. The Crown lurched and she watched the small figure drop fast, one knee braced against the dome, both hands grabbing for the edge. Not sliding. But close. Close enough that she stopped breathing for a second.

The machine restabilized. Three strides, normal geometry.

The boy came back up to standing, slow, checking his feet. He looked back down.

Held up the ten fingers again.

Ten. No neuretics. Too young.

She''d heard his voice and thought eleven, twelve. The gesture said ten.

"Okay," she said, and raised her voice against the wind. "Shouting works fine."'),
        N'maybe twelve or thirteen',
        N'maybe eleven or twelve'),
    UpdatedAt = GETUTCDATE()
WHERE Id = '019F3EB2-6438-7003-8243-927D5D7D6D8A';

SELECT @@ROWCOUNT AS RowsFixed;
