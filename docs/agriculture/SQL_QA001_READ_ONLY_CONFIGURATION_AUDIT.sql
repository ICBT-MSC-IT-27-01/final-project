/*
  QA-001 read-only audit.
  Run only against the authorized AnuradhapuraAI development database.
  This script makes no schema or data changes and deliberately contains no credentials.
*/
SET NOCOUNT ON;
SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

SELECT
    table_schema AS [Schema],
    table_name AS [Table]
FROM information_schema.tables
WHERE table_type = 'BASE TABLE'
  AND (
      table_name LIKE '%Crop%'
      OR table_name LIKE '%Suitability%'
      OR table_name LIKE '%Soil%'
      OR table_name LIKE '%Weight%'
      OR table_name LIKE '%Evidence%'
  )
ORDER BY table_schema, table_name;

SELECT
    table_schema AS [Schema],
    table_name AS [Table],
    column_name AS [Column],
    data_type AS [DataType],
    is_nullable AS [Nullable]
FROM information_schema.columns
WHERE table_name IN (
    SELECT table_name
    FROM information_schema.tables
    WHERE table_type = 'BASE TABLE'
      AND (
          table_name LIKE '%Crop%'
          OR table_name LIKE '%Suitability%'
          OR table_name LIKE '%Soil%'
          OR table_name LIKE '%Weight%'
          OR table_name LIKE '%Evidence%'
      )
)
ORDER BY table_schema, table_name, ordinal_position;

/*
  Use the results above to select the actual configuration tables. Check for:
  - one active record per supported crop and factor;
  - stable crop identifiers matching API values;
  - factor weights and units;
  - temporal scale / aggregation metadata;
  - source URL, publication, applicability and validation metadata;
  - soil compatibility records only where documented.

  Do not insert seasonal or annual rainfall values as seven-day thresholds.
  Do not seed humidity or soil thresholds without traceable applicable evidence.
*/
