SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

-- Fix mojibaked em-dash in ICFI beat 1 and swap for clean construction
UPDATE Beats
SET Text = REPLACE(b.Text,
    N'He went out through the kitchen quiet, past his parents'' door '
        + NCHAR(226) + NCHAR(8364) + NCHAR(8221)
        + N' they were still sleeping, the house hadn''t woken up yet, and this wasn''t the kind of sound you pulled someone out of bed over before you knew what it was. He went out through the screen door and across the yard in his boots, laces only half-done because there wasn''t time to be fussy about it.',
    N'He went out through the kitchen quiet, past his parents'' door. They were still sleeping; the house hadn''t woken up yet, and this wasn''t the kind of sound you pulled someone out of bed over before you knew what it was. He went out through the screen door and across the yard in his boots, laces only half-done because there wasn''t time to be fussy about it.'),
    UpdatedAt = GETUTCDATE()
FROM Beats b
WHERE b.Id = '019F3EB2-6438-7381-901D-FB023A9011FD';
