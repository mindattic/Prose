SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

-- Beat 1 (The Wail): fix mojibaked em-dash, add parents-still-sleeping detail
-- Uses NCHAR(8212) for em-dash to avoid sqlcmd encoding issues
UPDATE Beats
SET Text = REPLACE(
    REPLACE(Text,
        'He went out through the kitchen quiet, past his parents'' door ' + NCHAR(8212) + NCHAR(32554) + NCHAR(8260) + NCHAR(10),
        'PLACEHOLDER_MOJI'),
    REPLACE(Text,
        'He went out through the kitchen, the screen door, and across the yard in his boots, laces only half-done because there wasn''t time to be fussy about it.',
        'He went out through the kitchen quiet, past his parents'' door. They were still sleeping; the house hadn''t woken up yet, and this wasn''t the kind of sound you pulled someone out of bed over before you knew what it was. He went out through the screen door and across the yard in his boots, laces only half-done because there wasn''t time to be fussy about it.')),
    UpdatedAt = GETUTCDATE()
WHERE Id = '019F3EB2-6438-7381-901D-FB023A9011FD';
