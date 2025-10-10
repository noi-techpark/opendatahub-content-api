
-- Calculates the access role for a dataset based on its source and closed data status.
CREATE OR REPLACE FUNCTION public.calculate_access_array(source text, closeddata boolean)
RETURNS text[]
 LANGUAGE plpgsql
 IMMUTABLE
AS $function$
  begin
    if source = null or closeddata = null then
      return (array['ANONYMOUS','AUTHORIZED']);
    end if;
    if closeddata then
      return (array['AUTHORIZED']);
    end if;
    return (array['AUTHORIZED','ANONYMOUS']);
  end;
$function$;

-- Create Sensors table following standard pattern
CREATE TABLE IF NOT EXISTS public.sensors (
	id varchar(255) NOT NULL,
	"data" jsonb NULL,
	gen_active bool GENERATED ALWAYS AS ((data #> '{Active}'::text[])::boolean) STORED NULL,
	gen_smgactive bool GENERATED ALWAYS AS ((data #> '{SmgActive}'::text[])::boolean) STORED NULL,
	gen_source text GENERATED ALWAYS AS (data #>> '{_Meta,Source}'::text[]) STORED NULL,
	gen_sensortype text GENERATED ALWAYS AS (data #>> '{SensorType}'::text[]) STORED NULL,
	gen_sensorname text GENERATED ALWAYS AS (data #>> '{SensorName}'::text[]) STORED NULL,
	gen_parentid text GENERATED ALWAYS AS (data #>> '{ParentId}'::text[]) STORED NULL,
	gen_manufacturer text GENERATED ALWAYS AS (data #>> '{Manufacturer}'::text[]) STORED NULL,
	gen_model text GENERATED ALWAYS AS (data #>> '{Model}'::text[]) STORED NULL,
	gen_lastchange timestamp GENERATED ALWAYS AS (text2ts(data #>> '{LastChange}'::text[])) STORED NULL,
	gen_installationdate timestamp GENERATED ALWAYS AS (text2ts(data #>> '{InstallationDate}'::text[])) STORED NULL,
	gen_calibrationdate timestamp GENERATED ALWAYS AS (text2ts(data #>> '{CalibrationDate}'::text[])) STORED NULL,
	gen_latitude double precision GENERATED ALWAYS AS ((data #>> '{Latitude}'::text[])::double precision) STORED NULL,
	gen_longitude double precision GENERATED ALWAYS AS ((data #>> '{Longitude}'::text[])::double precision) STORED NULL,
	gen_altitude double precision GENERATED ALWAYS AS ((data #>> '{Altitude}'::text[])::double precision) STORED NULL,
	gen_languages _text GENERATED ALWAYS AS (json_array_to_pg_array(data #> '{HasLanguage}'::text[])) STORED NULL,
	gen_datasetids _text GENERATED ALWAYS AS (json_array_to_pg_array(data #> '{DatasetIds}'::text[])) STORED NULL,
	gen_measurementtypenames _text GENERATED ALWAYS AS (json_array_to_pg_array(data #> '{MeasurementTypeNames}'::text[])) STORED NULL,
	gen_smgtags _text GENERATED ALWAYS AS (json_array_to_pg_array(data #> '{SmgTags}'::text[])) STORED NULL,
	gen_publishedon _text GENERATED ALWAYS AS (json_array_to_pg_array(data #> '{PublishedOn}'::text[])) STORED NULL,
	gen_access_role text[] GENERATED ALWAYS AS (calculate_access_array(data#>>'{_Meta,Source}',(data#>'{LicenseInfo,ClosedData}')::bool)) STORED NULL,
	gen_position public.geography GENERATED ALWAYS AS (
		CASE
			-- CASE 1: Check for WKT string in 'Geometry' key
			WHEN (data ? 'Geometry') AND ((data #>> '{Geometry}')::text) IS NOT NULL THEN
				-- Attempt to convert WKT string to GEOMETRY, then cast to GEOGRAPHY
				ST_GeomFromText((data #>> '{Geometry}')::text, 4326)::geography

			-- CASE 2: Fallback to Latitude and Longitude keys
			WHEN ((data #>> '{Latitude}'::text[])::double precision) IS NOT NULL AND ((data #>> '{Longitude}'::text[])::double precision) IS NOT NULL THEN
				ST_SetSRID(
					ST_MakePoint(
						(data #>> '{Longitude}'::text[])::double precision,
						(data #>> '{Latitude}'::text[])::double precision
					), 4326
				)::geography

			ELSE NULL::geography
		END
	) STORED NULL,
	CONSTRAINT sensors_pkey PRIMARY KEY (id)
);

-- Create indices for performance (use GIN for most as per EventsTable pattern)
CREATE INDEX IF NOT EXISTS ix_sensors_data_gin ON public.sensors USING gin (data);
CREATE INDEX IF NOT EXISTS ix_sensors_position ON public.sensors USING gist (gen_position);
CREATE INDEX IF NOT EXISTS ix_sensors_active ON public.sensors USING btree (gen_active);
CREATE INDEX IF NOT EXISTS ix_sensors_smgactive ON public.sensors USING btree (gen_smgactive);
CREATE INDEX IF NOT EXISTS ix_sensors_source ON public.sensors USING btree (gen_source);
CREATE INDEX IF NOT EXISTS ix_sensors_sensortype ON public.sensors USING btree (gen_sensortype);
CREATE INDEX IF NOT EXISTS ix_sensors_lastchange ON public.sensors USING btree (gen_lastchange);
CREATE INDEX IF NOT EXISTS ix_sensors_languages ON public.sensors USING gin (gen_languages);
CREATE INDEX IF NOT EXISTS ix_sensors_datasetids ON public.sensors USING gin (gen_datasetids);
CREATE INDEX IF NOT EXISTS ix_sensors_measurementtypenames ON public.sensors USING gin (gen_measurementtypenames);
CREATE INDEX IF NOT EXISTS ix_sensors_smgtags ON public.sensors USING gin (gen_smgtags);
CREATE INDEX IF NOT EXISTS ix_sensors_publishedon ON public.sensors USING gin (gen_publishedon);
CREATE INDEX IF NOT EXISTS ix_sensors_access_role ON public.sensors USING gin (gen_access_role);
