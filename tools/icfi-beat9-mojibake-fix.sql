SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

-- Fix: stored sequence U+00E2 U+20AC U+201D (Windows-1252 interpretation of UTF-8 em-dash E2 80 94)
-- Replace with actual em-dash U+2014 = NCHAR(8212)
UPDATE Beats
SET Text = REPLACE(Text,
    NCHAR(226) + NCHAR(8364) + NCHAR(8221),
    NCHAR(8212)),
    UpdatedAt = GETUTCDATE()
WHERE Id = '019F3EB2-6438-7003-8243-927D5D7D6D8A';

SELECT @@ROWCOUNT AS RowsFixed;
