-- =============================================================================
-- Fix unresolved foreign keys on the Smith & Wesson Governor 2211 row.
-- =============================================================================
-- Source-of-truth queries that justify the targets below:
--   SELECT Id, Name, Slug FROM Entities
--    WHERE EntityType='character' AND Slug='kyle_ellen_corbin_vister';
--   → 019D6143-A648-7876-9688-0F6D38D70075  Kyle Ellen Corbin
--
--   SELECT Id, Name, Slug FROM Entities
--    WHERE EntityType='ammunition' AND Slug='45_auto_composite';
--   → B2E4D6F8-A1C3-0578-9123-4567BCDEF012  .45 Auto Composite
--
-- The Alias columns are NOT touched — they preserve the file's surface forms
-- ("Kyle Ellen Corbin-Reese", ".45 ACP") per the schema rule documented on
-- WeaponKnownUser/WeaponAmmunitionType: "alias preserved so display works
-- even when canon is missing." A follow-up canonicalization pass can rewrite
-- aliases to match canon, but that's a separate decision.
--
-- The two surname/caliber discrepancies are FILE vs CANON divergences:
--   Reese  (file)  vs  Vister  (DB / memory rule)
--   .45 ACP (file) vs  .45 Auto Composite (DB / GLMZ canon)
-- The author's intent reads through canon — these were typos / real-world
-- references that landed in the JSON and need a round of canonicalization.
-- =============================================================================

SET XACT_ABORT ON;
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

BEGIN TRAN;

DECLARE @WeaponId UNIQUEIDENTIFIER = (
    SELECT Id FROM dbo.Entities
    WHERE EntityType = 'weapon' AND Slug = 'governor-2211');

IF @WeaponId IS NULL
BEGIN
    PRINT 'No weapon with slug "governor-2211" found — run insert_weapon_sw_governor_2211.sql first.';
    ROLLBACK TRAN;
    RETURN;
END;

DECLARE @KyleId UNIQUEIDENTIFIER = (
    SELECT Id FROM dbo.Entities
    WHERE EntityType = 'character' AND Slug = 'kyle_ellen_corbin_vister');

DECLARE @AcpId UNIQUEIDENTIFIER = (
    SELECT Id FROM dbo.Entities
    WHERE EntityType = 'ammunition' AND Slug = '45_auto_composite');

DECLARE @KyleFixed INT = 0, @AmmoFixed INT = 0;

UPDATE dbo.WeaponKnownUsers
SET    CharacterId = @KyleId
WHERE  WeaponId = @WeaponId
  AND  CharacterId IS NULL
  AND  @KyleId IS NOT NULL;
SET @KyleFixed = @@ROWCOUNT;

UPDATE dbo.WeaponAmmunitionTypes
SET    AmmunitionId = @AcpId
WHERE  WeaponId = @WeaponId
  AND  Alias    = N'.45 ACP'
  AND  AmmunitionId IS NULL
  AND  @AcpId IS NOT NULL;
SET @AmmoFixed = @@ROWCOUNT;

COMMIT TRAN;

PRINT CONCAT(N'Resolved CharacterId on ', @KyleFixed, N' WeaponKnownUsers row(s).');
PRINT CONCAT(N'Resolved AmmunitionId on ', @AmmoFixed, N' WeaponAmmunitionTypes row(s).');
GO
