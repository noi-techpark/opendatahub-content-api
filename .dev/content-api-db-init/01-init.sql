-- This script initializes the Postgres database with all necessary extensions and custom functions
-- as required by the OpenDataHub Content API.

-- 1. Create Extensions
-- These extensions are required for geo-spatial queries and text searching.
CREATE EXTENSION IF NOT EXISTS cube;
CREATE EXTENSION IF NOT EXISTS earthdistance;
CREATE EXTENSION IF NOT EXISTS pg_trgm;
CREATE EXTENSION IF NOT EXISTS postgis;


CREATE OR REPLACE FUNCTION text2ts(text)
 RETURNS timestamp without time zone
 LANGUAGE sql
 IMMUTABLE
AS $function$SELECT CASE WHEN $1 ~'^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?(?:Z|\+\d{2}:\d{2})?$' THEN CAST($1 AS timestamp without time zone) END; $function$;

CREATE OR REPLACE FUNCTION json_array_to_pg_array(jsonarray jsonb)
 RETURNS text[]
 LANGUAGE plpgsql
 IMMUTABLE STRICT
AS $function$ begin if jsonarray <> 'null' then return (select array(select jsonb_array_elements_text(jsonarray))); else return null; end if; end; $function$;

CREATE OR REPLACE FUNCTION extract_keys_from_jsonb_object_array(jsonarray jsonb, key text DEFAULT 'Id'::text)
 RETURNS text[]
 LANGUAGE plpgsql
 IMMUTABLE STRICT
AS $function$ begin if jsonarray <> 'null' then return (select array(select data2::jsonb->> key from (select jsonb_array_elements_text(jsonarray) as data2) as subsel)); else return null; end if; end; $function$;

CREATE OR REPLACE FUNCTION public.extract_tags(jsonarray jsonb)
 RETURNS text[]
 LANGUAGE plpgsql
 IMMUTABLE strict
AS $function$ begin
	return
		(select array(select concat(x.tags->>'Source', '.', x.tags->>'Id') from
		(select jsonb_path_query(jsonarray, '$[*]') tags) x) x);
end; $function$;

CREATE OR REPLACE FUNCTION public.extract_tagkeys(jsonarray jsonb)
 RETURNS text[]
 LANGUAGE plpgsql
 IMMUTABLE strict
AS $function$ begin
	return (array(select distinct unnest(json_array_to_pg_array(jsonb_path_query_array(jsonarray, '$[*].Id')))));
end; $function$;

CREATE OR REPLACE FUNCTION is_valid_jsonb(p_json text) 
RETURNS JSONB
AS $$
begin
  return p_json::jsonb;
exception 
  when others then
     return null;  
end; $$ 
LANGUAGE plpgsql IMMUTABLE;

CREATE OR REPLACE FUNCTION public.json_2_ts_array(jsonarray jsonb)
 RETURNS timestamp[]
 LANGUAGE plpgsql
 IMMUTABLE STRICT
AS $function$ begin if jsonarray <> 'null' then return (
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
else return null;
end if;
end;
$function$
;

CREATE OR REPLACE FUNCTION public.json_2_tsrange_array(jsonarray jsonb)
 RETURNS tsrange[]
 LANGUAGE plpgsql
 IMMUTABLE STRICT
AS $function$ begin if jsonarray <> 'null' then return (
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
else return null;
end if;
end;
$function$;

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
    -- Durchlaufen des tsrange-Arrays
    FOREACH tsr IN ARRAY tsrange_array
    LOOP
        -- Hinzufügen des tsrange-Elements zum tsmultirange
        result := result + tsmultirange(tsrange(tsr));
    END LOOP;
	END IF;

    RETURN result;
END;
$$;

CREATE OR REPLACE FUNCTION public.calculate_access_array(source text, closeddata bool, reduced bool)
RETURNS text[]
LANGUAGE plpgsql
IMMUTABLE
AS $function$
begin
-- If data is from source lts and not reduced IDM only access --
if source = 'lts' and not reduced then return (array['IDM']);
end if;
-- If data is from source hgv only access IDM --
if source = 'hgv' then return (array['IDM']);
end if;
-- If data is from source a22 only access A22 --
if source = 'a22' then return (array['A22']);
end if;
-- If data is from source LTS and reduced give access to all others --
if source = 'lts' and reduced and not closeddata then return (array['A22','ANONYMOUS','STA']);
end if;
-- If data is not from source lts and a22 and not closed data give all access --
if source <> 'lts' and source <> 'a22' and not closeddata then return (array['A22','ANONYMOUS','IDM','STA']);
end if;
return (array['A22','ANONYMOUS','IDM','STA']);
end;
$function$;

CREATE OR REPLACE FUNCTION public.calculate_access_array_rawdata(source text, license text)
 RETURNS text[]
 LANGUAGE plpgsql
 IMMUTABLE
AS $function$
begin
-- If data is from source lts and not reduced IDM only access --
if source = 'lts' then return (array['IDM']);
end if;
-- If data is from source hgv only access IDM --
if source = 'hgv' then return (array['IDM']);
end if;
-- If data is from source a22 only access A22 --
if source = 'a22' then return (array['A22']);
end if;
-- If data is not from source lts and a22 and not closed data give all access --
if source <> 'lts' and source <> 'a22' then return (array['A22','ANONYMOUS','IDM','STA']);
end if;
return (array['A22','ANONYMOUS','IDM','STA']);
end;
$function$
;

--calculates the distance from given date
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
    -- Iterate trough Array
    FOREACH mytsrange IN array tsrange_array
    loop
	    singledayadd = 0;
	   
	   if(prioritizesingledayevents = true) then
	    
	    -- if prioritiyesingledayevents is used, increase all distances for multidayevents with 1
		     if(lower(mytsrange)::date = upper(mytsrange)::date) then
	         	singledayadd = 0;
	         else
	         	singledayadd = 1;
	         end if;
	        
	   end if;
	    
	    -- get tsrange which is matching (ongoing event) and add 0
	    if mytsrange @> ts_tocalculate then
           intarr := array_append(intarr, singledayadd);
        else
       -- check if period has already ended
           if upper(mytsrange)::timestamp > ts_tocalculate then
               --if period has not ended calculate distance
               intarr := array_append(intarr, extract(epoch from (lower(mytsrange)::timestamp - ts_tocalculate))::bigint + singledayadd);
	    	
           end if;
        
       end if;
       
    END LOOP;

      --get distance (default minimum distance is returned, desc gets the highest distance)
	  if sortorder = 'desc' then
	  	result = (select unnest(intarr) as x order by x desc limit 1);
	  else
	   result = (select unnest(intarr) as x order by x asc limit 1);
	  end if;
   	  
	END IF;		

    RETURN result;
END;
$function$
;

-- GETS the next tsrange to a given date
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
	-- Iterate trough Array
    FOREACH mytsrange IN array tsrange_array
    loop
	    -- get tsrange which is matching (ongoing event)
	    if mytsrange @> ts_tocalculate then
            result = mytsrange;
            distance = 0;
        else
       -- check if period has already ended
           if upper(mytsrange)::timestamp > ts_tocalculate then
           
               --if period has not ended calculate distance
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
$function$
;