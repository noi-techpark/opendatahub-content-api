-- public.tags definition

-- Drop table

-- DROP TABLE public.tags;

CREATE TABLE public.tags (
	id varchar(100) NOT NULL,
	"data" jsonb NULL,
	gen_licenseinfo_closeddata bool GENERATED ALWAYS AS ((data #> '{LicenseInfo,ClosedData}'::text[])::boolean) STORED NULL,
	gen_lastchange timestamp GENERATED ALWAYS AS (text2ts(data #>> '{LastChange}'::text[])) STORED NULL,
	gen_shortname text GENERATED ALWAYS AS (data #>> '{Shortname}'::text[]) STORED NULL,
	gen_source text GENERATED ALWAYS AS (data #>> '{_Meta,Source}'::text[]) STORED NULL,
	gen_reduced bool GENERATED ALWAYS AS ((data #> '{_Meta,Reduced}'::text[])::boolean) STORED NULL,
	gen_publishedon _text GENERATED ALWAYS AS (json_array_to_pg_array(data #> '{PublishedOn}'::text[])) STORED NULL,
	gen_access_role _text GENERATED ALWAYS AS (calculate_access_array('all'::text, (data #> '{LicenseInfo,ClosedData}'::text[])::boolean, (data #> '{_Meta,Reduced}'::text[])::boolean)) STORED NULL,
	gen_types _text GENERATED ALWAYS AS (json_array_to_pg_array(data #> '{Types}'::text[])) STORED NULL,
	rawdataid int4 NULL,
	CONSTRAINT tags_pkey PRIMARY KEY (id)
);