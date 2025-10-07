-- Materialize setup script (fixed for partitioned tables)
-- This script creates the PostgreSQL source, tables, and materialized views for streaming

-- Drop existing objects if they exist
DROP MATERIALIZED VIEW IF EXISTS latest_measurements_all CASCADE;
DROP MATERIALIZED VIEW IF EXISTS latest_measurements_boolean CASCADE;
DROP MATERIALIZED VIEW IF EXISTS latest_measurements_geoshape CASCADE;
DROP MATERIALIZED VIEW IF EXISTS latest_measurements_geoposition CASCADE;
DROP MATERIALIZED VIEW IF EXISTS latest_measurements_json CASCADE;
DROP MATERIALIZED VIEW IF EXISTS latest_measurements_string CASCADE;
DROP MATERIALIZED VIEW IF EXISTS latest_measurements_numeric CASCADE;
DROP SOURCE IF EXISTS pg_source CASCADE;
DROP CONNECTION IF EXISTS pg_connection CASCADE;
DROP SECRET IF EXISTS pg_password CASCADE;

-- Create PostgreSQL connection and source
CREATE SECRET pg_password AS 'password';

CREATE CONNECTION pg_connection TO POSTGRES (
    HOST 'db',
    PORT 5432,
    USER 'bdp',
    PASSWORD SECRET pg_password,
    DATABASE 'timeseries'
);

-- Create source from PostgreSQL with specific partition tables
-- Use TEXT COLUMNS for geometry types (PostGIS)
CREATE SOURCE pg_source
  FROM POSTGRES
  CONNECTION pg_connection (
    PUBLICATION 'timeseries_publication',
    TEXT COLUMNS (
      intimev3.measurements_geoposition_2025.value,
      intimev3.measurements_geoposition_2025_p1.value,
      intimev3.measurements_geoposition_2025_p2.value,
      intimev3.measurements_geoposition_2025_p3.value,
      intimev3.measurements_geoshape_2025.value,
      intimev3.measurements_geoshape_2025_p1.value,
      intimev3.measurements_geoshape_2025_p2.value,
      intimev3.measurements_geoshape_2025_p3.value
    )
  )
  FOR TABLES (
    intimev3.provenance AS provenance,
    intimev3.sensors AS sensors,
    intimev3.types AS types,
    intimev3.datasets AS datasets,
    intimev3.dataset_types AS dataset_types,
    intimev3.timeseries AS timeseries,
    intimev3.measurements_numeric_2025 AS measurements_numeric_p1,
    intimev3.measurements_numeric_2025_p1 AS measurements_numeric_p2,
    intimev3.measurements_numeric_2025_p2 AS measurements_numeric_p3,
    intimev3.measurements_numeric_2025_p3 AS measurements_numeric_p4,
    intimev3.measurements_string_2025 AS measurements_string_p1,
    intimev3.measurements_string_2025_p1 AS measurements_string_p2,
    intimev3.measurements_string_2025_p2 AS measurements_string_p3,
    intimev3.measurements_string_2025_p3 AS measurements_string_p4,
    intimev3.measurements_json_2025 AS measurements_json_p1,
    intimev3.measurements_json_2025_p1 AS measurements_json_p2,
    intimev3.measurements_json_2025_p2 AS measurements_json_p3,
    intimev3.measurements_json_2025_p3 AS measurements_json_p4,
    intimev3.measurements_geoposition_2025 AS measurements_geoposition_p1,
    intimev3.measurements_geoposition_2025_p1 AS measurements_geoposition_p2,
    intimev3.measurements_geoposition_2025_p2 AS measurements_geoposition_p3,
    intimev3.measurements_geoposition_2025_p3 AS measurements_geoposition_p4,
    intimev3.measurements_geoshape_2025 AS measurements_geoshape_p1,
    intimev3.measurements_geoshape_2025_p1 AS measurements_geoshape_p2,
    intimev3.measurements_geoshape_2025_p2 AS measurements_geoshape_p3,
    intimev3.measurements_geoshape_2025_p3 AS measurements_geoshape_p4,
    intimev3.measurements_boolean_2025 AS measurements_boolean_p1,
    intimev3.measurements_boolean_2025_p1 AS measurements_boolean_p2,
    intimev3.measurements_boolean_2025_p2 AS measurements_boolean_p3,
    intimev3.measurements_boolean_2025_p3 AS measurements_boolean_p4
  );

-- Union all numeric measurement partitions
CREATE VIEW measurements_numeric_union AS
SELECT * FROM measurements_numeric_p1
UNION ALL SELECT * FROM measurements_numeric_p2
UNION ALL SELECT * FROM measurements_numeric_p3
UNION ALL SELECT * FROM measurements_numeric_p4;

-- Union all string measurement partitions
CREATE VIEW measurements_string_union AS
SELECT * FROM measurements_string_p1
UNION ALL SELECT * FROM measurements_string_p2
UNION ALL SELECT * FROM measurements_string_p3
UNION ALL SELECT * FROM measurements_string_p4;

-- Union all json measurement partitions
CREATE VIEW measurements_json_union AS
SELECT * FROM measurements_json_p1
UNION ALL SELECT * FROM measurements_json_p2
UNION ALL SELECT * FROM measurements_json_p3
UNION ALL SELECT * FROM measurements_json_p4;

-- Union all geoposition measurement partitions
CREATE VIEW measurements_geoposition_union AS
SELECT * FROM measurements_geoposition_p1
UNION ALL SELECT * FROM measurements_geoposition_p2
UNION ALL SELECT * FROM measurements_geoposition_p3
UNION ALL SELECT * FROM measurements_geoposition_p4;

-- Union all geoshape measurement partitions
CREATE VIEW measurements_geoshape_union AS
SELECT * FROM measurements_geoshape_p1
UNION ALL SELECT * FROM measurements_geoshape_p2
UNION ALL SELECT * FROM measurements_geoshape_p3
UNION ALL SELECT * FROM measurements_geoshape_p4;

-- Union all boolean measurement partitions
CREATE VIEW measurements_boolean_union AS
SELECT * FROM measurements_boolean_p1
UNION ALL SELECT * FROM measurements_boolean_p2
UNION ALL SELECT * FROM measurements_boolean_p3
UNION ALL SELECT * FROM measurements_boolean_p4;

-- Create materialized views for latest measurements per timeseries
-- Latest numeric measurements
CREATE MATERIALIZED VIEW latest_measurements_numeric AS
SELECT DISTINCT ON (m.timeseries_id)
    m.timeseries_id,
    m.timestamp,
    m.value,
    m.provenance_id,
    m.created_on,
    ts.sensor_id,
    ts.type_id,
    s.name as sensor_name,
    t.name as type_name,
    t.data_type,
    t.unit,
    s.metadata as sensor_metadata
FROM measurements_numeric_union m
JOIN timeseries ts ON m.timeseries_id = ts.id
JOIN sensors s ON ts.sensor_id = s.id
JOIN types t ON ts.type_id = t.id
WHERE s.is_active = true
ORDER BY m.timeseries_id, m.timestamp DESC;

-- Latest string measurements
CREATE MATERIALIZED VIEW latest_measurements_string AS
SELECT DISTINCT ON (m.timeseries_id)
    m.timeseries_id,
    m.timestamp,
    m.value,
    m.provenance_id,
    m.created_on,
    ts.sensor_id,
    ts.type_id,
    s.name as sensor_name,
    t.name as type_name,
    t.data_type,
    t.unit,
    s.metadata as sensor_metadata
FROM measurements_string_union m
JOIN timeseries ts ON m.timeseries_id = ts.id
JOIN sensors s ON ts.sensor_id = s.id
JOIN types t ON ts.type_id = t.id
WHERE s.is_active = true
ORDER BY m.timeseries_id, m.timestamp DESC;

-- Latest JSON measurements
CREATE MATERIALIZED VIEW latest_measurements_json AS
SELECT DISTINCT ON (m.timeseries_id)
    m.timeseries_id,
    m.timestamp,
    m.value,
    m.provenance_id,
    m.created_on,
    ts.sensor_id,
    ts.type_id,
    s.name as sensor_name,
    t.name as type_name,
    t.data_type,
    t.unit,
    s.metadata as sensor_metadata
FROM measurements_json_union m
JOIN timeseries ts ON m.timeseries_id = ts.id
JOIN sensors s ON ts.sensor_id = s.id
JOIN types t ON ts.type_id = t.id
WHERE s.is_active = true
ORDER BY m.timeseries_id, m.timestamp DESC;

-- Latest geoposition measurements
CREATE MATERIALIZED VIEW latest_measurements_geoposition AS
SELECT DISTINCT ON (m.timeseries_id)
    m.timeseries_id,
    m.timestamp,
    m.value,
    m.provenance_id,
    m.created_on,
    ts.sensor_id,
    ts.type_id,
    s.name as sensor_name,
    t.name as type_name,
    t.data_type,
    t.unit,
    s.metadata as sensor_metadata
FROM measurements_geoposition_union m
JOIN timeseries ts ON m.timeseries_id = ts.id
JOIN sensors s ON ts.sensor_id = s.id
JOIN types t ON ts.type_id = t.id
WHERE s.is_active = true
ORDER BY m.timeseries_id, m.timestamp DESC;

-- Latest geoshape measurements
CREATE MATERIALIZED VIEW latest_measurements_geoshape AS
SELECT DISTINCT ON (m.timeseries_id)
    m.timeseries_id,
    m.timestamp,
    m.value,
    m.provenance_id,
    m.created_on,
    ts.sensor_id,
    ts.type_id,
    s.name as sensor_name,
    t.name as type_name,
    t.data_type,
    t.unit,
    s.metadata as sensor_metadata
FROM measurements_geoshape_union m
JOIN timeseries ts ON m.timeseries_id = ts.id
JOIN sensors s ON ts.sensor_id = s.id
JOIN types t ON ts.type_id = t.id
WHERE s.is_active = true
ORDER BY m.timeseries_id, m.timestamp DESC;

-- Latest boolean measurements
CREATE MATERIALIZED VIEW latest_measurements_boolean AS
SELECT DISTINCT ON (m.timeseries_id)
    m.timeseries_id,
    m.timestamp,
    m.value,
    m.provenance_id,
    m.created_on,
    ts.sensor_id,
    ts.type_id,
    s.name as sensor_name,
    t.name as type_name,
    t.data_type,
    t.unit,
    s.metadata as sensor_metadata
FROM measurements_boolean_union m
JOIN timeseries ts ON m.timeseries_id = ts.id
JOIN sensors s ON ts.sensor_id = s.id
JOIN types t ON ts.type_id = t.id
WHERE s.is_active = true
ORDER BY m.timeseries_id, m.timestamp DESC;

-- Create unified view combining all latest measurements
CREATE MATERIALIZED VIEW latest_measurements_all AS
SELECT
    timeseries_id,
    timestamp,
    CAST(value AS text) as value,
    provenance_id,
    created_on,
    sensor_id,
    type_id,
    sensor_name,
    type_name,
    data_type,
    unit,
    sensor_metadata
FROM latest_measurements_numeric
UNION ALL
SELECT
    timeseries_id,
    timestamp,
    value,
    provenance_id,
    created_on,
    sensor_id,
    type_id,
    sensor_name,
    type_name,
    data_type,
    unit,
    sensor_metadata
FROM latest_measurements_string
UNION ALL
SELECT
    timeseries_id,
    timestamp,
    CAST(value AS text),
    provenance_id,
    created_on,
    sensor_id,
    type_id,
    sensor_name,
    type_name,
    data_type,
    unit,
    sensor_metadata
FROM latest_measurements_json
UNION ALL
SELECT
    timeseries_id,
    timestamp,
    CAST(value AS text),
    provenance_id,
    created_on,
    sensor_id,
    type_id,
    sensor_name,
    type_name,
    data_type,
    unit,
    sensor_metadata
FROM latest_measurements_geoposition
UNION ALL
SELECT
    timeseries_id,
    timestamp,
    CAST(value AS text),
    provenance_id,
    created_on,
    sensor_id,
    type_id,
    sensor_name,
    type_name,
    data_type,
    unit,
    sensor_metadata
FROM latest_measurements_geoshape
UNION ALL
SELECT
    timeseries_id,
    timestamp,
    CAST(value AS text),
    provenance_id,
    created_on,
    sensor_id,
    type_id,
    sensor_name,
    type_name,
    data_type,
    unit,
    sensor_metadata
FROM latest_measurements_boolean;
