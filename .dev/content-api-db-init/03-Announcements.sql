-- SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
--
-- SPDX-License-Identifier: AGPL-3.0-or-later

-- public.announcements definition

CREATE TABLE public.announcements (
	id varchar(100) NOT NULL,
	"data" jsonb NULL,
	gen_active bool GENERATED ALWAYS AS ((data #> '{Active}'::text[])::boolean) STORED NULL,
	gen_haslanguage _text GENERATED ALWAYS AS (json_array_to_pg_array(data #> '{HasLanguage}'::text[])) STORED NULL,
	gen_licenseinfo_closeddata bool GENERATED ALWAYS AS ((data #> '{LicenseInfo,ClosedData}'::text[])::boolean) STORED NULL,
	gen_lastchange timestamp GENERATED ALWAYS AS (text2ts(data #>> '{LastChange}'::text[])) STORED NULL,
	gen_shortname text GENERATED ALWAYS AS (data #>> '{Shortname}'::text[]) STORED NULL,
	gen_source text GENERATED ALWAYS AS (data #>> '{_Meta,Source}'::text[]) STORED NULL,
	gen_reduced bool GENERATED ALWAYS AS ((data #> '{_Meta,Reduced}'::text[])::boolean) STORED NULL,
	gen_tags _text GENERATED ALWAYS AS (json_array_to_pg_array(data #> '{TagIds}'::text[])) STORED NULL,
	gen_id text GENERATED ALWAYS AS (data #>> '{Id}'::text[]) STORED NULL,
	gen_begindate TIMESTAMPTZ GENERATED ALWAYS AS (text2tstz(data #>> '{StartTime}')) STORED NULL,
    gen_enddate TIMESTAMPTZ GENERATED ALWAYS AS (text2tstz(data #>> '{EndTime}')) STORED NULL,
	gen_access_role _text GENERATED ALWAYS AS (calculate_access_array(data #>> '{_Meta,Source}'::text[], (data #> '{LicenseInfo,ClosedData}'::text[])::boolean, (data #> '{_Meta,Reduced}'::text[])::boolean)) STORED NULL,
	gen_geometry public.geometry GENERATED ALWAYS AS (
        ST_SetSRID(
            ST_GeomFromText(data #>> '{WKTGeometry4326}'), 
            4326
        )
    ) STORED NULL,
	gen_center_position public.geometry GENERATED ALWAYS AS (
        ST_Centroid(
            ST_SetSRID(
                ST_GeomFromText(data #>> '{WKTGeometry4326}'), 
                4326
            )
        )
    ) STORED NULL,
	CONSTRAINT roadincidents_pkey PRIMARY KEY (id)
);