-- ─────────────────────────────────────────────────────────────────────────
-- grant-managed-identity.sql
--
-- One-shot script that creates contained SQL users for the two AAD
-- principals that need to talk to the StreetSamurai database, and grants
-- each the database roles its workflow needs:
--
--   1. App Service system-assigned managed identity
--      → display name = <APP_SERVICE_NAME>  (default: streetsamurai)
--      → runtime workflow: read + write canon, no schema changes
--      → roles: db_datareader, db_datawriter
--
--   2. GitHub Actions OIDC service principal
--      → display name = <GITHUB_SP_NAME>    (e.g. streetsamurai-github)
--      → CI/CD workflow: run ApplyMigrations on every master push
--      → roles: db_ddladmin, db_datareader, db_datawriter
--
-- Run this AGAINST THE StreetSamurai DATABASE (not master), as the AAD
-- admin you configured in azure-sql.bicep. Example:
--
--   sqlcmd ^
--     -S streetsamurai-sql.database.windows.net ^
--     -d StreetSamurai ^
--     -G ^
--     -i infra/grant-managed-identity.sql ^
--     -v APP_SERVICE_NAME=streetsamurai GITHUB_SP_NAME=streetsamurai-github
--
-- -G uses AAD-interactive auth; you'll get a browser prompt. The two -v
-- variables substitute into the FROM EXTERNAL PROVIDER clauses below.
-- The script is idempotent — re-running is a no-op.
-- ─────────────────────────────────────────────────────────────────────────

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;

-- ── 1. App Service managed identity ──────────────────────────────────────
-- Contained-user creation from an AAD principal. The Azure SQL engine
-- resolves the display name against AAD at execution time, so the App
-- Service must already exist with a SystemAssigned identity (the bicep
-- deploy of the web app does this — `identity: { type: 'SystemAssigned' }`).

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '$(APP_SERVICE_NAME)')
BEGIN
    DECLARE @sqlCreateAppMI NVARCHAR(MAX) =
        N'CREATE USER [$(APP_SERVICE_NAME)] FROM EXTERNAL PROVIDER;';
    EXEC sp_executesql @sqlCreateAppMI;
    PRINT 'Created user: $(APP_SERVICE_NAME) (App Service managed identity).';
END
ELSE
BEGIN
    PRINT 'User already exists: $(APP_SERVICE_NAME). Skipping CREATE.';
END
GO

ALTER ROLE db_datareader ADD MEMBER [$(APP_SERVICE_NAME)];
ALTER ROLE db_datawriter ADD MEMBER [$(APP_SERVICE_NAME)];
PRINT 'Granted db_datareader + db_datawriter to $(APP_SERVICE_NAME).';
GO

-- ── 2. GitHub OIDC service principal ─────────────────────────────────────
-- The CI/CD workflow authenticates via federated identity (no client
-- secret in repo). The SP needs schema-modification rights so the
-- ApplyMigrations step can run T-SQL DDL.

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '$(GITHUB_SP_NAME)')
BEGIN
    DECLARE @sqlCreateGhSp NVARCHAR(MAX) =
        N'CREATE USER [$(GITHUB_SP_NAME)] FROM EXTERNAL PROVIDER;';
    EXEC sp_executesql @sqlCreateGhSp;
    PRINT 'Created user: $(GITHUB_SP_NAME) (GitHub OIDC service principal).';
END
ELSE
BEGIN
    PRINT 'User already exists: $(GITHUB_SP_NAME). Skipping CREATE.';
END
GO

ALTER ROLE db_ddladmin   ADD MEMBER [$(GITHUB_SP_NAME)];
ALTER ROLE db_datareader ADD MEMBER [$(GITHUB_SP_NAME)];
ALTER ROLE db_datawriter ADD MEMBER [$(GITHUB_SP_NAME)];
PRINT 'Granted db_ddladmin + db_datareader + db_datawriter to $(GITHUB_SP_NAME).';
GO

-- ── Verification ─────────────────────────────────────────────────────────
SELECT
    dp.name           AS principal,
    dp.type_desc      AS type,
    STRING_AGG(rp.name, ', ') WITHIN GROUP (ORDER BY rp.name) AS roles
FROM sys.database_principals dp
LEFT JOIN sys.database_role_members drm ON drm.member_principal_id = dp.principal_id
LEFT JOIN sys.database_principals rp    ON rp.principal_id = drm.role_principal_id
WHERE dp.name IN ('$(APP_SERVICE_NAME)', '$(GITHUB_SP_NAME)')
GROUP BY dp.name, dp.type_desc;
GO
