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
