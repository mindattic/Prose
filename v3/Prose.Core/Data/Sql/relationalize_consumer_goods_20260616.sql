-- relationalize_consumer_goods_20260616.sql
-- ───────────────────────────────────────────────────────────────────────────
-- RFC 0007: Make ConsumerGoods fully lossless.
--
-- ConsumerGoods is missing 8 columns present in ConsumerGoodData:
--   BrandName, ProductName, Subcategory, FlavorProfile,
--   Price, PopularityRank, Slogan, CulturalContext
--
-- Existing: Id, Name, Manufacturer, Category, Tier (=TierAvailability),
--           Rating, VoteCount, Description, MidjourneyPrompt, Dalle3Prompt,
--           SysStart, SysEnd, Slug
--
-- Pattern: SYSTEM_VERSIONING OFF → ALTER main + _History → ON
-- Idempotent + partial-state safe: every step guarded by catalog checks.
-- Run under QUOTED_IDENTIFIER ON (sqlcmd -I).
-- ───────────────────────────────────────────────────────────────────────────

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- Turn off system versioning so we can ALTER both tables.
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ConsumerGoods' AND temporal_type = 2)
    ALTER TABLE dbo.ConsumerGoods SET (SYSTEM_VERSIONING = OFF);
GO

-- BrandName
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ConsumerGoods') AND name = 'BrandName')
    ALTER TABLE dbo.ConsumerGoods ADD BrandName NVARCHAR(450) NOT NULL CONSTRAINT DF_ConsumerGoods_BrandName DEFAULT '';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ConsumerGoods_History') AND name = 'BrandName')
    ALTER TABLE dbo.ConsumerGoods_History ADD BrandName NVARCHAR(450) NOT NULL CONSTRAINT DF_CG_H_BrandName DEFAULT '';
GO

-- ProductName
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ConsumerGoods') AND name = 'ProductName')
    ALTER TABLE dbo.ConsumerGoods ADD ProductName NVARCHAR(450) NOT NULL CONSTRAINT DF_ConsumerGoods_ProductName DEFAULT '';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ConsumerGoods_History') AND name = 'ProductName')
    ALTER TABLE dbo.ConsumerGoods_History ADD ProductName NVARCHAR(450) NOT NULL CONSTRAINT DF_CG_H_ProductName DEFAULT '';
GO

-- Subcategory
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ConsumerGoods') AND name = 'Subcategory')
    ALTER TABLE dbo.ConsumerGoods ADD Subcategory NVARCHAR(200) NOT NULL CONSTRAINT DF_ConsumerGoods_Subcategory DEFAULT '';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ConsumerGoods_History') AND name = 'Subcategory')
    ALTER TABLE dbo.ConsumerGoods_History ADD Subcategory NVARCHAR(200) NOT NULL CONSTRAINT DF_CG_H_Subcategory DEFAULT '';
GO

-- FlavorProfile
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ConsumerGoods') AND name = 'FlavorProfile')
    ALTER TABLE dbo.ConsumerGoods ADD FlavorProfile NVARCHAR(MAX) NOT NULL CONSTRAINT DF_ConsumerGoods_FlavorProfile DEFAULT '';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ConsumerGoods_History') AND name = 'FlavorProfile')
    ALTER TABLE dbo.ConsumerGoods_History ADD FlavorProfile NVARCHAR(MAX) NOT NULL CONSTRAINT DF_CG_H_FlavorProfile DEFAULT '';
GO

-- Price
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ConsumerGoods') AND name = 'Price')
    ALTER TABLE dbo.ConsumerGoods ADD Price NVARCHAR(200) NOT NULL CONSTRAINT DF_ConsumerGoods_Price DEFAULT '';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ConsumerGoods_History') AND name = 'Price')
    ALTER TABLE dbo.ConsumerGoods_History ADD Price NVARCHAR(200) NOT NULL CONSTRAINT DF_CG_H_Price DEFAULT '';
GO

-- PopularityRank
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ConsumerGoods') AND name = 'PopularityRank')
    ALTER TABLE dbo.ConsumerGoods ADD PopularityRank INT NOT NULL CONSTRAINT DF_ConsumerGoods_PopularityRank DEFAULT 0;
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ConsumerGoods_History') AND name = 'PopularityRank')
    ALTER TABLE dbo.ConsumerGoods_History ADD PopularityRank INT NOT NULL CONSTRAINT DF_CG_H_PopularityRank DEFAULT 0;
GO

-- Slogan
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ConsumerGoods') AND name = 'Slogan')
    ALTER TABLE dbo.ConsumerGoods ADD Slogan NVARCHAR(MAX) NOT NULL CONSTRAINT DF_ConsumerGoods_Slogan DEFAULT '';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ConsumerGoods_History') AND name = 'Slogan')
    ALTER TABLE dbo.ConsumerGoods_History ADD Slogan NVARCHAR(MAX) NOT NULL CONSTRAINT DF_CG_H_Slogan DEFAULT '';
GO

-- CulturalContext
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ConsumerGoods') AND name = 'CulturalContext')
    ALTER TABLE dbo.ConsumerGoods ADD CulturalContext NVARCHAR(MAX) NOT NULL CONSTRAINT DF_ConsumerGoods_CulturalContext DEFAULT '';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ConsumerGoods_History') AND name = 'CulturalContext')
    ALTER TABLE dbo.ConsumerGoods_History ADD CulturalContext NVARCHAR(MAX) NOT NULL CONSTRAINT DF_CG_H_CulturalContext DEFAULT '';
GO

-- Re-enable system versioning.
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ConsumerGoods' AND temporal_type = 2)
    ALTER TABLE dbo.ConsumerGoods SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.ConsumerGoods_History, DATA_CONSISTENCY_CHECK = OFF));
GO
