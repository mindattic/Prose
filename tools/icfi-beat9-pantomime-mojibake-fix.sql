SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

-- Fix mojibake em-dashes (U+00E2 + U+20AC + U+201D) introduced by icfi-beat9-neuretics-pantomime.sql
UPDATE Beats
SET Text = REPLACE(Text,
    NCHAR(226) + NCHAR(8364) + NCHAR(8221),
    NCHAR(8212)),
    UpdatedAt = GETUTCDATE()
WHERE Id = '019F3EB2-6438-7003-8243-927D5D7D6D8A';

SELECT @@ROWCOUNT AS RowsFixed;
