-- SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
--
-- SPDX-License-Identifier: AGPL-3.0-or-later

-- public.rawchanges definition

-- Drop table

-- DROP TABLE public.rawchanges;

CREATE TABLE public.rawchanges (
	id serial4 NOT NULL,
	"type" varchar(150) NULL,
	datasource varchar(150) NULL,
	editedby varchar(150) NULL,
	editsource varchar(150) NULL,
	sourceid varchar(150) NULL,
	"date" timestamp NULL,
	changes jsonb NULL,
	license varchar(150) NULL,
	gen_access_role _text GENERATED ALWAYS AS (calculate_access_array_rawdata(datasource::text, license::text)) STORED NULL,
	CONSTRAINT rawchanges_pkey PRIMARY KEY (id)
);
CREATE INDEX source_ix ON public.rawchanges USING btree (datasource);
CREATE INDEX sourceid_ix ON public.rawchanges USING btree (sourceid);
CREATE INDEX type_ix ON public.rawchanges USING btree (type);