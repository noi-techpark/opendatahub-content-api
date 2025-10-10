-- SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
--
-- SPDX-License-Identifier: AGPL-3.0-or-later


CREATE TABLE public.geoshapes (
	id varchar(50) NOT NULL,
	country varchar(2) NULL,
	"name" varchar(100) NULL,
	"type" varchar NULL,
	licenseinfo jsonb NULL,
	meta jsonb NULL,
	"mapping" jsonb NULL,
	geometry public.geometry NULL,
	"source" varchar NULL,
	srid varchar NULL,
	CONSTRAINT geoshapes_pkey PRIMARY KEY (id)
);

CREATE OR REPLACE FUNCTION public.createshapedata_32632(id text, type text, name text, country text, source text, meta jsonb, licenseinfo jsonb, mapping jsonb, srid text, geom geometry)
 RETURNS jsonb
 LANGUAGE plpgsql
 IMMUTABLE
AS $function$ begin
	return
		json_build_object(
		'Id',id,
		'Self', CONCAT('GeoShape/',id),	
		'Name',name,		
		'Source',"source",
		'Type', "type",
        'Country', country,
		'_Meta', meta,		
		'LicenseInfo', licenseinfo,	
		'Mapping', mapping,	
		'Srid', 'EPSG:32632',	
		'Geometry',geom);
end; $function$
;

CREATE OR REPLACE FUNCTION public.createshapedata_3857(id text, type text, name text, country text, source text, meta jsonb, licenseinfo jsonb, mapping jsonb, srid text, geom geometry)
 RETURNS jsonb
 LANGUAGE plpgsql
 IMMUTABLE
AS $function$ begin
	return
		json_build_object(
		'Id',id,
		'Self', CONCAT('GeoShape/',id),	
		'Name',name,		
		'Source',"source",
		'Type', "type",
        'Country', country,
		'_Meta', meta,		
		'LicenseInfo', licenseinfo,	
		'Mapping', mapping,	
		'Srid', 'EPSG:3857',	
		'Geometry',ST_Transform(geom, 3857));
end; $function$
;


CREATE OR REPLACE FUNCTION public.createshapedata_4326(id text, type text, name text, country text, source text, meta jsonb, licenseinfo jsonb, mapping jsonb, srid text, geom geometry)
 RETURNS jsonb
 LANGUAGE plpgsql
 IMMUTABLE
AS $function$ begin
	return
		json_build_object(
		'Id',id,		
        'Self', CONCAT('GeoShape/',id),
		'Name',name,		
		'Source',"source",
		'Type', "type",
        'Country', country,
		'_Meta', meta,		
		'LicenseInfo', licenseinfo,	
		'Mapping', mapping,	
		'Srid', 'EPSG:4326',	
		'Geometry',ST_Transform(geom, 4326));
end; $function$
;

alter table geoshapes add column "data" jsonb GENERATED ALWAYS AS (createshapedata_4326(id::text, type::text, name::text, country::text, source::text, meta, licenseinfo, mapping, srid::text, geometry)) STORED NULL;

alter table geoshapes add column data32632 jsonb GENERATED ALWAYS AS (createshapedata_32632(id::text, type::text, name::text, country::text, source::text, meta, licenseinfo, mapping, srid::text, geometry)) STORED NULL;

alter table geoshapes add column data3857 jsonb GENERATED ALWAYS AS (createshapedata_3857(id::text, type::text, name::text, country::text, source::text, meta, licenseinfo, mapping, srid::text, geometry)) STORED NULL;