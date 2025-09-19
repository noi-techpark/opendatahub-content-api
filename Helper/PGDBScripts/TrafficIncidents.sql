
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

-- Create TrafficIncidents table following standard pattern
CREATE TABLE IF NOT EXISTS public.trafficincidents (
	id varchar(100) NOT NULL,
	"data" jsonb NULL,
	gen_active bool GENERATED ALWAYS AS ((data #> '{Active}'::text[])::boolean) STORED NULL,
	gen_smgactive bool GENERATED ALWAYS AS ((data #> '{SmgActive}'::text[])::boolean) STORED NULL,
	gen_source text GENERATED ALWAYS AS (data #>> '{_Meta,Source}'::text[]) STORED NULL,
	gen_incidenttype text GENERATED ALWAYS AS (data #>> '{IncidentType}'::text[]) STORED NULL,
	gen_severity text GENERATED ALWAYS AS (data #>> '{Severity}'::text[]) STORED NULL,
	gen_status text GENERATED ALWAYS AS (data #>> '{Status}'::text[]) STORED NULL,
	gen_starttime timestamp GENERATED ALWAYS AS (text2ts(data #>> '{StartTime}'::text[])) STORED NULL,
	gen_endtime timestamp GENERATED ALWAYS AS (text2ts(data #>> '{EndTime}'::text[])) STORED NULL,
	gen_lastchange timestamp GENERATED ALWAYS AS (text2ts(data #>> '{LastChange}'::text[])) STORED NULL,
	gen_roadclosure bool GENERATED ALWAYS AS ((data #> '{RoadClosure}'::text[])::boolean) STORED NULL,
	gen_languages _text GENERATED ALWAYS AS (json_array_to_pg_array(data #> '{HasLanguage}'::text[])) STORED NULL,
	gen_affectedroutes _text GENERATED ALWAYS AS (json_array_to_pg_array(data #> '{AffectedRoutes}'::text[])) STORED NULL,
	gen_smgtags _text GENERATED ALWAYS AS (json_array_to_pg_array(data #> '{SmgTags}'::text[])) STORED NULL,
	gen_access_role text[] GENERATED ALWAYS AS (calculate_access_array(data#>>'{_Meta,Source}',(data#>'{LicenseInfo,ClosedData}')::bool)) STORED NULL,
	gen_geometry public.geometry GENERATED ALWAYS AS (ST_SetSRID(ST_GeomFromGeoJSON(data #>> '{Geometry}'::text[]), 4326)) STORED NULL,
	CONSTRAINT trafficincidents_pkey PRIMARY KEY (id)
);

-- Create indices for performance (use GIN for most as per EventsTable pattern)
CREATE INDEX IF NOT EXISTS ix_trafficincidents_data_gin ON public.trafficincidents USING gin (data);
CREATE INDEX IF NOT EXISTS ix_trafficincidents_geometry ON public.trafficincidents USING gist (gen_geometry);
CREATE INDEX IF NOT EXISTS ix_trafficincidents_active ON public.trafficincidents USING btree (gen_active);
CREATE INDEX IF NOT EXISTS ix_trafficincidents_smgactive ON public.trafficincidents USING btree (gen_smgactive);
CREATE INDEX IF NOT EXISTS ix_trafficincidents_source ON public.trafficincidents USING btree (gen_source);
CREATE INDEX IF NOT EXISTS ix_trafficincidents_lastchange ON public.trafficincidents USING btree (gen_lastchange);
CREATE INDEX IF NOT EXISTS ix_trafficincidents_languages ON public.trafficincidents USING gin (gen_languages);
CREATE INDEX IF NOT EXISTS ix_trafficincidents_affectedroutes ON public.trafficincidents USING gin (gen_affectedroutes);
CREATE INDEX IF NOT EXISTS ix_trafficincidents_smgtags ON public.trafficincidents USING gin (gen_smgtags);
CREATE INDEX IF NOT EXISTS ix_trafficincidents_access_role ON public.trafficincidents USING gin (gen_access_role);