-- public.events definition

-- Drop table

-- DROP TABLE public.events;

CREATE TABLE IF NOT EXISTS public.events (
	id varchar(100) NOT NULL,
	"data" jsonb NULL,
	gen_licenseinfo_closeddata bool GENERATED ALWAYS AS ((data #> '{LicenseInfo,ClosedData}'::text[])::boolean) STORED NULL,
	gen_odhactive bool GENERATED ALWAYS AS ((data #> '{OdhActive}'::text[])::boolean) STORED NULL,
	gen_active bool GENERATED ALWAYS AS ((data #> '{Active}'::text[])::boolean) STORED NULL,
	gen_haslanguage _text GENERATED ALWAYS AS (json_array_to_pg_array(data #> '{HasLanguage}'::text[])) STORED NULL,
	gen_smgtags _text GENERATED ALWAYS AS (json_array_to_pg_array(data #> '{SmgTags}'::text[])) STORED NULL,
	gen_lastchange timestamp GENERATED ALWAYS AS (text2ts(data #>> '{LastChange}'::text[])) STORED NULL,
	gen_shortname text GENERATED ALWAYS AS (data #>> '{Shortname}'::text[]) STORED NULL,
	gen_eventtopic _text GENERATED ALWAYS AS (json_array_to_pg_array(data #> '{TopicRIDs}'::text[])) STORED NULL,
	gen_begindate timestamp GENERATED ALWAYS AS (text2ts(data #>> '{DateBegin}'::text[])) STORED NULL,
	gen_enddate timestamp GENERATED ALWAYS AS (text2ts(data #>> '{DateEnd}'::text[])) STORED NULL,
	gen_nextbegindate timestamp GENERATED ALWAYS AS (text2ts(data #>> '{NextBeginDate}'::text[])) STORED NULL,
	gen_latitude float8 GENERATED ALWAYS AS ((data #> '{Latitude}'::text[])::double precision) STORED NULL,
	gen_longitude float8 GENERATED ALWAYS AS ((data #> '{Longitude}'::text[])::double precision) STORED NULL,
	gen_syncsourceinterface text GENERATED ALWAYS AS (data #>> '{Source}'::text[]) STORED NULL,
	rawdataid int4 NULL,
	gen_publishedon _text GENERATED ALWAYS AS (json_array_to_pg_array(data #> '{PublishedOn}'::text[])) STORED NULL,
	gen_source text GENERATED ALWAYS AS (data #>> '{_Meta,Source}'::text[]) STORED NULL,
	gen_reduced bool GENERATED ALWAYS AS ((data #> '{_Meta,Reduced}'::text[])::boolean) STORED NULL,
	gen_eventdates tsmultirange GENERATED ALWAYS AS (convert_tsrange_array_to_tsmultirange(json_2_tsrange_array(data #> '{EventDate}'::text[]))) STORED NULL,
	gen_access_role _text GENERATED ALWAYS AS (calculate_access_array(data #>> '{_Meta,Source}'::text[], (data #> '{LicenseInfo,ClosedData}'::text[])::boolean, (data #> '{_Meta,Reduced}'::text[])::boolean)) STORED NULL,
	gen_eventdatebeginarray _timestamp GENERATED ALWAYS AS (json_2_ts_array(data #> '{EventDate}'::text[])) STORED NULL,
	gen_eventdatearray _tsrange GENERATED ALWAYS AS (json_2_tsrange_array(data #> '{EventDate}'::text[])) STORED NULL,
	gen_position public.geometry GENERATED ALWAYS AS (st_setsrid(st_makepoint((data #> '{GpsPoints,position,Longitude}'::text[])::double precision, (data #> '{GpsPoints,position,Latitude}'::text[])::double precision), 4326)) STORED NULL,
	gen_id text GENERATED ALWAYS AS (data #>> '{Id}'::text[]) STORED NULL,
	CONSTRAINT events_pkey PRIMARY KEY (id)
);
CREATE INDEX events_detail_de_title_trgm_idx ON public.events USING gin (((data #>> '{Detail,de,Title}'::text[])) gin_trgm_ops);
CREATE INDEX events_detail_en_title_trgm_idx ON public.events USING gin (((data #>> '{Detail,en,Title}'::text[])) gin_trgm_ops);
CREATE INDEX events_detail_it_title_trgm_idx ON public.events USING gin (((data #>> '{Detail,it,Title}'::text[])) gin_trgm_ops);
CREATE INDEX eventsearthix ON public.events USING gist (ll_to_earth(((data ->> 'Latitude'::text))::double precision, ((data ->> 'Longitude'::text))::double precision));