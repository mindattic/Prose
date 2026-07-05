SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
UPDATE Entities SET IsActive = 0, Status = N'retired', ModifiedAt = GETUTCDATE()
WHERE Id = 'EF8F7157-824E-41CE-BD97-FDBF9BE57E69';
PRINT N'Retired old The Corvin Station (House Corvus era)';
