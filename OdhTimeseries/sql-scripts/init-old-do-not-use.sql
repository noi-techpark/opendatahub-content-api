CREATE SCHEMA intimev2;

CREATE TABLE intimev2.measurement (
	id int8 DEFAULT nextval('intimev2.measurement_seq'::regclass) NOT NULL,
	created_on timestamp NOT NULL,
	"period" int4 NOT NULL,
	"timestamp" timestamp NOT NULL,
	double_value float8 NOT NULL,
	provenance_id int8 NULL,
	station_id int8 NOT NULL,
	type_id int8 NOT NULL,
	CONSTRAINT measurement_pkey PRIMARY KEY (id),
	CONSTRAINT uc_measurement_station_id_type_id_period UNIQUE (station_id, type_id, period),
	CONSTRAINT fk_measurement_provenance_id_provenance_pk FOREIGN KEY (provenance_id) REFERENCES intimev2.provenance(id),
	CONSTRAINT fk_measurement_station_id_station_pk FOREIGN KEY (station_id) REFERENCES intimev2.station(id),
	CONSTRAINT fk_measurement_type_id_type_pk FOREIGN KEY (type_id) REFERENCES intimev2."type"(id)
);
CREATE INDEX idx_measurement_timestamp ON intimev2.measurement USING btree ("timestamp" DESC);

CREATE TABLE intimev2.measurementhistory (
	id int8 DEFAULT nextval('intimev2.measurementhistory_seq'::regclass) NOT NULL,
	created_on timestamp NOT NULL,
	"period" int4 NOT NULL,
	"timestamp" timestamp NOT NULL,
	double_value float8 NOT NULL,
	provenance_id int8 NULL,
	station_id int8 NOT NULL,
	type_id int8 NOT NULL,
	CONSTRAINT measurementhistory_pkey PRIMARY KEY (id),
	CONSTRAINT uc_measurementhistory_station_i__timestamp_period_double_value_ UNIQUE (station_id, type_id, "timestamp", period, double_value),
	CONSTRAINT fk_measurementhistory_provenance_id_provenance_pk FOREIGN KEY (provenance_id) REFERENCES intimev2.provenance(id),
	CONSTRAINT fk_measurementhistory_station_id_station_pk FOREIGN KEY (station_id) REFERENCES intimev2.station(id),
	CONSTRAINT fk_measurementhistory_type_id_type_pk FOREIGN KEY (type_id) REFERENCES intimev2."type"(id)
);

CREATE TABLE intimev2.provenance (
	id int8 DEFAULT nextval('intimev2.provenance_seq'::regclass) NOT NULL,
	data_collector varchar(255) NOT NULL,
	data_collector_version varchar(255) NULL,
	lineage varchar(255) NOT NULL,
	"uuid" varchar(255) NOT NULL,
	CONSTRAINT provenance_pkey PRIMARY KEY (id),
	CONSTRAINT uc_provenance_lineage_data_collector_data_collector_version UNIQUE (lineage, data_collector, data_collector_version),
	CONSTRAINT uc_provenance_uuid UNIQUE (uuid)
);

CREATE TABLE intimev2."type" (
	id int8 DEFAULT nextval('intimev2.type_seq'::regclass) NOT NULL,
	cname varchar(255) NOT NULL,
	created_on timestamp NULL,
	cunit varchar(255) NULL,
	description varchar(255) NULL,
	rtype varchar(255) NULL,
	meta_data_id int8 NULL,
	CONSTRAINT type_pkey PRIMARY KEY (id),
	CONSTRAINT uc_type_cname UNIQUE (cname),
	CONSTRAINT fk_type_meta_data_id_type_metadata_pk FOREIGN KEY (meta_data_id) REFERENCES intimev2.type_metadata(id)
);

CREATE TABLE intimev2.type_metadata (
	id int8 DEFAULT nextval('intimev2.type_metadata_seq'::regclass) NOT NULL,
	created_on timestamp NULL,
	"json" jsonb NULL,
	type_id int8 NULL,
	CONSTRAINT type_metadata_pkey PRIMARY KEY (id),
	CONSTRAINT fk_type_metadata_type_id_type_pk FOREIGN KEY (type_id) REFERENCES intimev2."type"(id)
);

CREATE TABLE intimev2.station (
	id int8 DEFAULT nextval('intimev2.station_seq'::regclass) NOT NULL,
	active bool NULL,
	available bool NULL,
	"name" varchar(255) NOT NULL,
	origin varchar(255) NULL,
	pointprojection public.geometry NULL,
	stationcode varchar(255) NOT NULL,
	stationtype varchar(255) NOT NULL,
	meta_data_id int8 NULL,
	parent_id int8 NULL,
	CONSTRAINT station_pkey PRIMARY KEY (id),
	CONSTRAINT uc_station_stationcode_stationtype UNIQUE (stationcode, stationtype),
	CONSTRAINT fk_station_meta_data_id_metadata_pk FOREIGN KEY (meta_data_id) REFERENCES intimev2.metadata(id),
	CONSTRAINT fk_station_parent_id_station_pk FOREIGN KEY (parent_id) REFERENCES intimev2.station(id)
);
CREATE INDEX idx_station_parent ON intimev2.station USING btree (parent_id);

CREATE TABLE intimev2.metadata (
	id int8 DEFAULT nextval('intimev2.metadata_seq'::regclass) NOT NULL,
	created_on timestamp NULL,
	"json" jsonb NULL,
	station_id int8 NULL,
	CONSTRAINT metadata_pkey PRIMARY KEY (id),
	CONSTRAINT fk_metadata_station_id_station_pk FOREIGN KEY (station_id) REFERENCES intimev2.station(id)
);
CREATE INDEX idx_metadata_history ON intimev2.metadata USING btree (station_id, created_on);