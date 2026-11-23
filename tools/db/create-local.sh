#!/usr/bin/env bash
# Make a working local Prose database in Docker (RA-T4.4.1.1).
#
# The migration history does not replay on an empty server (an early migration indexes Nodes.NodeCode
# while it is nvarchar(max) — see docs/planning/read-along-plan.md RA-T4.4.1.2), so this builds the
# schema from the CURRENT model instead, then stamps every migration id as applied so the Hub's
# start-up MigrateAsync finds nothing pending, and seeds the GLMZ universe row.
#
#   tools/db/create-local.sh [database] [container] [sa-password]
#   ConnectionStrings__Prose="Server=localhost,1433;Database=<database>;User Id=sa;Password=<pw>;TrustServerCertificate=True;Encrypt=False"
#
# Needs: docker, the .NET SDK and dotnet-ef (dotnet tool install --global dotnet-ef).
set -euo pipefail
DB="${1:-ProseLocal}"
CONTAINER="${2:-prose-sql}"
PW="${3:-Pr0se!Local#2026}"
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT

if ! docker ps --format '{{.Names}}' | grep -qx "$CONTAINER"; then
  if docker ps -a --format '{{.Names}}' | grep -qx "$CONTAINER"; then docker start "$CONTAINER" >/dev/null
  else docker run -d --name "$CONTAINER" -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD="$PW" -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest >/dev/null; fi
fi
SQL=(docker exec "$CONTAINER" /opt/mssql-tools18/bin/sqlcmd -C -I -S localhost -U sa -P "$PW")
for i in $(seq 1 60); do "${SQL[@]}" -Q "SELECT 1" >/dev/null 2>&1 && break; sleep 2; done

echo "[create-local] schema from the model"
( cd "$ROOT/src" && ConnectionStrings__Prose="Server=localhost;Database=x;User Id=x;Password=y;TrustServerCertificate=True" \
  dotnet ef dbcontext script --context ProseDbContext --project Prose.Core --startup-project Prose.Core -o "$WORK/schema.sql" >/dev/null )

echo "[create-local] migration history"
{
  echo "CREATE TABLE [__EFMigrationsHistory] ([MigrationId] nvarchar(150) NOT NULL PRIMARY KEY, [ProductVersion] nvarchar(32) NOT NULL);"
  # The ids EF itself knows (not file names, not a grep: some ids differ from their file, some attributes
  # sit outside a Designer file).
  ( cd "$ROOT/src" && ConnectionStrings__Prose="Server=localhost;Database=x;User Id=x;Password=y;TrustServerCertificate=True" \
    dotnet ef migrations list --no-connect --context ProseDbContext --project Prose.Core --startup-project Prose.Core 2>/dev/null ) |
    grep -E '^[0-9]{14}_' | sed 's/ (Pending)//' |
    while read -r id; do echo "INSERT INTO [__EFMigrationsHistory] VALUES ('$id','10.0');"; done
  echo "INSERT INTO [Universe] (Id, Slug, Name, IsActive, SortKey, CreatedAt) VALUES ('0197e9c9-0001-7000-8000-000000000001','glmz','GLMZ',1,100,SYSUTCDATETIME());"
} > "$WORK/stamp.sql"

docker cp "$WORK/schema.sql" "$CONTAINER:/tmp/prose-schema.sql" >/dev/null
docker cp "$WORK/stamp.sql" "$CONTAINER:/tmp/prose-stamp.sql" >/dev/null
"${SQL[@]}" -Q "IF DB_ID('$DB') IS NOT NULL BEGIN ALTER DATABASE [$DB] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$DB]; END; CREATE DATABASE [$DB];" >/dev/null
"${SQL[@]}" -d "$DB" -i /tmp/prose-schema.sql -b >/dev/null
"${SQL[@]}" -d "$DB" -i /tmp/prose-stamp.sql -b >/dev/null
TABLES=$("${SQL[@]}" -d "$DB" -h -1 -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM sys.tables" | tr -d ' ')
echo "[create-local] $DB ready: $TABLES tables"
echo "ConnectionStrings__Prose=\"Server=localhost,1433;Database=$DB;User Id=sa;Password=$PW;TrustServerCertificate=True;Encrypt=False\""
