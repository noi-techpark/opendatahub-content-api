-- SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
--
-- SPDX-License-Identifier: AGPL-3.0-or-later

-- Populate TrafficIncidents table with test data
-- This script inserts sample traffic incident data following the model definition

-- Sample traffic incident 1: Road accident on highway
INSERT INTO public.trafficincidents (id, data) VALUES (
    'INCIDENT_001',
    '{
        "Id": "INCIDENT_001",
        "Active": true,
        "SmgActive": true,
        "Source": "api",
        "FirstImport": "2024-01-15T08:30:00.000Z",
        "LastChange": "2024-01-15T09:15:00.000Z",
        "IncidentType": "accident",
        "Severity": "High",
        "Status": "Active",
        "StartTime": "2024-01-15T08:30:00.000Z",
        "EndTime": "2024-01-15T12:00:00.000Z",
        "EstimatedResolution": "2024-01-15T12:00:00.000Z",
        "RoadClosure": true,
        "AffectedRoutes": ["A22", "SS12"],
        "HasLanguage": ["de", "it", "en"],
        "SmgTags": ["highway", "accident", "traffic"],
        "Detail": {
            "de": {
                "Title": "Verkehrsunfall auf der A22",
                "BaseText": "Schwerer Verkehrsunfall zwischen Bozen Süd und Bozen Nord. Autobahn gesperrt.",
                "AdditionalText": "Umleitung über die Staatsstraße empfohlen."
            },
            "it": {
                "Title": "Incidente stradale sulla A22",
                "BaseText": "Grave incidente stradale tra Bolzano Sud e Bolzano Nord. Autostrada chiusa.",
                "AdditionalText": "Si consiglia deviazione tramite strada statale."
            },
            "en": {
                "Title": "Traffic accident on A22",
                "BaseText": "Serious traffic accident between Bolzano South and Bolzano North. Highway closed.",
                "AdditionalText": "Detour via state road recommended."
            }
        },
        "ContactInfos": {
            "Address": "A22 Autostrada del Brennero",
            "City": "Bolzano",
            "ZipCode": "39100",
            "CountryCode": "IT",
            "Phonenumber": "+39 0471 123456",
            "Email": "info@autostrada.it"
        },
        "DetourInfo": {
            "de": "Umleitung über SS12 - Verlängerte Fahrtzeit ca. 30 Minuten",
            "it": "Deviazione tramite SS12 - Tempo di percorrenza aggiuntivo circa 30 minuti",
            "en": "Detour via SS12 - Additional travel time about 30 minutes"
        },
        "Geometry": {
            "type": "LineString",
            "coordinates": [[11.3547, 46.4983], [11.3567, 46.5001], [11.3587, 46.5019]]
        },
        "_Meta": {
            "Type": "trafficincident",
            "LastUpdate": "2024-01-15T09:15:00.000Z",
            "Source": "api",
            "Reduced": false
        },
        "LicenseInfo": {
            "License": "CC-BY",
            "LicenseHolder": "Provincia Autonoma di Bolzano",
            "Author": "Traffic Control Center",
            "ClosedData": false
        },
        "Mapping": {}
    }'::jsonb
);

-- Sample traffic incident 2: Road construction
INSERT INTO public.trafficincidents (id, data) VALUES (
    'INCIDENT_002',
    '{
        "Id": "INCIDENT_002",
        "Active": true,
        "SmgActive": true,
        "Source": "municipality",
        "FirstImport": "2024-01-10T06:00:00.000Z",
        "LastChange": "2024-01-15T07:00:00.000Z",
        "IncidentType": "construction",
        "Severity": "Medium",
        "Status": "Active",
        "StartTime": "2024-01-10T06:00:00.000Z",
        "EndTime": "2024-02-28T18:00:00.000Z",
        "EstimatedResolution": "2024-02-28T18:00:00.000Z",
        "RoadClosure": false,
        "AffectedRoutes": ["Via Roma", "Via Museo"],
        "HasLanguage": ["de", "it"],
        "SmgTags": ["construction", "city", "longterm"],
        "Detail": {
            "de": {
                "Title": "Straßenarbeiten in der Via Roma",
                "BaseText": "Sanierungsarbeiten der Fahrbahn. Einspurig befahrbar.",
                "AdditionalText": "Arbeiten finden von Montag bis Freitag 06:00-18:00 statt."
            },
            "it": {
                "Title": "Lavori stradali in Via Roma",
                "BaseText": "Lavori di risanamento del manto stradale. Percorribile a senso unico alternato.",
                "AdditionalText": "I lavori si svolgono dal lunedì al venerdì dalle 06:00 alle 18:00."
            }
        },
        "ContactInfos": {
            "Address": "Via Roma 15",
            "City": "Bolzano",
            "ZipCode": "39100",
            "CountryCode": "IT",
            "Phonenumber": "+39 0471 997111",
            "Email": "lavori@comune.bolzano.it"
        },
        "DetourInfo": {
            "de": "Alternative Routen: Via Museo oder Via del Parco",
            "it": "Percorsi alternativi: Via Museo o Via del Parco"
        },
        "Geometry": {
            "type": "LineString",
            "coordinates": [[11.3496, 46.4978], [11.3506, 46.4988], [11.3516, 46.4998]]
        },
        "_Meta": {
            "Type": "trafficincident",
            "LastUpdate": "2024-01-15T07:00:00.000Z",
            "Source": "municipality",
            "Reduced": false
        },
        "LicenseInfo": {
            "License": "CC0",
            "LicenseHolder": "Comune di Bolzano",
            "Author": "Ufficio Tecnico Comunale",
            "ClosedData": false
        },
        "Mapping": {}
    }'::jsonb
);

-- Sample traffic incident 3: Weather-related incident
INSERT INTO public.trafficincidents (id, data) VALUES (
    'INCIDENT_003',
    '{
        "Id": "INCIDENT_003",
        "Active": true,
        "SmgActive": false,
        "Source": "weather_service",
        "FirstImport": "2024-01-15T14:30:00.000Z",
        "LastChange": "2024-01-15T15:00:00.000Z",
        "IncidentType": "weather",
        "Severity": "Critical",
        "Status": "Investigating",
        "StartTime": "2024-01-15T14:30:00.000Z",
        "EndTime": null,
        "EstimatedResolution": "2024-01-15T18:00:00.000Z",
        "RoadClosure": true,
        "AffectedRoutes": ["SS240", "SP117"],
        "HasLanguage": ["de", "it", "en"],
        "SmgTags": ["weather", "snow", "mountain"],
        "Detail": {
            "de": {
                "Title": "Schneeverwehungen auf der Passo Sella",
                "BaseText": "Starke Schneefälle und Verwehungen. Straße gesperrt.",
                "AdditionalText": "Schneeketten obligatorisch auf alternativen Routen."
            },
            "it": {
                "Title": "Accumuli di neve sul Passo Sella",
                "BaseText": "Forti nevicate e accumuli. Strada chiusa.",
                "AdditionalText": "Catene da neve obbligatorie sui percorsi alternativi."
            },
            "en": {
                "Title": "Snow drifts on Sella Pass",
                "BaseText": "Heavy snowfall and drifts. Road closed.",
                "AdditionalText": "Snow chains mandatory on alternative routes."
            }
        },
        "ContactInfos": {
            "Address": "Passo Sella",
            "City": "Selva di Val Gardena",
            "ZipCode": "39048",
            "CountryCode": "IT",
            "Phonenumber": "+39 0471 777888",
            "Email": "info@meteotrentino.it"
        },
        "DetourInfo": {
            "de": "Umleitung über Passo Pordoi möglich (mit Schneeketten)",
            "it": "Deviazione possibile tramite Passo Pordoi (con catene)",
            "en": "Detour possible via Passo Pordoi (with snow chains)"
        },
        "Geometry": {
            "type": "LineString",
            "coordinates": [[11.7581, 46.5089], [11.7601, 46.5109], [11.7621, 46.5129]]
        },
        "_Meta": {
            "Type": "trafficincident",
            "LastUpdate": "2024-01-15T15:00:00.000Z",
            "Source": "weather_service",
            "Reduced": true
        },
        "LicenseInfo": {
            "License": "CC-BY",
            "LicenseHolder": "Servizio Meteorologico",
            "Author": "Weather Service",
            "ClosedData": false
        },
        "Mapping": {}
    }'::jsonb
);

-- Sample traffic incident 4: Resolved incident
INSERT INTO public.trafficincidents (id, data) VALUES (
    'INCIDENT_004',
    '{
        "Id": "INCIDENT_004",
        "Active": false,
        "SmgActive": false,
        "Source": "police",
        "FirstImport": "2024-01-14T16:45:00.000Z",
        "LastChange": "2024-01-14T18:30:00.000Z",
        "IncidentType": "closure",
        "Severity": "Low",
        "Status": "Resolved",
        "StartTime": "2024-01-14T16:45:00.000Z",
        "EndTime": "2024-01-14T18:30:00.000Z",
        "EstimatedResolution": "2024-01-14T18:00:00.000Z",
        "RoadClosure": false,
        "AffectedRoutes": ["Via Portici"],
        "HasLanguage": ["de", "it"],
        "SmgTags": ["event", "resolved", "city"],
        "Detail": {
            "de": {
                "Title": "Straßensperrung für Veranstaltung - BEENDET",
                "BaseText": "Straßensperrung für kulturelle Veranstaltung wurde aufgehoben.",
                "AdditionalText": "Normale Verkehrssituation wiederhergestellt."
            },
            "it": {
                "Title": "Chiusura stradale per evento - CONCLUSA",
                "BaseText": "Chiusura stradale per evento culturale è stata rimossa.",
                "AdditionalText": "Situazione del traffico normalizzata."
            }
        },
        "ContactInfos": {
            "Address": "Via Portici",
            "City": "Bolzano",
            "ZipCode": "39100",
            "CountryCode": "IT",
            "Phonenumber": "+39 0471 997700",
            "Email": "eventi@comune.bolzano.it"
        },
        "DetourInfo": {},
        "Geometry": {
            "type": "Point",
            "coordinates": [11.3539, 46.4983]
        },
        "_Meta": {
            "Type": "trafficincident",
            "LastUpdate": "2024-01-14T18:30:00.000Z",
            "Source": "police",
            "Reduced": false
        },
        "LicenseInfo": {
            "License": "CC0",
            "LicenseHolder": "Polizia Locale Bolzano",
            "Author": "Traffic Police",
            "ClosedData": false
        },
        "Mapping": {}
    }'::jsonb
);

-- Sample traffic incident 5: Highway maintenance with special access rules
INSERT INTO public.trafficincidents (id, data) VALUES (
    'INCIDENT_005',
    '{
        "Id": "INCIDENT_005",
        "Active": true,
        "SmgActive": true,
        "Source": "lts",
        "FirstImport": "2024-01-12T20:00:00.000Z",
        "LastChange": "2024-01-15T06:00:00.000Z",
        "IncidentType": "maintenance",
        "Severity": "Medium",
        "Status": "Active",
        "StartTime": "2024-01-12T22:00:00.000Z",
        "EndTime": "2024-01-16T06:00:00.000Z",
        "EstimatedResolution": "2024-01-16T06:00:00.000Z",
        "RoadClosure": false,
        "AffectedRoutes": ["A22", "E45"],
        "HasLanguage": ["de", "it", "en"],
        "SmgTags": ["maintenance", "highway", "nightwork"],
        "Detail": {
            "de": {
                "Title": "Nachtarbeiten auf der A22",
                "BaseText": "Wartungsarbeiten an der Fahrbahndecke. Eingeschränkter Verkehr.",
                "AdditionalText": "Arbeiten von 22:00 bis 06:00 Uhr. Eine Spur gesperrt."
            },
            "it": {
                "Title": "Lavori notturni sulla A22",
                "BaseText": "Lavori di manutenzione del manto stradale. Traffico limitato.",
                "AdditionalText": "Lavori dalle 22:00 alle 06:00. Una corsia chiusa."
            },
            "en": {
                "Title": "Night works on A22",
                "BaseText": "Road surface maintenance works. Limited traffic.",
                "AdditionalText": "Works from 22:00 to 06:00. One lane closed."
            }
        },
        "ContactInfos": {
            "Address": "A22 Brenner Autobahn",
            "City": "Trento",
            "ZipCode": "38122",
            "CountryCode": "IT",
            "Phonenumber": "+39 0461 000111",
            "Email": "manutenzione@autobrennero.it"
        },
        "DetourInfo": {
            "de": "Reduzierte Geschwindigkeit 60 km/h im Arbeitsbereich",
            "it": "Velocità ridotta 60 km/h nella zona di lavoro",
            "en": "Reduced speed 60 km/h in work zone"
        },
        "Geometry": {
            "type": "LineString",
            "coordinates": [[11.1186, 46.0748], [11.1206, 46.0768], [11.1226, 46.0788], [11.1246, 46.0808]]
        },
        "_Meta": {
            "Type": "trafficincident",
            "LastUpdate": "2024-01-15T06:00:00.000Z",
            "Source": "lts",
            "Reduced": true
        },
        "LicenseInfo": {
            "License": "Closed",
            "LicenseHolder": "LTS Traffic Service",
            "Author": "LTS System",
            "ClosedData": true
        },
        "Mapping": {},
        "OdhActive": {
            "Active": true,
            "Access": ["IDM"]
        }
    }'::jsonb
);

-- Sample traffic incident 6: Multi-polygon area incident
INSERT INTO public.trafficincidents (id, data) VALUES (
    'INCIDENT_006',
    '{
        "Id": "INCIDENT_006",
        "Active": true,
        "SmgActive": true,
        "Source": "api",
        "FirstImport": "2024-01-15T10:00:00.000Z",
        "LastChange": "2024-01-15T11:30:00.000Z",
        "IncidentType": "event",
        "Severity": "Low",
        "Status": "Active",
        "StartTime": "2024-01-15T09:00:00.000Z",
        "EndTime": "2024-01-15T18:00:00.000Z",
        "EstimatedResolution": "2024-01-15T18:00:00.000Z",
        "RoadClosure": false,
        "AffectedRoutes": ["Via dei Giardini", "Piazza Walther", "Via del Museo"],
        "HasLanguage": ["de", "it", "en", "nl"],
        "SmgTags": ["event", "festival", "city", "tourism"],
        "Detail": {
            "de": {
                "Title": "Weihnachtsmarkt - Verkehrseinschränkungen",
                "BaseText": "Traditioneller Weihnachtsmarkt auf dem Waltherplatz. Eingeschränkter Verkehr im Zentrum.",
                "AdditionalText": "Parkmöglichkeiten in den Parkhäusern außerhalb des Zentrums."
            },
            "it": {
                "Title": "Mercatini di Natale - Limitazioni traffico",
                "BaseText": "Mercatini di Natale tradizionali in Piazza Walther. Traffico limitato in centro.",
                "AdditionalText": "Parcheggi disponibili nei parcheggi fuori dal centro."
            },
            "en": {
                "Title": "Christmas Market - Traffic restrictions",
                "BaseText": "Traditional Christmas market in Walther Square. Limited traffic in city center.",
                "AdditionalText": "Parking available in parking garages outside the center."
            },
            "nl": {
                "Title": "Kerstmarkt - Verkeersbeperking",
                "BaseText": "Traditionele kerstmarkt op Waltherplein. Beperkt verkeer in het centrum.",
                "AdditionalText": "Parkeren mogelijk in parkeergarages buiten het centrum."
            }
        },
        "ContactInfos": {
            "Address": "Piazza Walther",
            "City": "Bolzano",
            "ZipCode": "39100",
            "CountryCode": "IT",
            "Phonenumber": "+39 0471 307000",
            "Email": "mercatini@bolzano.info"
        },
        "DetourInfo": {
            "de": "Umfahrung über Ring-Allee empfohlen",
            "it": "Si consiglia circonvallazione tramite Viale della Circonvallazione",
            "en": "Bypass via Ring Road recommended",
            "nl": "Omleiding via Ringweg aanbevolen"
        },
        "Geometry": {
            "type": "Polygon",
            "coordinates": [[[11.3528, 46.4970], [11.3548, 46.4970], [11.3548, 46.4990], [11.3528, 46.4990], [11.3528, 46.4970]]]
        },
        "_Meta": {
            "Type": "trafficincident",
            "LastUpdate": "2024-01-15T11:30:00.000Z",
            "Source": "api",
            "Reduced": false
        },
        "LicenseInfo": {
            "License": "CC-BY",
            "LicenseHolder": "Azienda di Soggiorno e Turismo",
            "Author": "Tourism Office",
            "ClosedData": false
        },
        "Mapping": {},
        "PublishedOn": ["idm-marketplace", "sta-portal"]
    }'::jsonb
);

-- Verify the inserted data and generated columns
SELECT 
    id,
    gen_active,
    gen_smgactive,
    gen_source,
    gen_incidenttype,
    gen_severity,
    gen_status,
    gen_starttime,
    gen_endtime,
    gen_lastchange,
    gen_roadclosure,
    gen_languages,
    gen_affectedroutes,
    gen_smgtags,
    ST_AsText(gen_geometry) as geometry_wkt
FROM public.trafficincidents
ORDER BY id;