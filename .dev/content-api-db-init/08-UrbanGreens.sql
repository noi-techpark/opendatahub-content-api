-- SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
--
-- SPDX-License-Identifier: AGPL-3.0-or-later

-- public.urbangreens definition

CREATE TABLE public.urbangreens (
	id varchar(100) NOT NULL,
	"data" jsonb NULL,
	geo public.geometry NOT NULL,
	gen_active bool GENERATED ALWAYS AS ((data #> '{Active}'::text[])::boolean) STORED NULL,
	gen_haslanguage _text GENERATED ALWAYS AS (json_array_to_pg_array(data #> '{HasLanguage}'::text[])) STORED NULL,
	gen_licenseinfo_closeddata bool GENERATED ALWAYS AS ((data #> '{LicenseInfo,ClosedData}'::text[])::boolean) STORED NULL,
	gen_lastchange timestamp GENERATED ALWAYS AS (text2ts(data #>> '{LastChange}'::text[])) STORED NULL,
	gen_shortname text GENERATED ALWAYS AS (data #>> '{Shortname}'::text[]) STORED NULL,
	gen_source text GENERATED ALWAYS AS (data #>> '{_Meta,Source}'::text[]) STORED NULL,
	gen_reduced bool GENERATED ALWAYS AS ((data #> '{_Meta,Reduced}'::text[])::boolean) STORED NULL,
	gen_tags _text GENERATED ALWAYS AS (json_array_to_pg_array(data #> '{TagIds}'::text[])) STORED NULL,
	gen_id text GENERATED ALWAYS AS (data #>> '{Id}'::text[]) STORED NULL,
	gen_greencode text GENERATED ALWAYS AS (data #>> '{GreenCode}'::text[]) STORED NULL,
	gen_greencodeversion text GENERATED ALWAYS AS (data #>> '{GreenCodeVersion}'::text[]) STORED NULL,
	gen_greencodetype text GENERATED ALWAYS AS (data #>> '{GreenCodeType}'::text[]) STORED NULL,
	gen_greencodesubtype text GENERATED ALWAYS AS (data #>> '{GreenCodeSubtype}'::text[]) STORED NULL,
	gen_putonsite TIMESTAMPTZ GENERATED ALWAYS AS (text2tstz(data #>> '{PutOnSite}')) STORED NULL,
	gen_removedfromsite TIMESTAMPTZ GENERATED ALWAYS AS (text2tstz(data #>> '{RemovedFromSite}')) STORED NULL,
	gen_access_role _text GENERATED ALWAYS AS (calculate_access_array(data #>> '{_Meta,Source}'::text[], (data #> '{LicenseInfo,ClosedData}'::text[])::boolean, (data #> '{_Meta,Reduced}'::text[])::boolean)) STORED NULL,
	gen_center_position public.geometry GENERATED ALWAYS AS ( ST_Centroid(geo) ) STORED NULL,
	CONSTRAINT urbangreens_pkey PRIMARY KEY (id)
);
