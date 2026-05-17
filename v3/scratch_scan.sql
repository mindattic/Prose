-- Refined scan for true Silence/Chorus power-weapon language hits.
-- Strips "anthropic" (LLM provider name) which contains "strop" as a substring.

SET NOCOUNT ON;

PRINT '--- Records.Json refined hit counts ---';
SELECT 'strop_word'   AS Kw, COUNT(*) AS Hits
  FROM Records
 WHERE (Json LIKE '% strop %' OR Json LIKE '% strop,%' OR Json LIKE '% strop.%' OR Json LIKE '%[''"]strop[''"]%')
UNION ALL
SELECT 'corundum',     COUNT(*) FROM Records WHERE Json LIKE '%corundum%'
UNION ALL
SELECT 'piezo',        COUNT(*) FROM Records WHERE Json LIKE '%piezo%'
UNION ALL
SELECT 'piezoelectric',COUNT(*) FROM Records WHERE Json LIKE '%piezoelectric%'
UNION ALL
SELECT 'glow_blade',   COUNT(*) FROM Records WHERE Json LIKE '%glow%' AND Json LIKE '%Silence%'
UNION ALL
SELECT 'electric_silence', COUNT(*) FROM Records WHERE (Json LIKE '%electric%' OR Json LIKE '%voltage%' OR Json LIKE '%volt %') AND Json LIKE '%Silence%';

PRINT '--- Entities with piezo / corundum / strop-as-word in Records.Json ---';
SELECT e.Name, e.EntityType
  FROM Records r JOIN Entities e ON e.Id = r.EntityId
 WHERE r.Json LIKE '%piezoelectric%'
    OR r.Json LIKE '%corundum%'
    OR r.Json LIKE '%[''"]strop[''"]%'
 ORDER BY e.EntityType, e.Name;

PRINT '--- Documents.Body refined ---';
SELECT 'strop_word' AS Kw, COUNT(*) AS Hits
  FROM Documents
 WHERE (Body LIKE '% strop %' OR Body LIKE '%[''"]strop[''"]%')
UNION ALL
SELECT 'corundum', COUNT(*) FROM Documents WHERE Body LIKE '%corundum%'
UNION ALL
SELECT 'piezo',    COUNT(*) FROM Documents WHERE Body LIKE '%piezo%';
