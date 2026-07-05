-- Restore old Houses to active status; Ophiuchus gets Tier='House'
-- 2026-07-04

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- Reactivate Atrax, Cetus, Noctua, Vulcanus
UPDATE Entities SET IsActive = 1, Status = N'active', ModifiedAt = GETUTCDATE()
WHERE Id IN (
    'ED8CFFD0-732C-48A1-B433-E8B9B93F2687',  -- House Atrax
    '5E10AE1F-8A85-461C-B844-F461037058D7',  -- House Cetus
    '5D2AD2EE-8B93-4E8D-B92D-583CA9081438',  -- House Noctua
    'E2FA1827-AF1A-400B-A703-B67230D23CA7'   -- House Vulcanus
);
PRINT N'Restored House Atrax, Cetus, Noctua, Vulcanus (IsActive=1)';

-- Fix Ophiuchus: find by name, set Tier='House' on the Faction row
-- There are two rows named Ophiuchus / House Ophiuchus — target both
UPDATE Factions SET Tier = N'House'
WHERE Id IN (
    SELECT e.Id FROM Entities e
    WHERE e.Name IN (N'House Ophiuchus', N'Ophiuchus')
      AND e.UniverseId = '0197E9C9-0002-7000-8000-000000000002'
);

UPDATE Entities SET IsActive = 1, Status = N'active', ModifiedAt = GETUTCDATE()
WHERE Name IN (N'House Ophiuchus', N'Ophiuchus')
  AND UniverseId = '0197E9C9-0002-7000-8000-000000000002';

PRINT N'Set Ophiuchus / House Ophiuchus Tier=House, IsActive=1';
