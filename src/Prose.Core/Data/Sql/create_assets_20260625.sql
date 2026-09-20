-- create_assets_20260625.sql
-- ─────────────────────────────────────────────────────────────────────────────
-- Assets: binary storage for images and other media attached to strands or
-- the universe at large. Covers, logos, watermarks, banners.
--
-- CoverImagePrompts: structured prompt library per AI image generator.
-- Generators: chatgpt | midjourney | gemini | stable_diffusion | ideogram | flux | firefly
-- NegativePrompt is used by MidJourney (--no ...) and Stable Diffusion.
-- Parameters is a JSON blob for model-specific flags (--ar, --v, CFG scale, etc.).
--
-- Storage note: Data is VARBINARY(MAX) (~2GB ceiling per row). For the book
-- cover use-case (< 5MB per image, dozens of images) this is perfectly sized.
-- StorageUrl is a reserved column for future Azure Blob offload -- once set,
-- the engine will serve from URL instead of streaming the binary.
--
-- Idempotent. Run under QUOTED_IDENTIFIER ON (sqlcmd -I).
-- ─────────────────────────────────────────────────────────────────────────────

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ── Assets ───────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Assets')
BEGIN
    CREATE TABLE dbo.Assets (
        Id            uniqueidentifier NOT NULL DEFAULT NEWSEQUENTIALID(),
        -- cover_image | logo | watermark | banner | thumbnail | promotional
        Type          nvarchar(50)     NOT NULL DEFAULT 'cover_image',
        StrandId      uniqueidentifier NULL,
        UniverseId    uniqueidentifier NULL,
        FileName      nvarchar(500)    NOT NULL,
        ContentType   nvarchar(100)    NOT NULL DEFAULT 'image/png',
        Data          varbinary(max)   NULL,
        StorageUrl    nvarchar(1000)   NULL,
        FileSizeBytes bigint           NOT NULL DEFAULT 0,
        Width         int              NULL,
        Height        int              NULL,
        Notes         nvarchar(1000)   NULL,
        CreatedAt     datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt     datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_Assets PRIMARY KEY (Id),
        CONSTRAINT FK_Assets_Strands
            FOREIGN KEY (StrandId) REFERENCES dbo.Strands(Id),
        CONSTRAINT CHK_Assets_Type
            CHECK (Type IN ('cover_image','logo','watermark','banner','thumbnail','promotional')),
        CONSTRAINT CHK_Assets_HasData
            CHECK (Data IS NOT NULL OR StorageUrl IS NOT NULL)
    );
    CREATE INDEX IX_Assets_StrandId   ON dbo.Assets(StrandId)   WHERE StrandId IS NOT NULL;
    CREATE INDEX IX_Assets_UniverseId ON dbo.Assets(UniverseId) WHERE UniverseId IS NOT NULL;
    CREATE INDEX IX_Assets_Type       ON dbo.Assets(Type);
END
GO

-- ── CoverImagePrompts ────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CoverImagePrompts')
BEGIN
    CREATE TABLE dbo.CoverImagePrompts (
        Id             uniqueidentifier NOT NULL DEFAULT NEWSEQUENTIALID(),
        StrandId       uniqueidentifier NULL,
        -- Set when a prompt was used to generate an asset via API.
        AssetId        uniqueidentifier NULL,
        -- chatgpt | midjourney | gemini | stable_diffusion | ideogram | flux | firefly
        Generator      nvarchar(50)     NOT NULL,
        -- Short author label: e.g. ATTE v1 dark rain, VATD high-contrast mono
        Label          nvarchar(200)    NULL,
        PromptText     nvarchar(max)    NOT NULL,
        -- For MidJourney (--no ...) and Stable Diffusion negative conditioning.
        NegativePrompt nvarchar(max)    NULL,
        -- JSON blob for model-specific parameters.
        -- MidJourney example: ar 2:3, v 6.1, style raw
        -- Stable Diffusion example: steps 30, cfg_scale 7, sampler dpm++_2m
        -- ChatGPT example: size 1024x1536, quality hd
        Parameters     nvarchar(max)    NULL,
        GeneratedAt    datetime2        NULL,
        Notes          nvarchar(500)    NULL,
        CreatedAt      datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt      datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_CoverImagePrompts PRIMARY KEY (Id),
        CONSTRAINT FK_CoverImagePrompts_Strands
            FOREIGN KEY (StrandId) REFERENCES dbo.Strands(Id),
        CONSTRAINT FK_CoverImagePrompts_Assets
            FOREIGN KEY (AssetId) REFERENCES dbo.Assets(Id),
        CONSTRAINT CHK_CoverImagePrompts_Generator
            CHECK (Generator IN ('chatgpt','midjourney','gemini','stable_diffusion','ideogram','flux','firefly'))
    );
    CREATE INDEX IX_CoverImagePrompts_StrandId   ON dbo.CoverImagePrompts(StrandId)   WHERE StrandId IS NOT NULL;
    CREATE INDEX IX_CoverImagePrompts_AssetId    ON dbo.CoverImagePrompts(AssetId)    WHERE AssetId IS NOT NULL;
    CREATE INDEX IX_CoverImagePrompts_Generator  ON dbo.CoverImagePrompts(Generator);
END
GO
