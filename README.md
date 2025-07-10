<!--
SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>

SPDX-License-Identifier: CC0-1.0
-->

# opendatahub-content-api

Port of Open Data Hub Tourim Api to .Net Core.

[![REUSE Compliance](https://github.com/noi-techpark/opendatahub-content-api/actions/workflows/reuse.yml/badge.svg)](https://github.com/noi-techpark/opendatahub-docs/wiki/REUSE#badges)
[![CI/CD API](https://github.com/noi-techpark/opendatahub-content-api/actions/workflows/main_api.yml/badge.svg)](https://github.com/noi-techpark/opendatahub-content-api/actions/workflows/main_api.yml)
[![CI/CD Importer](https://github.com/noi-techpark/opendatahub-content-api/actions/workflows/main_importer.yml/badge.svg)](https://github.com/noi-techpark/opendatahub-content-api/actions/workflows/main_importer.yml)

## Project Goals/Requirements:

* .Net Core 5
* PostgreSQL Database
* Docker Support
* Swagger Support
* Identity Server Integration (Keycloak)
* Full Async Api
* improved Api Error Handling
* Browseable Api
* Swagger / Openapi Support
* Advanced api operations 1/2 (fields chooser, language selector, search terms)
* Advanced api operation 2/2 (raw sorting, raw filtering)
* Compatibility tourism api

## Project Structure

Test Server on https://tourism.api.opendatahub.testingmachine.eu  
Production Server on https://tourism.api.opendatahub.com

### DataModel

Definition of all ODH Tourism Models. Available as Nuget package (https://www.nuget.org/packages/opendatahub-datamodel-tourism)

### OdhApiCore

Api to retrieve ODH Tourism Data

### OdhApiImporter

Workerservice which is importing the Data with help of the Data Collectors

### Helper

Class Library with Extension Methods and other ODH Helper Methods

### CDB, DSS, EBMS, LCS, MSS, NINJA, RAVEN, SIAG, STA, SuedtirolWein, A22, FERATEL, LOOPTEC, PANOMAX, PANOCLOUD

Data Collectors used by Api and Importer, usually containing classes that retrieve Data, and classes that parse the data to ODH Objects (defined in DataModel)

### GeoConverter

Helper Library which allows converting  
kml to GeoJson  
gpx to GeoJson

### JsonLDTransformer

Helper Library which converts Open Data Hub Content Api Objects to schema.org Models in ld+json format.

### OdhNotifier

Component which notifies pre configured Consumers about Data Changes

### PushServer

Component which generateds Push Messages (Firebase Cloud Messaging)

### RawQueryParser

Helper Library where the rawfilter/rawsort functionality is implemented.

## Database

Postgres 15. The data is stored as Json Datatype in a single Column.

### Extensions

Extensions active on DB

* extension earthdistance;
* extension cube;
* extension pg_trgm;
* extension postgis

### Custom Functions

Custom Functions on DB

* json_array_to_pg_array
* extract_keys_from_jsonb_object_array
* text2ts
* json_array_to_pg_array
* extract_keys_from_jsonb_object_array
* extract_tags
* extract_tagkeys
* is_valid_jsonb
* json_2_tsrange_array
* convert_tsrange_array_to_tsmultirange
* json_2_ts_array
* get_abs_eventdate_single

These custom functions are used for the generated Columns

### Generated Columns

To speed up certain filters this project uses generated Columns where certain fields of the json data
is modelled as columns where indices can be set.

### Indices

Different Indices used
- btree indices (Single column indices)
- gin indices (Index for the Json Column)
- gist indices (Indices to speed up distance search)
- trigram indices (Indices to speed up Title search)



## Getting started:

Clone the repository  
Copy `.env.example` to `.env`  
Set the needed environment variables

### Environment Variables

* PG_CONNECTION (Connection to Postgres Database)
* MSS_USER; (Optional User to retrieve availability request from HGV Mss)
* MSS_PSWD; (Optional Pswd to retrieve availability request from HGV Mss)
* LCS_USER; (Optional User to retrieve availability request from Lts)
* LCS_PSWD; (Optional Pswd to retrieve availability request from Lts)
* LCS_MSGPSWD; (Optional Messagepswd to retrieve availability requests from LTS)
* SIAG_USER; (Optional User to retrieve data from Siag)
* SIAG_PSWD; (Optional Pswd to retrieve data from Siag)
* XMLDIR; (Directory where xml config file is stored)
* JSONPATH; (Directory where json files are stored)
* S3_BUCKET_ACCESSPOINT; (S3 Bucket for Image Upload accesspoint)
* S3_IMAGEUPLOADER_ACCESSKEY; (S3 Bucket for Image Upload accesskey)
* S3_IMAGEUPLOADER_SECRETKEY; (S3 Bucket for Image Upload secretkey)
* OAUTH_AUTORITY; (Oauth Server Authority URL)
* ELK_URL; (Serilog Elasticsearch Sink Elastic URL)
* ELK_TOKEN; (Serilog Elasticsearch Access Token)
* EBMS_USER; (Optional User to access EBMS interface)
* EBMS_PASS; (Optional Pswd to access EBMS interface)
* DSS_USER; (Optional User to access DSS interface)
* DSS_PSWD; (Optional Pswd to access DSS interface)
* DSS_SERVICEURL; (Optional DSS interface serviceurl)
* RAVEN_USER; (Optional User to access Raven interface)
* RAVEN_PSWD; (Optional Pswd to access Raven interface)
* RAVEN_SERVICEURL; (Optional Raven interface serviceurl)

### using Docker

go into \OdhApiCore\ folder \
`docker-compose up` starts the Open Data Hub Content Api application on http://localhost:8001/

go into \OdhApiImporter\ folder \
`docker-compose up` starts the  Open Data Hub Content Api application on http://localhost:8002/

### using .Net Core CLI

Install .Net Core SDK 5\
go into \OdhApiCore\ folder \
`dotnet run`
starts the application on 
https://localhost:5001;
http://localhost:5000

### Postgres

Activate extensions

```
CREATE EXTENSION cube;
```
```
CREATE EXTENSION earthdistance;
```
```
CREATE EXTENSION pg_trgm;
```
```
CREATE EXTENSION postgis;
```

Create generated columns on Postgres

* bool

```sql
ALTER TABLE tablename ADD IF NOT EXISTS gen_bool bool GENERATED ALWAYS AS ((data#>'{Active}')::bool)stored;
```

*string

```sql
ALTER TABLE tablename ADD IF NOT EXISTS gen_string text GENERATED ALWAYS AS ((data#>>'{Source}'))stored;
```

*double

```sql
ALTER TABLE tablename ADD IF NOT EXISTS gen_double DOUBLE precision GENERATED ALWAYS AS ((data#>'{Latitude}')::DOUBLE precision) stored;
```

* array (simple)

```sql
ALTER TABLE tablename ADD IF NOT EXISTS gen_array text[] GENERATED ALWAYS AS (json_array_to_pg_array(data#>'{HasLanguage}')) stored;
```

* array (object)

```sql
ALTER TABLE tablename ADD IF NOT EXISTS gen_array text[] GENERATED ALWAYS AS (extract_keys_from_jsonb_object_array(data#>'{Features}','Id')) stored;
```

* date

```sql
ALTER TABLE tablename ADD IF NOT EXISTS gen_date timestamp GENERATED ALWAYS AS (text2ts(data#>>'{LastChange}')::timestamp) stored;
```

* tsarray
```sql
ALTER TABLE events ADD IF NOT EXISTS gen_tsarray timestamp[] GENERATED ALWAYS AS (json_2_ts_array(data#>'{EventDate}')) stored;
```

* tsrangearray
```sql
ALTER TABLE events ADD IF NOT EXISTS gen_tsrangearray tsrange[] GENERATED ALWAYS AS (json_2_tsrange_array(data#>'{EventDate}')) stored;
```

* tsmultirange
```sql
ALTER TABLE tablename ADD IF NOT EXISTS gen_tsmultirange tsmultirange GENERATED ALWAYS AS (convert_tsrange_array_to_tsmultirange(json_2_tsrange_array(data#>'{EventDate}'))) stored;
```

* jsonb
```sql
ALTER TABLE tablename ADD IF NOT EXISTS gen_jsonb jsonb GENERATED ALWAYS AS ((data#>'{SomeJsonB}')::jsonb) stored;
```

* geometry (postgis)
```sql
ALTER TABLE tablename ADD IF NOT EXISTS gen_position public.geometry GENERATED ALWAYS AS (st_setsrid(st_makepoint((data #> '{GpsPoints,position,Longitude}'::text[])::double precision, (data #> '{GpsPoints,position,Latitude}'::text[])::double precision), 4326)) STORED NULL;
```

* access_based
```sql
ALTER TABLE tablename ADD IF NOT EXISTS gen_access_role text[] GENERATED ALWAYS AS (calculate_access_array(data#>>'{_Meta,Source}',(data#>'{LicenseInfo,ClosedData}')::bool,(data#>'{_Meta,Reduced}')::bool)) stored;
```
 

Custom functions for Postgres Generated Columns creation

* text2ts

```sql
CREATE OR REPLACE FUNCTION text2ts(text)
 RETURNS timestamp without time zone
 LANGUAGE sql
 IMMUTABLE
AS $function$SELECT CASE WHEN $1 ~'^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?(?:Z|\+\d{2}:\d{2})?$' THEN CAST($1 AS timestamp without time zone) END; $function$;
```
* json_array_to_pg_array

```sql
CREATE OR REPLACE FUNCTION json_array_to_pg_array(jsonarray jsonb)
 RETURNS text[]
 LANGUAGE plpgsql
 IMMUTABLE STRICT
AS $function$ begin if jsonarray <> 'null' then return (select array(select jsonb_array_elements_text(jsonarray))); else return null; end if; end; $function$;
```

* extract_keys_from_jsonb_object_array

```sql
CREATE OR REPLACE FUNCTION extract_keys_from_jsonb_object_array(jsonarray jsonb, key text DEFAULT 'Id'::text)
 RETURNS text[]
 LANGUAGE plpgsql
 IMMUTABLE STRICT
AS $function$ begin if jsonarray <> 'null' then return (select array(select data2::jsonb->> key from (select jsonb_array_elements_text(jsonarray) as data2) as subsel)); else return null; end if; end; $function$;
```

* extract_tags

```sql
CREATE OR REPLACE FUNCTION public.extract_tags(jsonarray jsonb)
 RETURNS text[]
 LANGUAGE plpgsql
 IMMUTABLE strict
AS $function$ begin
	return
		(select array(select concat(x.tags->>'Source', '.', x.tags->>'Id') from
		(select jsonb_path_query(jsonarray, '$.*[*]') tags) x) x);
end; $function$
```

* extract_tagkeys

```sql
CREATE OR REPLACE FUNCTION public.extract_tagkeys(jsonarray jsonb)
 RETURNS text[]
 LANGUAGE plpgsql
 IMMUTABLE strict
AS $function$ begin
	return (array(select distinct unnest(json_array_to_pg_array(jsonb_path_query_array(jsonarray, '$.*[*].Id')))));
end; $function$
```

* is_valid_jsonb

```sql
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
```

* json_2_ts_array

```sql
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
```

* json_2_tsrange_array

Used on Events, creates a TSRange Array from EventDate Json

```sql
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
$function$
```

* convert_tsrange_array_to_tsmultirange

```sql
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
```

* calculate_access_array

```sql
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
$function$

//For the rawdata table
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
```

* get_nearest_tsrange_distance

Used on Events calculates the difference between a given date and it's next Date Interval

```sql
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
```

* get_nearest_tsrange

Used on Events, gets the next Date Interval to a passed date as TSRange

```sql
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
```

### REUSE

This project is [REUSE](https://reuse.software) compliant, more information about the usage of REUSE in NOI Techpark repositories can be found [here](https://github.com/noi-techpark/opendatahub-docs/wiki/Guidelines-for-developers-and-licenses#guidelines-for-contributors-and-new-developers).

Since the CI for this project checks for REUSE compliance you might find it useful to use a pre-commit hook checking for REUSE compliance locally. The [pre-commit-config](.pre-commit-config.yaml) file in the repository root is already configured to check for REUSE compliance with help of the [pre-commit](https://pre-commit.com) tool.

Install the tool by running:
```bash
pip install pre-commit
```
Then install the pre-commit hook via the config file by running:
```bash
pre-commit install
```
