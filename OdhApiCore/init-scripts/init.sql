-- This script initializes the Postgres database with all necessary extensions and custom functions
-- as required by the OpenDataHub Content API.

-- 1. Create Extensions
-- These extensions are required for geo-spatial queries and text searching.
CREATE EXTENSION IF NOT EXISTS cube;
CREATE EXTENSION IF NOT EXISTS earthdistance;
CREATE EXTENSION IF NOT EXISTS pg_trgm;
CREATE EXTENSION IF NOT EXISTS postgis;

-- 2. Create Custom Functions

-- Converts a text string into a timestamp, handling various date formats.
CREATE OR REPLACE FUNCTION text2ts(text)
 RETURNS timestamp without time zone
 LANGUAGE sql
 IMMUTABLE
AS $function$
  SELECT CASE WHEN $1 ~'^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?(?:Z|\+\d{2}:\d{2})?$' THEN CAST($1 AS timestamp without time zone) END;
$function$;

-- Converts a JSON array of text to a Postgres text array.
CREATE OR REPLACE FUNCTION json_array_to_pg_array(jsonarray jsonb)
 RETURNS text[]
 LANGUAGE plpgsql
 IMMUTABLE STRICT
AS $function$
  begin
    if jsonarray <> 'null' then
      return (select array(select jsonb_array_elements_text(jsonarray)));
    else
      return null;
    end if;
  end;
$function$;

-- Extracts keys from a JSON object array within a JSONB column.
CREATE OR REPLACE FUNCTION extract_keys_from_jsonb_object_array(jsonarray jsonb, key text DEFAULT 'Id'::text)
 RETURNS text[]
 LANGUAGE plpgsql
 IMMUTABLE STRICT
AS $function$
  begin
    if jsonarray <> 'null' then
      return (select array(select data2::jsonb->> key from (select jsonb_array_elements_text(jsonarray) as data2) as subsel));
    else
      return null;
    end if;
  end;
$function$;

-- Extracts 'Source.Id' from nested JSON objects.
CREATE OR REPLACE FUNCTION public.extract_tags(jsonarray jsonb)
 RETURNS text[]
 LANGUAGE plpgsql
 IMMUTABLE strict
AS $function$
  begin
    return (select array(select concat(x.tags->>'Source', '.', x.tags->>'Id') from (select jsonb_path_query(jsonarray, '$.*[*]') tags) x) x);
  end;
$function$;

-- Extracts unique 'Id' keys from a nested JSON array.
CREATE OR REPLACE FUNCTION public.extract_tagkeys(jsonarray jsonb)
 RETURNS text[]
 LANGUAGE plpgsql
 IMMUTABLE strict
AS $function$
  begin
    return (array(select distinct unnest(json_array_to_pg_array(jsonb_path_query_array(jsonarray, '$.*[*].Id')))));
  end;
$function$;

-- Checks if a text string is a valid JSONB object and returns it, otherwise returns null.
CREATE OR REPLACE FUNCTION is_valid_jsonb(p_json text)
 RETURNS JSONB
AS $$
  begin
    return p_json::jsonb;
  exception
    when others then
      return null;
  end;
$$
LANGUAGE plpgsql IMMUTABLE;

-- Converts a JSON array of event dates into a Postgres timestamp array.
CREATE OR REPLACE FUNCTION public.json_2_ts_array(jsonarray jsonb)
 RETURNS timestamp[]
 LANGUAGE plpgsql
 IMMUTABLE STRICT
AS $function$
  begin
    if jsonarray <> 'null' then
      return (
        select
          array(
            select
              (event ->> 'From')::timestamp + (event ->> 'Begin')::time
            from
              jsonb_array_elements(jsonarray) as event
            where
              (event ->> 'From')::timestamp + (event ->> 'Begin')::time < (event ->> 'To')::timestamp + (event ->> 'End')::time
          )
      );
    else
      return null;
    end if;
  end;
$function$;

-- Converts a JSON array of event dates into a Postgres tsrange array.
CREATE OR REPLACE FUNCTION public.json_2_tsrange_array(jsonarray jsonb)
 RETURNS tsrange[]
 LANGUAGE plpgsql
 IMMUTABLE STRICT
AS $function$
  begin
    if jsonarray <> 'null' then
      return (
        select
          array(
            select
              tsrange(
                ( (event ->> 'From')::timestamp + (event ->> 'Begin')::time),
                ( (event ->> 'To')::timestamp + (event ->> 'End')::time)
              )
            from
              jsonb_array_elements(jsonarray) as event
            where
              (event ->> 'From')::timestamp + (event ->> 'Begin')::time < (event ->> 'To')::timestamp + (event ->> 'End')::time
          )
      );
    else
      return null;
    end if;
  end;
$function$;

-- Converts a Postgres tsrange array to a tsmultirange.
CREATE OR REPLACE FUNCTION convert_tsrange_array_to_tsmultirange(tsrange_array tsrange[])
RETURNS tsmultirange
LANGUAGE plpgsql
 IMMUTABLE STRICT
AS $$
  DECLARE
      result tsmultirange := tsmultirange();
      tsr tsrange;
  BEGIN
    IF tsrange_array IS NOT NULL THEN
      FOREACH tsr IN ARRAY tsrange_array
      LOOP
          result := result + tsmultirange(tsrange(tsr));
      END LOOP;
    END IF;
    RETURN result;
  END;
$$;

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

-- Calculates the access role for raw data based on its source and license.
CREATE OR REPLACE FUNCTION public.calculate_access_array_rawdata(source text, license text)
 RETURNS text[]
 LANGUAGE plpgsql
 IMMUTABLE
AS $function$
  begin
    if license = 'closed' then
      return (array['AUTHORIZED']);
    end if;
    return (array['AUTHORIZED','ANONYMOUS']);
  end;
$function$;

-- Calculates the distance from a given timestamp to the nearest event in a tsrange array.
CREATE OR REPLACE FUNCTION public.get_nearest_tsrange_distance(tsrange_array tsrange[], ts_tocalculate timestamp without time zone, sortorder text, prioritizesingledayevents bool)
 RETURNS bigint
 LANGUAGE plpgsql
 IMMUTABLE STRICT
AS $function$
DECLARE
  result bigint;
  intarr bigint[];
  mytsrange tsrange;
  tsr timestamp;
  singledayadd int;
BEGIN
  IF tsrange_array IS NOT NULL THEN
    FOREACH mytsrange IN array tsrange_array
    loop
      singledayadd = 0;
      if(prioritizesingledayevents = true) then
        if(lower(mytsrange)::date = upper(mytsrange)::date) then
          singledayadd = 0;
        else
          singledayadd = 1;
        end if;
      end if;
      if mytsrange @> ts_tocalculate then
        intarr := array_append(intarr, singledayadd);
      else
        if upper(mytsrange)::timestamp > ts_tocalculate then
          intarr := array_append(intarr, extract(epoch from (lower(mytsrange)::timestamp - ts_tocalculate))::bigint + singledayadd);
        end if;
      end if;
    END LOOP;
    if sortorder = 'desc' then
      result = (select unnest(intarr) as x order by x desc limit 1);
    else
      result = (select unnest(intarr) as x order by x asc limit 1);
    end if;
  END IF;
  RETURN result;
END;
$function$;

-- Gets the nearest tsrange to a given timestamp from a tsrange array.
CREATE OR REPLACE FUNCTION public.get_nearest_tsrange(tsrange_array tsrange[], ts_tocalculate timestamp without time zone)
 RETURNS tsrange
 LANGUAGE plpgsql
 IMMUTABLE STRICT
AS $function$
DECLARE
  result tsrange;
  distance bigint;
  distancetemp bigint;
  mytsrange tsrange;
  tsr timestamp;
BEGIN
  IF tsrange_array IS NOT NULL then
    distance = 9999999999;
    FOREACH mytsrange IN array tsrange_array
    loop
      if mytsrange @> ts_tocalculate then
        result = mytsrange;
        distance = 0;
      else
        if upper(mytsrange)::timestamp > ts_tocalculate then
          distancetemp = extract(epoch from (lower(mytsrange)::timestamp - ts_tocalculate))::bigint;
          if(distance > distancetemp) then
            distance = distancetemp;
            result = mytsrange;
          end if;
        end if;
      end if;
    END LOOP;
  END IF;
  RETURN result;
END;
$function$;
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
			WHEN (data #>> '{Latitude}'::text[])::double precision IS NOT NULL
			 AND (data #>> '{Longitude}'::text[])::double precision IS NOT NULL
			THEN ST_SetSRID(ST_MakePoint(
				(data #>> '{Longitude}'::text[])::double precision,
				(data #>> '{Latitude}'::text[])::double precision
			), 4326)::geography
			ELSE NULL
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
