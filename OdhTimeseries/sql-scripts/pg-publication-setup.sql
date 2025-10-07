-- PostgreSQL publication setup for logical replication to Materialize
-- This creates the publication that Materialize will subscribe to

-- Drop existing publication if it exists
DROP PUBLICATION IF EXISTS timeseries_publication;

-- Set REPLICA IDENTITY FULL on all tables (required for Materialize)
ALTER TABLE intimev3.provenance REPLICA IDENTITY FULL;
ALTER TABLE intimev3.sensors REPLICA IDENTITY FULL;
ALTER TABLE intimev3.types REPLICA IDENTITY FULL;
ALTER TABLE intimev3.datasets REPLICA IDENTITY FULL;
ALTER TABLE intimev3.dataset_types REPLICA IDENTITY FULL;
ALTER TABLE intimev3.timeseries REPLICA IDENTITY FULL;

-- Measurement partitions - numeric
ALTER TABLE intimev3.measurements_numeric_2025 REPLICA IDENTITY FULL;
ALTER TABLE intimev3.measurements_numeric_2025_p1 REPLICA IDENTITY FULL;
ALTER TABLE intimev3.measurements_numeric_2025_p2 REPLICA IDENTITY FULL;
ALTER TABLE intimev3.measurements_numeric_2025_p3 REPLICA IDENTITY FULL;

-- Measurement partitions - string
ALTER TABLE intimev3.measurements_string_2025 REPLICA IDENTITY FULL;
ALTER TABLE intimev3.measurements_string_2025_p1 REPLICA IDENTITY FULL;
ALTER TABLE intimev3.measurements_string_2025_p2 REPLICA IDENTITY FULL;
ALTER TABLE intimev3.measurements_string_2025_p3 REPLICA IDENTITY FULL;

-- Measurement partitions - json
ALTER TABLE intimev3.measurements_json_2025 REPLICA IDENTITY FULL;
ALTER TABLE intimev3.measurements_json_2025_p1 REPLICA IDENTITY FULL;
ALTER TABLE intimev3.measurements_json_2025_p2 REPLICA IDENTITY FULL;
ALTER TABLE intimev3.measurements_json_2025_p3 REPLICA IDENTITY FULL;

-- Measurement partitions - geoposition
ALTER TABLE intimev3.measurements_geoposition_2025 REPLICA IDENTITY FULL;
ALTER TABLE intimev3.measurements_geoposition_2025_p1 REPLICA IDENTITY FULL;
ALTER TABLE intimev3.measurements_geoposition_2025_p2 REPLICA IDENTITY FULL;
ALTER TABLE intimev3.measurements_geoposition_2025_p3 REPLICA IDENTITY FULL;

-- Measurement partitions - geoshape
ALTER TABLE intimev3.measurements_geoshape_2025 REPLICA IDENTITY FULL;
ALTER TABLE intimev3.measurements_geoshape_2025_p1 REPLICA IDENTITY FULL;
ALTER TABLE intimev3.measurements_geoshape_2025_p2 REPLICA IDENTITY FULL;
ALTER TABLE intimev3.measurements_geoshape_2025_p3 REPLICA IDENTITY FULL;

-- Measurement partitions - boolean
ALTER TABLE intimev3.measurements_boolean_2025 REPLICA IDENTITY FULL;
ALTER TABLE intimev3.measurements_boolean_2025_p1 REPLICA IDENTITY FULL;
ALTER TABLE intimev3.measurements_boolean_2025_p2 REPLICA IDENTITY FULL;
ALTER TABLE intimev3.measurements_boolean_2025_p3 REPLICA IDENTITY FULL;

-- Create publication for all tables
CREATE PUBLICATION timeseries_publication FOR TABLE
  intimev3.provenance,
  intimev3.sensors,
  intimev3.types,
  intimev3.datasets,
  intimev3.dataset_types,
  intimev3.timeseries,
  intimev3.measurements_numeric_2025,
  intimev3.measurements_numeric_2025_p1,
  intimev3.measurements_numeric_2025_p2,
  intimev3.measurements_numeric_2025_p3,
  intimev3.measurements_string_2025,
  intimev3.measurements_string_2025_p1,
  intimev3.measurements_string_2025_p2,
  intimev3.measurements_string_2025_p3,
  intimev3.measurements_json_2025,
  intimev3.measurements_json_2025_p1,
  intimev3.measurements_json_2025_p2,
  intimev3.measurements_json_2025_p3,
  intimev3.measurements_geoposition_2025,
  intimev3.measurements_geoposition_2025_p1,
  intimev3.measurements_geoposition_2025_p2,
  intimev3.measurements_geoposition_2025_p3,
  intimev3.measurements_geoshape_2025,
  intimev3.measurements_geoshape_2025_p1,
  intimev3.measurements_geoshape_2025_p2,
  intimev3.measurements_geoshape_2025_p3,
  intimev3.measurements_boolean_2025,
  intimev3.measurements_boolean_2025_p1,
  intimev3.measurements_boolean_2025_p2,
  intimev3.measurements_boolean_2025_p3;

-- Verify publication
SELECT pubname, puballtables FROM pg_publication WHERE pubname = 'timeseries_publication';
