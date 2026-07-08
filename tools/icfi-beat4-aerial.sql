SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

-- Beat 4: update aerial feed description with leg anatomy visible in the feed
-- Replace the "Tripodal base." description with a clearer anatomical read

UPDATE Beats
SET Text = REPLACE(Text,
    N'ninety feet at the apex per the scale readout. Tripodal base. Moving at a bearing',
    N'ninety feet at the apex per the scale readout. Three legs visible in the feed: two long lateral ones, thin for the bulk they carried, the knee joint bending the wrong way at mid-height like something designed for a different kind of ground; one shorter rear stabilizer. The body sat high on them, a bulbous mass with the forward sensor array at the face. Tripodal. Moving at a bearing'),
    UpdatedAt = GETUTCDATE()
FROM Beats
WHERE Id = '019F3EB2-6438-7587-ACC1-5A8600539152';
