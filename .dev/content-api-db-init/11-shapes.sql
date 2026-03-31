-- SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
--
-- SPDX-License-Identifier: AGPL-3.0-or-later

-- public.shapes definition

-- Drop table

-- DROP TABLE public.shapes;

CREATE TABLE public.shapes (
	id serial4 NOT NULL,
	country varchar(2) NULL,
	code_rip float8 NULL,
	code_reg float8 NULL,
	code_prov float8 NULL,
	code_cm float8 NULL,
	code_uts float8 NULL,
	istatnumber varchar(6) NULL,
	abbrev varchar(2) NULL,
	type_uts varchar(50) NULL,
	"name" varchar(100) NULL,
	name_alternative varchar(100) NULL,
	shape_leng numeric NULL,
	shape_area numeric NULL,
	geom public.geometry(multipolygon, 32632) NULL,
	"type" varchar NULL,
	licenseinfo jsonb NULL,
	meta jsonb NULL,
	geometry public.geometry(multipolygon, 4326) NULL,
	"source" varchar NULL,
	"data" jsonb GENERATED ALWAYS AS (createshapejson(id, country::text, code_rip, code_reg, code_prov, code_cm, code_uts, istatnumber::text, abbrev::text, type_uts::text, type::text, name::text, name_alternative::text, shape_leng::double precision, shape_area::double precision, source::text, meta, licenseinfo, geometry)) STORED NULL,
	idstring varchar NULL,
	"mapping" jsonb NULL,
	srid varchar NULL,
	CONSTRAINT shapes_pkey PRIMARY KEY (id)
);
CREATE INDEX shapes_geom_idx ON public.shapes USING gist (geom);