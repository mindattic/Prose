-- =============================================================================
-- Insert: .45 ACP (Automatic Colt Pistol)
-- =============================================================================
-- Adds the historic 200-year-old cartridge as a first-class ammunition entity.
-- Existing canon already has its modern descendant (.45 Auto Composite, slug
-- 45_auto_composite); .45 ACP fits in alongside as the legacy / collector /
-- traditionalist load that platforms like the S&W Governor 2211 Bicentennial
-- Edition still chamber by name.
--
-- Idempotent: pre-flight guards on the unique (EntityType='ammunition', Slug)
-- index. Re-running is a no-op.
--
-- Side effect: the Governor's WeaponAmmunitionType row that currently points
-- at .45 Auto Composite (resolved by fix_weapon_sw_governor_2211_fks.sql) is
-- re-pointed at the new .45 ACP entity, since the file's known caliber was
-- literally ".45 ACP" — no longer a NULL fallback case.
-- =============================================================================

SET XACT_ABORT ON;
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

BEGIN TRAN;

IF EXISTS (SELECT 1 FROM dbo.Entities
           WHERE EntityType = 'ammunition' AND Slug = '45_acp')
BEGIN
    PRINT 'Skipping insert: ammunition with slug "45_acp" already exists.';
END
ELSE
BEGIN
    DECLARE @Id  UNIQUEIDENTIFIER = NEWID();
    DECLARE @Now DATETIME2(7)     = SYSUTCDATETIME();

    DECLARE @Json NVARCHAR(MAX) = N'{
  "id": "REPLACED_BY_DB",
  "name": ".45 ACP",
  "type": "ammunition",
  "aliases": [".45 Automatic Colt Pistol", ".45 Auto"],
  "category": "ballistic",
  "caliber": ".45",
  "propulsion": "gunpowder",
  "description": "Two centuries old and still in production. The .45 Automatic Colt Pistol cartridge predates the GLMZ, the corponations, the BCI revolution, and most of the cities the round has been fired in. By 2211 it survives largely as a heritage cartridge — the load you buy when your sidearm was designed before the Lake Michigan Reclamation Accords made lead-core projectiles a regulatory headache. It still uses a lead-core full-metal-jacket projectile, which is why it''s priced higher than its modern descendant .45 Auto Composite and why most jurisdictions require a Tier 2 collector / heritage license to carry more than a single magazine of it on the street. What you''re paying for is fidelity. The pressure curve, the recoil impulse, the report — same as the round was when the original 2011 Smith & Wesson Governor was rolling off the line. Gunsmiths can chamber a Bicentennial Edition for either .45 ACP or .45 Auto Composite without modification; collectors who can afford it choose ACP because the gun was designed around it.",
  "compatibility_note": "Functionally identical chambering to .45 Auto Composite — any .45 ACP-marked weapon will accept either, though Composite produces lower barrel wear and better barrier performance. Used most commonly in heritage-class platforms: original Governors, Halverson 1A service pistols, the entire Drevko D-45 ''Vintage'' line, and any pre-2150 .45-marked sidearm a collector hasn''t had re-bored.",
  "reliability_note": "Lead-core FMJ in brass cases, gunpowder-propelled. Reliable across temperature and fouling, but produces more bore-leading than Composite — heritage shooters are expected to clean after every range session. No electronic dependency; functions in EMP environments. Not loaded by Kovash Munitions; small-batch production by Drevko Vintage Works and a handful of independent reloaders licensed under the Heritage Cartridge Exemption.",
  "tags": ["ammunition", "ballistic", "heritage", "lead_core", "collector", "200_year_legacy"]
}';
    -- Substitute the real Id into the JSON payload so consumers reading
    -- Records.Json round-trip the same Id they'd see in the Entities row.
    SET @Json = REPLACE(@Json, N'REPLACED_BY_DB', LOWER(REPLACE(CAST(@Id AS NVARCHAR(36)), '-', '')));

    -- ── Entities ─────────────────────────────────────────────────────────
    INSERT INTO dbo.Entities
        (Id, EntityType, Name, Slug, Status, Description,
         CreatedAt, ModifiedAt, IsActive, ArchivedAt, InWorldCreatedDate)
    VALUES
        (@Id,
         N'ammunition',
         N'.45 ACP',
         N'45_acp',
         N'canon',
         N'Two centuries old and still in production. The .45 Automatic Colt Pistol cartridge predates the GLMZ. By 2211 it survives as a heritage cartridge — the load you buy when your sidearm was designed before lead-core projectiles became a regulatory headache. Functionally identical chambering to .45 Auto Composite; collectors choose it because the gun was designed around it.',
         @Now, @Now, 1, NULL,
         '2011-01-01');  -- the historical cartridge is original-era
    -- Tags: see EntityTags MERGE below — TagsJson denorm column was dropped 2026-05-08.

    -- ── Records ──────────────────────────────────────────────────────────
    INSERT INTO dbo.Records (EntityId, Json, UpdatedAt) VALUES (@Id, @Json, @Now);

    -- ── Ammunitions (typed columns) ──────────────────────────────────────
    INSERT INTO dbo.Ammunitions
        (Id, Name, Manufacturer, Caliber, Category, Tier, Legality,
         Rating, VoteCount, Description, Specifications, CulturalContext,
         MidjourneyPrompt, Dalle3Prompt)
    VALUES
        (@Id,
         N'.45 ACP',
         N'Drevko Vintage Works',
         N'.45',
         N'ballistic',
         N'Tier 2',
         N'Heritage Cartridge Exemption — Tier 2 collector license required for street carry above one magazine',
         0.0, 0,
         N'Two-century-old cartridge. Lead-core FMJ. Functionally identical chambering to .45 Auto Composite, retained in production for heritage platforms.',
         N'caliber: .45 (.452 in) bullet: 230 gr lead-core FMJ velocity: ~830 fps energy: ~352 ft-lb case: brass propulsion: gunpowder pressure: SAAMI standard',
         N'The cartridge that predates everything. By 2211 it''s a deliberate aesthetic — collectors and traditionalists carrying weapons designed around the original load, paying a premium for fidelity to a 200-year-old pressure curve.',
         N'',
         N'');

    -- ── AmmunitionAliases ────────────────────────────────────────────────
    INSERT INTO dbo.AmmunitionAliases (AmmunitionId, Position, Value) VALUES
        (@Id, 0, N'.45 Automatic Colt Pistol'),
        (@Id, 1, N'.45 Auto');

    -- ── Tags + EntityTags ────────────────────────────────────────────────
    DECLARE @TagNames TABLE (Name NVARCHAR(120));
    INSERT INTO @TagNames (Name) VALUES
        (N'ammunition'),
        (N'ballistic'),
        (N'heritage'),
        (N'lead_core'),
        (N'collector'),
        (N'200_year_legacy');

    MERGE dbo.Tags AS T
    USING @TagNames AS S ON T.Name = S.Name
    WHEN NOT MATCHED THEN INSERT (Name) VALUES (S.Name);

    INSERT INTO dbo.EntityTags (EntityId, TagId)
    SELECT @Id, T.Id
    FROM dbo.Tags T
    INNER JOIN @TagNames N ON T.Name = N.Name
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.EntityTags ET
        WHERE ET.EntityId = @Id AND ET.TagId = T.Id
    );

    PRINT CONCAT(N'Inserted ammunition 45_acp with EntityId ', CAST(@Id AS NVARCHAR(36)));
END;

-- ── Re-point the Governor's WeaponAmmunitionType row at the new .45 ACP ──
DECLARE @AcpId UNIQUEIDENTIFIER = (
    SELECT Id FROM dbo.Entities
    WHERE EntityType = 'ammunition' AND Slug = '45_acp');

DECLARE @WeaponId UNIQUEIDENTIFIER = (
    SELECT Id FROM dbo.Entities
    WHERE EntityType = 'weapon' AND Slug = 'governor-2211');

DECLARE @Repointed INT = 0;
IF @AcpId IS NOT NULL AND @WeaponId IS NOT NULL
BEGIN
    UPDATE dbo.WeaponAmmunitionTypes
    SET    AmmunitionId = @AcpId
    WHERE  WeaponId = @WeaponId
      AND  Alias    = N'.45 ACP';
    SET @Repointed = @@ROWCOUNT;
END;

COMMIT TRAN;

PRINT CONCAT(N'Re-pointed ', @Repointed, N' Governor 2211 WeaponAmmunitionType row(s) at .45 ACP entity.');
GO
