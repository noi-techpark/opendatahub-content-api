-- SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
--
-- SPDX-License-Identifier: AGPL-3.0-or-later

-- public.testdatas definition

CREATE TABLE public.testdatas (
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
	gen_access_role _text GENERATED ALWAYS AS (calculate_access_array(data #>> '{_Meta,Source}'::text[], (data #> '{LicenseInfo,ClosedData}'::text[])::boolean, (data #> '{_Meta,Reduced}'::text[])::boolean)) STORED NULL,
	CONSTRAINT testdatas_pkey PRIMARY KEY (id)
);

INSERT INTO public.testdatas (id, "data")
VALUES 
(
    'test-id-001-complete', -- Single Entry ID
    '
    {
		"Id": "test-id-001-complete",
		"Type": "Testdata",
        "Active": true,
        "Detail": {
            "en": { "Title": "English Title", "BaseText": "Main detail in English.", "Language": "en" },
            "it": { "Title": "Titolo Italiano", "BaseText": "Dettaglio principale in Italiano.", "Language": "it" },
            "de": { "Title": "Deutscher Titel", "BaseText": "Hauptdetail in Deutsch.", "Language": "de" }
        },
        "RelatedContent": [
            { "Id": "rel-A" },
            { "Id": "rel-B" }
        ],
        "Foo": {
            "Deprecated": [
                {
                    "DeprecatedSource": "Old Source Dep-1",
                    "NestedDeprecatedDetail": {
                        "en": { "BaseText": "Nested Deprecated EN", "Language": "en" },
                        "it": { "BaseText": "Nested Deprecated IT", "Language": "it" }
                    },
                    "Geo": {
                        "old_point_1": { "Gpstype": "viewpoint", "Latitude": 46.500, "Longitude": 11.500, "Geometry": "POINT(11.5 46.5)" },
                        "old_point_2": { "Gpstype": "carparking", "Latitude": 46.550, "Longitude": 11.550, "Geometry": "POINT(11.55 46.55)" }
                    }
                }
            ],
            "Valid": [
                {
                    "ValidSource": "New Source Valid-1",
                    "DeprecatedLeaf": "This leaf is obsolete",
                    "NestedDetail": {
                        "it": { "BaseText": "Nested Valid IT", "Language": "it" },
                        "de": { "BaseText": "Nested Valid DE", "Language": "de" }
                    },
                    "NestedDeprecatedLeafDetail": {
                        "en": { "BaseText": "Nested Deprecated Leaf EN", "Language": "en" },
                        "de": { "BaseText": "Nested Deprecated Leaf DE", "Language": "de" }
                    }
                }
            ]
        },
        "FooDeprecated": { 
            "Deprecated": [
                { "DeprecatedSource": "FDP-DS-1" }, 
                { "DeprecatedSource": "FDP-DS-2" }
            ], 
            "Valid": [
                { "ValidSource": "FDP-VS-1" }, 
                { "ValidSource": "FDP-VS-2" }
            ] 
        },
        "LoreIpsum": [
            { 
                "Deprecated": [
                    { 
                        "DeprecatedSource": "Lore-Dep-1",
                        "NestedDeprecatedDetail": { 
                            "en": { "BaseText": "LoreIpsum Dep Detail EN", "Language": "en" },
                            "it": { "BaseText": "LoreIpsum Dep Detail IT", "Language": "it" }
                        }
                    }, 
                    { 
                        "DeprecatedSource": "Lore-Dep-2",
                        "NestedDeprecatedDetail": { 
                            "de": { "BaseText": "LoreIpsum Dep Detail DE", "Language": "de" },
                            "it": { "BaseText": "LoreIpsum Dep Detail IT 2", "Language": "it" }
                        }
                    }
                ],
                "Valid": [
                    { 
                        "ValidSource": "Lore-V1",
                        "NestedDetail": {
                            "en": { "Title": "LoreIpsum Valid Title EN", "Language": "en" },
                            "de": { "Title": "LoreIpsum Valid Title DE", "Language": "de" }
                        },
                        "NestedDeprecatedLeafDetail": {
                            "it": { "BaseText": "LoreIpsum Valid Dep Leaf IT", "Language": "it" },
                            "de": { "BaseText": "LoreIpsum Valid Dep Leaf DE", "Language": "de" }
                        }
                    }, 
                    { 
                        "ValidSource": "Lore-V2",
                        "NestedDetail": {
                            "en": { "Title": "LoreIpsum Valid Title EN 2", "Language": "en" },
                            "it": { "Title": "LoreIpsum Valid Title IT 2", "Language": "it" }
                        }
                    }
                ]
            }
        ],
        "LoreIpsumDeprecated": [
            { 
                "Deprecated": [
                    { 
                        "DeprecatedSource": "Lore-DepD-1",
                        "NestedDeprecatedDetail": { 
                            "en": { "BaseText": "LID Dep Detail EN", "Language": "en" },
                            "de": { "BaseText": "LID Dep Detail DE", "Language": "de" }
                        }
                    }, 
                    { 
                        "DeprecatedSource": "Lore-DepD-2",
                        "NestedDeprecatedDetail": { 
                            "it": { "BaseText": "LID Dep Detail IT", "Language": "it" },
                            "en": { "BaseText": "LID Dep Detail EN 2", "Language": "en" }
                        }
                    }
                ],
                "Valid": [
                    { 
                        "ValidSource": "Lore-D1",
                        "NestedDetail": {
                            "en": { "Title": "LID Valid Title EN", "Language": "en" },
                            "it": { "Title": "LID Valid Title IT", "Language": "it" }
                        },
                        "NestedDeprecatedLeafDetail": {
                            "en": { "BaseText": "LID Valid Dep Leaf EN", "Language": "en" },
                            "de": { "BaseText": "LID Valid Dep Leaf DE", "Language": "de" }
                        }
                    }, 
                    { 
                        "ValidSource": "Lore-D2",
                        "NestedDetail": {
                            "de": { "Title": "LID Valid Title DE 2", "Language": "de" },
                            "it": { "Title": "LID Valid Title IT 2", "Language": "it" }
                        }
                    }
                ]
            }
        ],
        "Geo": {
            "main_pos": { "Gpstype": "position", "Latitude": 46.49, "Longitude": 11.35, "Default": true, "Geometry": "POINT(11.35 46.49)" },
            "secondary_pos": { "Gpstype": "viewpoint", "Latitude": 46.40, "Longitude": 11.30, "Geometry": "POINT(11.3 46.4)" }
        },
        "AdditionalProperties": {
            "TestadataProperties": {
                "Valid": [
                    { "ValidSource": "AP-Source-1" },
                    { "ValidSource": "AP-Source-2" }
                ]
            }
        },
        "Id": "test-id-001-complete",
        "Source": "Internal-Test-System",
        "Shortname": "CompleteTest",
        "FirstImport": "2025-11-20T10:00:00Z",
        "LastChange": "2025-11-21T10:30:00Z",
        "HasLanguage": ["en", "it", "de"],
        "TagIds": ["full", "testdata", "geo"],
        "Mapping": {
            "legacy_id": {
                "system_alpha": "ID-A-123",
                "system_beta": "ID-B-456"
            }
        },
        "_Meta": {
			"Reduced": false,
            "Source": "Internal-Test-System",
			"Id": "test-id-001-complete",
			"Type": "Testdata"
        },
        "LicenseInfo": {
            "ClosedData": false
        }
    }
    '::jsonb
);

INSERT INTO public.testdatas (id, "data")
VALUES 
(
    'test-id-002', -- Entry 1
    '
    {
		"Id": "test-id-002",
		"Type": "Testdata",
        "Active": true,
		"_Meta": {
			"Reduced": false,
			"Id": "test-id-002",
			"Type": "Testdata"
		},
        "Detail": {
            "en": { "BaseText": "Main detail EN.", "Language": "en" },
            "de": { "BaseText": "Main detail DE.", "Language": "de" }
        },
        "RelatedContent": [
            { "Id": "rel-001" },
            { "Id": "rel-002" }
        ],
        "Foo": {
            "Deprecated": [
                {
                    "DeprecatedSource": "lts A",
                    "NestedDeprecatedDetail": {
                        "it": { "BaseText": "Nested IT.", "Language": "it" },
                        "fr": { "BaseText": "Nested FR.", "Language": "fr" }
                    },
                    "Geo": {
                        "old_gps_a": { "Gpstype": "viewpoint", "Latitude": 46.5, "Longitude": 11.5, "Geometry": "POINT(12.0 47.0)" },
                        "old_gps_b": { "Gpstype": "arrivalpoint", "Latitude": 46.6, "Longitude": 11.6, "Geometry": "POINT(12.0 47.0)" }
                    }
                },
                { 
                    "DeprecatedSource": "lts B",
                    "NestedDeprecatedDetail": {
                        "en": { "BaseText": "Nested ES.", "Language": "en" },
                        "it": { "BaseText": "Nested PT.", "Language": "it" }
                    }
                }
            ],
            "Valid": [
                {
                    "ValidSource": "lts C",
                    "DeprecatedLeaf": "Leaf C is old",
                    "NestedDetail": {
                        "en": { "BaseText": "Nested ES.", "Language": "en" },
                        "it": { "BaseText": "Nested PT.", "Language": "it" }
                    },
                    "NestedDeprecatedLeafDetail": {
                        "nl": { "BaseText": "Nested NL.", "Language": "nl" },
                        "de": { "BaseText": "Nested SV.", "Language": "de" }
                    }
                },
                { 
                    "ValidSource": "lts D",
                    "NestedDetail": {
                        "en": { "BaseText": "Nested DA.", "Language": "en" },
                        "it": { "BaseText": "Nested FI.", "Language": "it" }
                    }
                }
            ]
        },
        "FooDeprecated": { 
            "Deprecated": [
                { "DeprecatedSource": "lts" }, 
                { "DeprecatedSource": "lts" }
            ], 
            "Valid": [
                { "ValidSource": "lts" }, 
                { "ValidSource": "lts" }
            ] 
        },
        "LoreIpsum": [
            { "Valid": [{"ValidSource": "lts"}, {"ValidSource": "lts"}] }
        ],
        "LoreIpsumDeprecated": [
            { "Valid": [{"ValidSource": "lts"}, {"ValidSource": "lts"}] }
        ],
        "Geo": {
            "primary_pos": { "Gpstype": "position", "Latitude": 46.4, "Longitude": 11.3, "Default": true, "Geometry": "POINT(12.0 47.0)" },
            "secondary_pos": { "Gpstype": "halfwaypoint", "Latitude": 46.7, "Longitude": 11.7, "Geometry": "POINT(12.0 47.0)" }
        },
        "Id": "test-id-002",
        "HasLanguage": ["en", "de"],
        "TagIds": ["sample", "full-data"],
        "Source": "lts-A",
        "Mapping": {
            "external_id": {
                "system_a": "12345",
                "system_b": "67890"
            }
        },
        "AdditionalProperties": {
            "TestadataProperties": {
                "Valid": [
                    { "ValidSource": "lts-1" },
                    { "ValidSource": "lts-2" }
                ]
            }
        },
		"LicenseInfo": {
            "ClosedData": false
        }
    }
    '::jsonb
),
(
    'test-id-003', -- Entry 2
    '
    {
		"Id": "test-id-003",
		"Type": "Testdata",
        "Active": false,
		"_Meta": {
			"Reduced": false,
			"Id": "test-id-003",
			"Type": "Testdata"
		},
        "Detail": {
            "en": { "Title": "Title Two EN.", "Language": "en" },
            "it": { "Title": "Title Two IT.", "Language": "it" }
        },
        "RelatedContent": [ { "Id": "rel-003" }, { "Id": "rel-004" } ],
        "Foo": {
            "Deprecated": [ { "DeprecatedSource": "lts" }, { "DeprecatedSource": "lts" } ],
            "Valid": [ { "ValidSource": "lts" }, { "ValidSource": "lts" } ]
        },
        "FooDeprecated": { 
            "Deprecated": [
                { "DeprecatedSource": "lts" }, 
                { "DeprecatedSource": "lts" }
            ], 
            "Valid": [
                { "ValidSource": "lts" }, 
                { "ValidSource": "lts" }
            ] 
        },
        "LoreIpsum": [
            { "Valid": [{"ValidSource": "lts"}, {"ValidSource": "lts"}] }
        ],
        "LoreIpsumDeprecated": [
            { "Valid": [{"ValidSource": "lts"}, {"ValidSource": "lts"}] }
        ],
        "Geo": {
            "pos_a": { "Gpstype": "viewpoint", "Latitude": 47.0, "Longitude": 12.0, "Geometry": "POINT(12.0 47.0)" },
            "pos_b": { "Gpstype": "carparking", "Latitude": 47.1, "Longitude": 12.1, "Geometry": "POINT(12.0 47.0)" }
        },
        "Id": "test-id-003",
        "HasLanguage": ["en", "it"],
        "TagIds": ["test-two", "minimal"],
        "Source": "lts-B",
        "Mapping": {
            "external_name": {
                "system_x": "Name_3",
                "system_y": "Name_C"
            }
        },
        "AdditionalProperties": {
            "TestadataProperties": {
                "Valid": [
                    { "ValidSource": "lts-3" },
                    { "ValidSource": "lts-4" }
                ]
            }
        },
		"LicenseInfo": {
            "ClosedData": false
        }
    }
    '::jsonb
),
(
    'test-id-004', -- Entry 3
    '
    {
		"Id": "test-id-004",
		"Type": "Testdata",
        "Active": true,
		"_Meta": {
			"Reduced": false,
			"Id": "test-id-004",
			"Type": "Testdata"
		},
        "Detail": {
            "fr": { "BaseText": "Base FR.", "Language": "fr" },
            "en": { "BaseText": "Base ES.", "Language": "en" }
        },
        "RelatedContent": [ { "Id": "rel-005" }, { "Id": "rel-006" } ],
        "Foo": {
            "Deprecated": [ { "DeprecatedSource": "lts" }, { "DeprecatedSource": "lts" } ],
            "Valid": [ { "ValidSource": "lts" }, { "ValidSource": "lts" } ]
        },
        "FooDeprecated": { 
            "Deprecated": [
                { "DeprecatedSource": "lts" }, 
                { "DeprecatedSource": "lts" }
            ], 
            "Valid": [
                { "ValidSource": "lts" }, 
                { "ValidSource": "lts" }
            ] 
        },
        "LoreIpsum": [
            { "Valid": [{"ValidSource": "lts"}, {"ValidSource": "lts"}] }
        ],
        "LoreIpsumDeprecated": [
            { "Valid": [{"ValidSource": "lts"}, {"ValidSource": "lts"}] }
        ],
        "Geo": {
            "pos_c": { "Gpstype": "startingpoint", "Latitude": 45.0, "Longitude": 10.0, "Geometry": "POINT(12.0 47.0)" },
            "pos_d": { "Gpstype": "arrivalpoint", "Latitude": 45.1, "Longitude": 10.1, "Geometry": "POINT(12.0 47.0)" }
        },
        "Id": "test-id-004",
        "HasLanguage": ["fr", "en"],
        "TagIds": ["third", "entry"],
        "Source": "lts-C",
        "Mapping": {
            "reference_key": {
                "crm": "ABC-456",
                "cms": "XYZ-789"
            }
        },
        "AdditionalProperties": {
            "TestadataProperties": {
                "Valid": [
                    { "ValidSource": "lts-5" },
                    { "ValidSource": "lts-6" }
                ]
            }
        },
		"LicenseInfo": {
            "ClosedData": false
        }
    }
    '::jsonb
),
(
    'test-id-005', -- Entry 4
    '
    {
		"Id": "test-id-005",
		"Type": "Testdata",
        "Active": true,
		"_Meta": {
			"Reduced": false,
			"Id": "test-id-005",
			"Type": "Testdata"
		},
        "Detail": {
            "nl": { "Title": "Title NL.", "Language": "nl" },
            "de": { "Title": "Title SV.", "Language": "de" }
        },
        "RelatedContent": [ { "Id": "rel-007" }, { "Id": "rel-008" } ],
        "Foo": {
            "Deprecated": [ { "DeprecatedSource": "lts" }, { "DeprecatedSource": "lts" } ],
            "Valid": [ { "ValidSource": "lts" }, { "ValidSource": "lts" } ]
        },
        "FooDeprecated": { 
            "Deprecated": [
                { "DeprecatedSource": "lts" }, 
                { "DeprecatedSource": "lts" }
            ], 
            "Valid": [
                { "ValidSource": "lts" }, 
                { "ValidSource": "lts" }
            ] 
        },
        "LoreIpsum": [
            { "Valid": [{"ValidSource": "lts"}, {"ValidSource": "lts"}] }
        ],
        "LoreIpsumDeprecated": [
            { "Valid": [{"ValidSource": "lts"}, {"ValidSource": "lts"}] }
        ],
        "Geo": {
            "pos_e": { "Gpstype": "viewpoint", "Latitude": 48.0, "Longitude": 13.0, "Geometry": "POINT(12.0 47.0)" },
            "pos_f": { "Gpstype": "viewpoint", "Latitude": 48.1, "Longitude": 13.1, "Geometry": "POINT(12.0 47.0)" }
        },
        "Id": "test-id-005",
        "HasLanguage": ["nl", "de"],
        "TagIds": ["fourth", "test"],
        "Source": "lts-D",
        "Mapping": {
            "legacy_code": {
                "old_sys_z": "CodeZ1",
                "old_sys_w": "CodeW2"
            }
        },
        "AdditionalProperties": {
            "TestadataProperties": {
                "Valid": [
                    { "ValidSource": "lts-7" },
                    { "ValidSource": "lts-8" }
                ]
            }
        },
		"LicenseInfo": {
            "ClosedData": false
        }
    }
    '::jsonb
);