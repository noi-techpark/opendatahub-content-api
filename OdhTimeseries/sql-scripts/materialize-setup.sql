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

-- Create source from PostgreSQL base tables (no partitioning)
-- Use TEXT COLUMNS for geometry types (PostGIS)
CREATE SOURCE pg_source
  FROM POSTGRES
  CONNECTION pg_connection (
    PUBLICATION 'timeseries_publication',
    TEXT COLUMNS (
      intimev3.measurements_geoposition.value,
      intimev3.measurements_geoshape.value
    )
  )
  FOR TABLES (
    intimev3.provenance AS provenance,
    intimev3.sensors AS sensors,
    intimev3.types AS types,
    intimev3.datasets AS datasets,
    intimev3.dataset_types AS dataset_types,
    intimev3.timeseries AS timeseries,
    intimev3.measurements_numeric AS measurements_numeric,
    intimev3.measurements_string AS measurements_string,
    intimev3.measurements_json AS measurements_json,
    intimev3.measurements_geoposition AS measurements_geoposition,
    intimev3.measurements_geoshape AS measurements_geoshape,
    intimev3.measurements_boolean AS measurements_boolean
  );

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
FROM measurements_numeric m
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
FROM measurements_string m
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
FROM measurements_json m
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
FROM measurements_geoposition m
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
FROM measurements_geoshape m
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
FROM measurements_boolean m
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
