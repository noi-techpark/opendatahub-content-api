-- SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
--
-- SPDX-License-Identifier: AGPL-3.0-or-later

-- public.smgpois definition

-- Drop table

-- DROP TABLE public.smgpois;

CREATE TABLE public.smgpois (
	id varchar(200) NOT NULL,
	"data" jsonb NULL,
	gen_licenseinfo_closeddata bool GENERATED ALWAYS AS ((data #> '{LicenseInfo,ClosedData}'::text[])::boolean) STORED NULL,
	gen_active bool GENERATED ALWAYS AS ((data #> '{Active}'::text[])::boolean) STORED NULL,
	gen_haslanguage _text GENERATED ALWAYS AS (json_array_to_pg_array(data #> '{HasLanguage}'::text[])) STORED NULL,
	gen_smgtags _text GENERATED ALWAYS AS (json_array_to_pg_array(data #> '{SmgTags}'::text[])) STORED NULL,
	gen_lastchange timestamp GENERATED ALWAYS AS (text2ts(data #>> '{LastChange}'::text[])) STORED NULL,
	gen_latitude float8 GENERATED ALWAYS AS ((data #> '{GpsPoints,position,Latitude}'::text[])::double precision) STORED NULL,
	gen_longitude float8 GENERATED ALWAYS AS ((data #> '{GpsPoints,position,Longitude}'::text[])::double precision) STORED NULL,
	gen_syncsourceinterface text GENERATED ALWAYS AS (data #>> '{SyncSourceInterface}'::text[]) STORED NULL,
	gen_shortname text GENERATED ALWAYS AS (data #>> '{Shortname}'::text[]) STORED NULL,
	gen_odhactive bool GENERATED ALWAYS AS ((data #> '{OdhActive}'::text[])::boolean) STORED NULL,
	gen_publishedon _text GENERATED ALWAYS AS (json_array_to_pg_array(data #> '{PublishedOn}'::text[])) STORED NULL,
	gen_source text GENERATED ALWAYS AS (data #>> '{_Meta,Source}'::text[]) STORED NULL,
	gen_reduced bool GENERATED ALWAYS AS ((data #> '{_Meta,Reduced}'::text[])::boolean) STORED NULL,
	rawdataid int4 NULL,
	gen_hascc0image bool GENERATED ALWAYS AS (data @> '{"ImageGallery": [{"License": "CC0"}]}'::jsonb) STORED NULL,
	gen_hasimage bool GENERATED ALWAYS AS ((data #>> '{ImageGallery}'::text[]) IS NOT NULL) STORED NULL,
	gen_access_role _text GENERATED ALWAYS AS (calculate_access_array(data #>> '{_Meta,Source}'::text[], (data #> '{LicenseInfo,ClosedData}'::text[])::boolean, (data #> '{_Meta,Reduced}'::text[])::boolean)) STORED NULL,
	gen_position public.geometry GENERATED ALWAYS AS (st_setsrid(st_makepoint((data #> '{GpsPoints,position,Longitude}'::text[])::double precision, (data #> '{GpsPoints,position,Latitude}'::text[])::double precision), 4326)) STORED NULL,
	gen_id text GENERATED ALWAYS AS (data #>> '{Id}'::text[]) STORED NULL,
	gen_tags _text GENERATED ALWAYS AS (json_array_to_pg_array_lower(data #> '{TagIds}'::text[])) STORED NULL,
	CONSTRAINT smgpois_pkey PRIMARY KEY (id)
);
CREATE INDEX customid_ix ON public.smgpois USING btree (((data ->> 'CustomId'::text)));
CREATE INDEX smgpois_detail_de_title_trgm_idx ON public.smgpois USING gin (((data #>> '{Detail,de,Title}'::text[])) gin_trgm_ops);
CREATE INDEX smgpois_detail_en_title_trgm_idx ON public.smgpois USING gin (((data #>> '{Detail,en,Title}'::text[])) gin_trgm_ops);
CREATE INDEX smgpois_detail_it_title_trgm_idx ON public.smgpois USING gin (((data #>> '{Detail,it,Title}'::text[])) gin_trgm_ops);
CREATE INDEX smgpois_gen_smgtags ON public.smgpois USING gin (gen_smgtags);
CREATE INDEX smgpoisearthix ON public.smgpois USING gist (ll_to_earth(((((data -> 'GpsPoints'::text) -> 'position'::text) ->> 'Latitude'::text))::double precision, ((((data -> 'GpsPoints'::text) -> 'position'::text) ->> 'Longitude'::text))::double precision));