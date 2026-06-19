-- Fix Phi → Φ in On Call (Book) beats (numbers 4148–4151)
-- sqlcmd -Q mangles Unicode; this file is UTF-8 BOM so sqlcmd reads it correctly.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

UPDATE Beats
SET
    Text      = REPLACE(Text, N'Phi', N'Φ'),
    UpdatedAt = GETUTCDATE()
WHERE Number IN (4148, 4149, 4150, 4151)
  AND Text LIKE N'%Phi%';

PRINT CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' beat(s) updated.';
GO
