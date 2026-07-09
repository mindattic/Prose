SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

-- Beat 21: cut paraphrase before locked Selvamani metaphor
-- Removes: "The echo tells you who it tuned. The heading tells you where they walked. "
UPDATE Beats
SET
    Text = REPLACE(Text,
        'The echo tells you who it tuned. The heading tells you where they walked. The echo is a fingerprint',
        'The echo is a fingerprint'),
    UpdatedAt = GETUTCDATE()
WHERE Id = '019EC177-E509-70C8-9D25-FF54E5361DE9';

-- Beat 29: remove "She drove" opener from final paragraph; keep heater/cold as closing image
UPDATE Beats
SET
    Text = REPLACE(Text,
        'She drove the seam back toward the district office, and the cold',
        'The cold'),
    UpdatedAt = GETUTCDATE()
WHERE Id = '019F2A49-DF2A-78B5-837E-5B738F215441';

-- Beat 30: cut final "She got in and drove it home" sentence; end on "It was still just the car."
UPDATE Beats
SET
    Text = REPLACE(Text,
        CHAR(10) + CHAR(10) + 'She got in and drove it home, the long way, past the seam, because the seam was hers and driving it had never once required a reason beyond that.',
        ''),
    UpdatedAt = GETUTCDATE()
WHERE Id = '019F2A49-FCC9-7924-BF91-BE57EDB0FDD5';
