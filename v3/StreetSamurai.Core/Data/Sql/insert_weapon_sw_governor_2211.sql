-- =============================================================================
-- Insert: Smith & Wesson Governor - Bicentennial Edition 2211
-- =============================================================================
-- Net-new canon weapon. Adds:
--   1 row in Entities (universal spine)
--   1 row in Records (canonical JSON safety-net)
--   1 row in Weapons (typed columns)
--   1 row in WeaponKnownUsers (Kyle - alias only, CharacterId resolved if found)
--   3 rows in WeaponAmmunitionTypes (.45 ACP / .45 Colt / .410 bore - aliases,
--     AmmunitionId resolved if found, otherwise NULL with alias preserved)
--   3 rows in WeaponStoryHooks
--  ~15 rows in WeaponSpecs (structured key/value facts)
--   7 rows in EntityTags (with MERGE into Tags for any not yet seen)
--
-- Idempotent: re-running is a no-op once the Entities row exists (guarded by
-- the unique (EntityType, Slug) index).
--
-- Schema notes:
--   - Entity.Id is a guid7 in app code; here we materialize it via NEWID()
--     since T-SQL doesn't have a guid7 generator. The app reads it back fine.
--   - SysStart/SysEnd on Entities/WeaponSpec are GENERATED ALWAYS (system-time
--     period) - we don't write them.
--   - Slug must collision-check against (EntityType, Slug) unique index.
-- =============================================================================

SET XACT_ABORT ON;
SET NOCOUNT ON;
-- Required by the filtered index on Entities (and any computed-column /
-- indexed-view path EF created). sqlcmd defaults to OFF.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

BEGIN TRAN;

-- ── Pre-flight: bail out if this slug already exists for type 'weapon' ────────
IF EXISTS (SELECT 1 FROM dbo.Entities
           WHERE EntityType = 'weapon' AND Slug = 'governor-2211')
BEGIN
    PRINT 'Skipping: weapon with slug "governor-2211" already exists.';
    ROLLBACK TRAN;
    RETURN;
END;

DECLARE @EntityId UNIQUEIDENTIFIER = NEWID();
DECLARE @Now      DATETIME2(7)    = SYSUTCDATETIME();

-- ── Canonical JSON (matches engine/data weaponry shape, story hook removed) ──
DECLARE @Json NVARCHAR(MAX) = N'{
  "entity_type": "weapon",
  "id": "weapon_sw_governor_2211",
  "name": "Smith & Wesson Governor — Bicentennial Edition 2211",
  "slug": "governor-2211",
  "tagline": "Analog soul, Digital edge.",
  "description": "Descendant of the 2011 original, the Governor Bicentennial Edition is a triple-caliber wheel gun — .45ACP / .45 Colt / .410 bore — unchanged by design philosophy. S&W marketed the throwback as a ''200 years of proven stopping power'' collector/carry piece. The CyberEYE system features a miniaturized optical sensor embedded in the top strap, hardwired to the user''s ocular implant, projecting real-time round count, caliber type loaded, and shot placement reticle directly into the shooter''s vision. No external screen — the gun sees what you see. Compatible with low-light/thermal overlays if the user has the right implants. The matte black Scandium frame features bicentennial engravings along the barrel with red tritium cylinder inserts as a nod to the original''s signature look. The S&W medallion on the grip has been replaced with a subtle holographic bicentennial seal.",
  "manufacturer": "Smith & Wesson",
  "manufacture_year": 2211,
  "anniversary_of": 2011,
  "anniversary_milestone": "200_years",
  "category": "revolver",
  "subcategory": "multi-caliber wheelgun",
  "rarity": "limited_edition_collector_carry",
  "legal_status": "licensed_carry_GLMZ",
  "base_stats": {
    "capacity": 6,
    "action": "double_single",
    "barrel_length_inches": 2.75,
    "weight_oz": 29.6,
    "frame_material": "scandium_alloy"
  },
  "compatible_calibers": [".45_ACP", ".45_Colt", ".410_bore_shotshell"],
  "damage_profile": {
    ".45_ACP":   { "0_10m": "LETHAL", "10_25m": "SERIOUS", "25m_plus": "FLESH" },
    ".45_Colt":  { "0_10m": "LETHAL", "10_25m": "LETHAL",  "25m_plus": "SERIOUS" },
    ".410_bore": { "0_5m":  "LETHAL", "5_15m":  "SERIOUS", "15m_plus": "FLESH" }
  },
  "cyberware_integration": {
    "system_name": "CyberEYE Ballistic Overlay",
    "version": "2.4.1",
    "signal_type": "short_range_encrypted_mesh",
    "always_active_on_draw": true,
    "required_implant_tier": 2,
    "hud_elements": {
      "aim_line": { "type": "linear_trace", "color": "amber", "description": "Single projected line from muzzle along bullet path. Real-time wrist tracking." },
      "impact_reticle": { "type": "dynamic_point", "description": "Paints point of impact at aim line terminus. Adjusts continuously with weapon movement." },
      "damage_readout": {
        "type": "one_word_assessment",
        "position": "reticle_adjacent",
        "values": ["FLESH", "SERIOUS", "LETHAL", "NEGLIGIBLE"],
        "value_definitions": {
          "FLESH": "Wound, non-incapacitating",
          "SERIOUS": "High probability of incapacitation",
          "LETHAL": "Estimated kill shot at current range",
          "NEGLIGIBLE": "Hard cover, insufficient penetration"
        },
        "factors": ["caliber_loaded", "optical_depth_range_estimate", "target_biometric_scan_if_available"],
        "known_limitation": "Does not account for target armor. LETHAL read is pre-armor assessment only."
      }
    },
    "vulnerabilities": ["cyberware_jammers", "EMP_pulse", "incompatible_ocular_implants_below_tier_2"],
    "power_source": "integrated_microcell",
    "battery_life_hours": 72,
    "recharge_method": "standard_weapon_cradle"
  },
  "aesthetics": {
    "finish": "matte_black",
    "frame_engravings": "bicentennial_pattern_along_barrel",
    "cylinder_inserts": "red_tritium",
    "grip_medallion": "holographic_bicentennial_seal",
    "notes": "Retro-futurist throwback aesthetic. Two centuries of the same silhouette. Old iron, digital internals."
  },
  "lore": {
    "setting": "GLMZ",
    "region": "Meridian_88_corridor",
    "flavor_text": "Two hundred years of proven stopping power. S&W didn''t change the philosophy — they just gave it eyes.",
    "known_users": ["Kyle_Ellen_Corbin-Reese"],
    "story_hooks": [
      "CyberEYE overlay returns LETHAL on armored target — shot lands, target keeps walking",
      "Jammer in the Glooms kills the overlay mid-confrontation — Kyle shoots analog",
      "Bicentennial engraving recognized by an NPC arms dealer — opens dialogue"
    ]
  },
  "tags": ["kyle_loadout", "multi-caliber", "cyberware_integrated", "collector_piece", "GLMZ_legal", "analog_soul_digital_edge", "bicentennial"]
}';

-- ── Entities (universal spine) ────────────────────────────────────────────────
INSERT INTO dbo.Entities
    (Id, EntityType, Name, Slug, Status, Description,
     CreatedAt, ModifiedAt, IsActive, ArchivedAt, InWorldCreatedDate)
VALUES
    (@EntityId,
     N'weapon',
     N'Smith & Wesson Governor — Bicentennial Edition 2211',
     N'governor-2211',
     N'canon',
     N'Descendant of the 2011 original, the Governor Bicentennial Edition is a triple-caliber wheel gun — .45ACP / .45 Colt / .410 bore — unchanged by design philosophy. S&W marketed the throwback as a ''200 years of proven stopping power'' collector/carry piece. The CyberEYE system features a miniaturized optical sensor embedded in the top strap, hardwired to the user''s ocular implant, projecting real-time round count, caliber type loaded, and shot placement reticle directly into the shooter''s vision. No external screen — the gun sees what you see. Compatible with low-light/thermal overlays if the user has the right implants. The matte black Scandium frame features bicentennial engravings along the barrel with red tritium cylinder inserts as a nod to the original''s signature look. The S&W medallion on the grip has been replaced with a subtle holographic bicentennial seal.',
     @Now, @Now, 1, NULL,
     '2211-01-01');  -- in-world manufacture year
-- Tags: see EntityTags MERGE below — TagsJson denorm column was dropped 2026-05-08.

-- ── Records (canonical JSON safety-net) ───────────────────────────────────────
INSERT INTO dbo.Records (EntityId, Json, UpdatedAt)
VALUES (@EntityId, @Json, @Now);

-- ── Weapons (typed columns) ───────────────────────────────────────────────────
INSERT INTO dbo.Weapons
    (Id, Name, Manufacturer, Category, Tier, Legality,
     Rating, VoteCount, Description, Specifications,
     TacticalUse, CulturalContext, MidjourneyPrompt, Dalle3Prompt)
VALUES
    (@EntityId,
     N'Smith & Wesson Governor — Bicentennial Edition 2211',
     N'Smith & Wesson',
     N'revolver',
     N'',  -- no Tier on source (rarity preserved in Records.Json)
     N'licensed_carry_GLMZ',
     0.0, 0,
     N'Descendant of the 2011 original, the Governor Bicentennial Edition is a triple-caliber wheel gun (.45 ACP / .45 Colt / .410 bore) with an integrated CyberEYE Ballistic Overlay. Matte-black scandium frame, bicentennial engraving along the barrel, red tritium cylinder inserts, holographic bicentennial seal on the grip. Tagline: "Analog soul, Digital edge."',
     N'caliber: .45 ACP / .45 Colt / .410 bore (triple-caliber wheelgun) capacity: 6 action: double/single barrel: 2.75 in weight: 29.6 oz frame: scandium alloy finish: matte black',
     N'',
     N'Two hundred years of proven stopping power. S&W didn''t change the philosophy — they just gave it eyes.',
     N'',
     N'');

-- ── WeaponKnownUsers (Kyle — resolve to CharacterId by slug if it exists) ─────
DECLARE @KyleId UNIQUEIDENTIFIER = (
    SELECT TOP 1 Id FROM dbo.Entities
    WHERE EntityType = 'character'
      AND Slug IN ('kyle-ellen-corbin-reese', 'kyle-ellen-Corbin', 'kyle-corbin-reese', 'kyle-corbin')
    ORDER BY CASE Slug
        WHEN 'kyle-ellen-corbin-reese'  THEN 1
        WHEN 'kyle-ellen-Corbin' THEN 2
        WHEN 'kyle-corbin-reese'        THEN 3
        ELSE 4 END
);

INSERT INTO dbo.WeaponKnownUsers (WeaponId, Position, CharacterId, Alias)
VALUES (@EntityId, 0, @KyleId, N'Kyle Ellen Corbin-Reese');

-- ── WeaponAmmunitionTypes (resolve to AmmunitionId by name match if it exists)
INSERT INTO dbo.WeaponAmmunitionTypes (WeaponId, Position, AmmunitionId, Alias)
SELECT @EntityId, 0,
       (SELECT TOP 1 Id FROM dbo.Entities
        WHERE EntityType = 'ammunition'
          AND (Name LIKE N'%.45%ACP%' OR Slug LIKE N'%45-acp%' OR Slug LIKE N'45-auto%')),
       N'.45 ACP'
UNION ALL
SELECT @EntityId, 1,
       (SELECT TOP 1 Id FROM dbo.Entities
        WHERE EntityType = 'ammunition'
          AND (Name LIKE N'%.45%Colt%' OR Slug LIKE N'%45-colt%' OR Slug LIKE N'%long-colt%')),
       N'.45 Colt'
UNION ALL
SELECT @EntityId, 2,
       (SELECT TOP 1 Id FROM dbo.Entities
        WHERE EntityType = 'ammunition'
          AND (Name LIKE N'%.410%' OR Slug LIKE N'%410-bore%' OR Slug LIKE N'%410%')),
       N'.410 bore shotshell';

-- ── WeaponStoryHooks (the 3 retained hooks, in order) ─────────────────────────
INSERT INTO dbo.WeaponStoryHooks (WeaponId, Position, Hook) VALUES
    (@EntityId, 0, N'CyberEYE overlay returns LETHAL on armored target — shot lands, target keeps walking'),
    (@EntityId, 1, N'Jammer in the Glooms kills the overlay mid-confrontation — Kyle shoots analog'),
    (@EntityId, 2, N'Bicentennial engraving recognized by an NPC arms dealer — opens dialogue');

-- ── WeaponSpecs (structured queryable facts) ──────────────────────────────────
INSERT INTO dbo.WeaponSpecs (WeaponId, SpecKey, SpecValue, Notes) VALUES
    (@EntityId, N'chambering',          N'.45 ACP / .45 Colt / .410 bore shotshell', NULL),
    (@EntityId, N'capacity',             N'6-round cylinder',          NULL),
    (@EntityId, N'action',               N'double/single-action',      NULL),
    (@EntityId, N'barrel_length',        N'2.75 in',                   NULL),
    (@EntityId, N'weight',               N'29.6 oz',                   NULL),
    (@EntityId, N'frame_material',       N'scandium alloy',            NULL),
    (@EntityId, N'analogue',             N'Smith & Wesson Governor (2011 original)', N'200th anniversary edition'),
    (@EntityId, N'manufacture_year',     N'2211',                      NULL),
    (@EntityId, N'anniversary_of',       N'2011',                      NULL),
    (@EntityId, N'anniversary_milestone',N'200 years',                 NULL),
    (@EntityId, N'subcategory',          N'multi-caliber wheelgun',    NULL),
    (@EntityId, N'rarity',               N'limited edition collector/carry', NULL),
    (@EntityId, N'cyberware_system',     N'CyberEYE Ballistic Overlay v2.4.1', NULL),
    (@EntityId, N'cyberware_required_implant_tier', N'2',              N'ocular implant tier 2 minimum'),
    (@EntityId, N'cyberware_battery_life_hours',    N'72',              NULL),
    (@EntityId, N'cyberware_signal_type',           N'short-range encrypted mesh', NULL);

-- ── Tags + EntityTags (MERGE so we don't duplicate Tag rows) ─────────────────
DECLARE @TagNames TABLE (Name NVARCHAR(120));
INSERT INTO @TagNames (Name) VALUES
    (N'kyle_loadout'),
    (N'multi-caliber'),
    (N'cyberware_integrated'),
    (N'collector_piece'),
    (N'GLMZ_legal'),
    (N'analog_soul_digital_edge'),
    (N'bicentennial');

MERGE dbo.Tags AS T
USING @TagNames AS S ON T.Name = S.Name
WHEN NOT MATCHED THEN INSERT (Name) VALUES (S.Name);

INSERT INTO dbo.EntityTags (EntityId, TagId)
SELECT @EntityId, T.Id
FROM dbo.Tags T
INNER JOIN @TagNames N ON T.Name = N.Name
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.EntityTags ET
    WHERE ET.EntityId = @EntityId AND ET.TagId = T.Id
);

COMMIT TRAN;

PRINT CONCAT(N'Inserted weapon governor-2211 with EntityId ', CAST(@EntityId AS NVARCHAR(36)));
GO
