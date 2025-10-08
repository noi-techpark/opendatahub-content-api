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

-- Base measurement tables (no partitioning)
ALTER TABLE intimev3.measurements_numeric REPLICA IDENTITY FULL;
ALTER TABLE intimev3.measurements_string REPLICA IDENTITY FULL;
ALTER TABLE intimev3.measurements_json REPLICA IDENTITY FULL;
ALTER TABLE intimev3.measurements_geoposition REPLICA IDENTITY FULL;
ALTER TABLE intimev3.measurements_geoshape REPLICA IDENTITY FULL;
ALTER TABLE intimev3.measurements_boolean REPLICA IDENTITY FULL;

-- Create publication for all tables
CREATE PUBLICATION timeseries_publication FOR TABLE
  intimev3.provenance,
  intimev3.sensors,
  intimev3.types,
  intimev3.datasets,
  intimev3.dataset_types,
  intimev3.timeseries,
  intimev3.measurements_numeric,
  intimev3.measurements_string,
  intimev3.measurements_json,
  intimev3.measurements_geoposition,
  intimev3.measurements_geoshape,
  intimev3.measurements_boolean;

-- Verify publication
SELECT pubname, puballtables FROM pg_publication WHERE pubname = 'timeseries_publication';
