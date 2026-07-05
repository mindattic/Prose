-- Rename House Thresh → House Lacerta.
-- Rename The Thresh Chamber → The Lacerta Chamber.
-- 2026-07-04

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

UPDATE Entities SET Name = N'House Lacerta', ModifiedAt = GETUTCDATE()
WHERE Id = 'E63D3F09-737D-4EB7-B479-1BAA9F2815E1';

UPDATE Factions SET Slug = N'house_lacerta'
WHERE Id = 'E63D3F09-737D-4EB7-B479-1BAA9F2815E1';
PRINT N'House Thresh renamed to House Lacerta.';
GO

UPDATE Entities SET Name = N'The Lacerta Chamber', ModifiedAt = GETUTCDATE()
WHERE Id = '345E7015-FB49-4A1C-BA00-D577DBF6BEDA';

UPDATE Places SET Name = N'The Lacerta Chamber', Slug = N'the_lacerta_chamber'
WHERE Id = '345E7015-FB49-4A1C-BA00-D577DBF6BEDA';
PRINT N'The Thresh Chamber renamed to The Lacerta Chamber.';
GO

PRINT N'Done.';
