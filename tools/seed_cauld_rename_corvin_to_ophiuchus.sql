-- Rename House Corvin → House Ophiuchus.
-- Retire the two stale old-Ophiuchus entries that pre-date the Seven Houses redesign.
-- Rename Corvin Station → Ophiuchus Station.
-- 2026-07-04

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- 1. Rename House Corvin to House Ophiuchus (canonical Seven Houses entry)
UPDATE Entities SET
    Name       = N'House Ophiuchus',
    ModifiedAt = GETUTCDATE()
WHERE Id = '019ED86A-2874-765E-99BA-BC83E4F97026';

UPDATE Factions SET
    Slug = N'house_ophiuchus'
WHERE Id = '019ED86A-2874-765E-99BA-BC83E4F97026';
PRINT N'House Corvin renamed to House Ophiuchus.';
GO

-- 2. Retire the two stale old-Ophiuchus entries (pre-Seven-Houses-redesign)
UPDATE Entities SET IsActive = 0, Status = N'retired', ModifiedAt = GETUTCDATE()
WHERE Id IN (
    '019ECE49-C6B2-7D60-A281-E1D5FA18747B',  -- House Ophiuchus (old)
    '019ED86D-B426-7699-9835-87F4399362DE'   -- Ophiuchus (old duplicate)
);
PRINT N'Retired 2 stale Ophiuchus entries.';
GO

-- 3. Rename Corvin Station → Ophiuchus Station
UPDATE Entities SET Name = N'Ophiuchus Station', ModifiedAt = GETUTCDATE()
WHERE Id = '246906E3-D617-4E88-B734-6569AE90B6A7';

UPDATE Places SET
    Name = N'Ophiuchus Station',
    Slug = N'ophiuchus_station'
WHERE Id = '246906E3-D617-4E88-B734-6569AE90B6A7';
PRINT N'Corvin Station renamed to Ophiuchus Station.';
GO

PRINT N'Done.';
