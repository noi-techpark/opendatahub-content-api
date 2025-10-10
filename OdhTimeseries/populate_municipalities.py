#!/usr/bin/env python3
"""
Municipality Database Population Script for Timeseries API

This script populates the database with Italian municipality sensors:
- Sensors represent municipalities with polygon geometries (not points)
- Loads geometries from limits_IT_municipalities.geojson
- Creates timeseries for temperature and pollution measurements
- Generates correlated/realistic measurements (each value depends on previous)

Usage:
    python populate_municipalities.py [--clean] [--days 30] [--limit 100]
"""

import os
import sys
import json
import random
import argparse
from datetime import datetime, timedelta, timezone
from typing import List, Dict, Any, Optional, Tuple
import uuid

import psycopg
from dotenv import load_dotenv

# Load environment variables from .env file
load_dotenv()

# Configuration
DB_CONFIG = {
    'host': os.getenv('DB_HOST', 'localhost'),
    'port': os.getenv('DB_PORT', '5556'),
    'dbname': os.getenv('DB_NAME', 'timeseries'),
    'user': os.getenv('DB_USER', 'bdp'),
    'password': os.getenv('DB_PASSWORD', 'password'),
}

# Configuration for content database (OdhApiCore database)
CONTENT_DB_CONFIG = {
    'host': os.getenv('CONTENT_DB_HOST', 'localhost'),
    'port': os.getenv('CONTENT_DB_PORT', '5432'),
    'dbname': os.getenv('CONTENT_DB_NAME', 'postgres'),
    'user': os.getenv('CONTENT_DB_USER', 'postgres'),
    'password': os.getenv('CONTENT_DB_PASSWORD', 'your_password'),
}

SCHEMA = os.getenv('DB_SCHEMA', 'intimev3')

GEOJSON_FILE = 'limits_IT_provinces.geojson'


class MunicipalityPopulator:
    def __init__(self, batch_size: int = 1000):
        self.batch_size = batch_size
        self.conn = None
        self.cursor = None
        self.content_conn = None
        self.content_cursor = None

        # Data containers
        self.provenance_id = None
        self.sensor_ids = []
        self.sensor_urns = []
        self.type_ids = {}  # name -> id mapping
        self.dataset_id = None
        self.timeseries_map = {}  # sensor_id -> {temperature_ts_id, pollution_ts_id}
        self.municipalities = []  # List of municipality data from GeoJSON

    def connect(self):
        """Connect to PostgreSQL databases"""
        try:
            # Connect to timeseries database
            self.conn = psycopg.connect(**DB_CONFIG)
            self.cursor = self.conn.cursor(row_factory=psycopg.rows.dict_row)
            print(f"✅ Connected to timeseries database: {DB_CONFIG['dbname']}")

            # Connect to content database
            self.content_conn = psycopg.connect(**CONTENT_DB_CONFIG)
            self.content_cursor = self.content_conn.cursor(row_factory=psycopg.rows.dict_row)
            print(f"✅ Connected to content database: {CONTENT_DB_CONFIG['dbname']}")
        except Exception as e:
            print(f"❌ Database connection failed: {e}")
            sys.exit(1)

    def disconnect(self):
        """Close database connections"""
        if self.cursor:
            self.cursor.close()
        if self.conn:
            self.conn.close()
        if self.content_cursor:
            self.content_cursor.close()
        if self.content_conn:
            self.content_conn.close()
        print("✅ Database connections closed")

    def load_geojson(self, limit: Optional[int] = None):
        """Load municipalities from GeoJSON file"""
        print(f"📂 Loading municipalities from {GEOJSON_FILE}...")

        if not os.path.exists(GEOJSON_FILE):
            print(f"❌ GeoJSON file not found: {GEOJSON_FILE}")
            sys.exit(1)

        try:
            with open(GEOJSON_FILE, 'r', encoding='utf-8') as f:
                geojson_data = json.load(f)

            features = geojson_data.get('features', [])

            if limit:
                features = features[:limit]
                print(f"  ⚠️  Limiting to first {limit} municipalities")

            for feature in features:
                props = feature.get('properties', {})
                geom = feature.get('geometry', {})

                municipality = {
                    'name': props.get('prov_name', 'Unknown'),
                    'province': props.get('prov_name', 'Unknown'),
                    'province_code': props.get('prov_acr'),
                    'region': props.get('reg_name', 'Unknown'),
                    'geometry': geom
                }

                self.municipalities.append(municipality)

            print(f"✅ Loaded {len(self.municipalities)} municipalities")

        except Exception as e:
            print(f"❌ Error loading GeoJSON: {e}")
            sys.exit(1)

    def clean_municipality_data(self):
        """Clean existing municipality data"""
        print("🧹 Cleaning existing municipality data...")

        # Delete measurements for municipality sensors
        try:
            self.cursor.execute(f"""
                DELETE FROM {SCHEMA}.measurements_numeric
                WHERE timeseries_id IN (
                    SELECT ts.id FROM {SCHEMA}.timeseries ts
                    JOIN {SCHEMA}.sensors s ON ts.sensor_id = s.id
                    WHERE s.name LIKE 'urn:odh:municipality:%'
                )
            """)
            print(f"  🗑️  Cleaned municipality numeric measurements")

            self.cursor.execute(f"""
                DELETE FROM {SCHEMA}.timeseries
                WHERE sensor_id IN (
                    SELECT id FROM {SCHEMA}.sensors
                    WHERE name LIKE 'urn:odh:municipality:%'
                )
            """)
            print(f"  🗑️  Cleaned municipality timeseries")

            self.cursor.execute(f"""
                DELETE FROM {SCHEMA}.sensors
                WHERE name LIKE 'urn:odh:municipality:%'
            """)
            print(f"  🗑️  Cleaned municipality sensors")

            # Clean from content database
            self.content_cursor.execute("""
                DELETE FROM public.sensors
                WHERE id LIKE 'urn:odh:municipality:%'
            """)
            print(f"  🗑️  Cleaned municipality sensors from content DB")

            self.conn.commit()
            self.content_conn.commit()
            print("✅ Municipality data cleaned successfully")

        except Exception as e:
            print(f"⚠️  Warning during cleanup: {e}")
            self.conn.rollback()
            self.content_conn.rollback()

    def create_provenance(self):
        """Create or get provenance record for municipalities"""
        print("📋 Creating provenance record...")

        provenance_uuid = str(uuid.uuid4())
        lineage = "italian_municipalities"
        collector = "MunicipalityCollector"
        version = "v1.0.0"

        self.cursor.execute(f"""
            INSERT INTO {SCHEMA}.provenance (uuid, lineage, data_collector, data_collector_version)
            VALUES (%s, %s, %s, %s)
            ON CONFLICT (lineage, data_collector, data_collector_version) DO UPDATE SET
                uuid = EXCLUDED.uuid
            RETURNING id
        """, (provenance_uuid, lineage, collector, version))

        self.provenance_id = self.cursor.fetchone()['id']
        self.conn.commit()
        print(f"✅ Created provenance record (id: {self.provenance_id})")

    def create_or_get_types(self):
        """Create or get measurement types for temperature and pollution"""
        print("📊 Creating/retrieving measurement types...")

        types_to_create = [
            ("air_temperature", "Air Temperature", "°C", "numeric"),
            ("pollution_level", "Air Pollution Level (AQI)", "AQI", "numeric")
        ]

        for name, description, unit, data_type in types_to_create:
            metadata = {
                "category": "environmental",
                "precision": 2,
                "sampling_rate": "1hour"
            }

            self.cursor.execute(f"""
                INSERT INTO {SCHEMA}.types (name, description, unit, data_type, metadata)
                VALUES (%s, %s, %s, %s, %s)
                ON CONFLICT (name) DO UPDATE SET
                    description = EXCLUDED.description,
                    unit = EXCLUDED.unit,
                    data_type = EXCLUDED.data_type,
                    metadata = EXCLUDED.metadata
                RETURNING id
            """, (name, description, unit, data_type, json.dumps(metadata)))

            self.type_ids[name] = self.cursor.fetchone()['id']

        self.conn.commit()
        print(f"✅ Created/retrieved {len(self.type_ids)} measurement types")

    def create_dataset(self):
        """Create dataset for municipality monitoring"""
        print("📦 Creating municipality dataset...")

        dataset_name = "italian_municipality_monitoring"
        dataset_description = "Environmental monitoring data aggregated by Italian municipalities"

        self.cursor.execute(f"""
            INSERT INTO {SCHEMA}.datasets (name, description)
            VALUES (%s, %s)
            ON CONFLICT (name) DO UPDATE SET
                description = EXCLUDED.description
            RETURNING id
        """, (dataset_name, dataset_description))

        self.dataset_id = self.cursor.fetchone()['id']

        # Associate types with dataset
        for type_name in ["air_temperature", "pollution_level"]:
            self.cursor.execute(f"""
                INSERT INTO {SCHEMA}.dataset_types (dataset_id, type_id, is_required)
                VALUES (%s, %s, %s)
                ON CONFLICT (dataset_id, type_id) DO NOTHING
            """, (self.dataset_id, self.type_ids[type_name], True))

        self.conn.commit()
        print(f"✅ Created dataset (id: {self.dataset_id})")

    def _geometry_to_wkt(self, geometry: dict) -> Tuple[str, float, float]:
        """
        Convert GeoJSON geometry to WKT format and calculate centroid
        Returns: (wkt_string, center_lat, center_lon)
        """
        geom_type = geometry.get('type')
        coordinates = geometry.get('coordinates', [])

        if geom_type == 'Polygon':
            # Polygon has one ring (outer boundary)
            ring = coordinates[0]
            wkt = self._polygon_ring_to_wkt(ring)
            center_lat, center_lon = self._calculate_centroid(ring)
            return f"POLYGON(({wkt}))", center_lat, center_lon

        elif geom_type == 'MultiPolygon':
            # MultiPolygon has multiple polygons
            # We'll use the first (largest) polygon for simplicity
            if coordinates:
                ring = coordinates[0][0]  # First polygon, outer ring
                wkt = self._polygon_ring_to_wkt(ring)
                center_lat, center_lon = self._calculate_centroid(ring)
                return f"POLYGON(({wkt}))", center_lat, center_lon

        # Fallback for unsupported types
        return "POLYGON((0 0, 1 0, 1 1, 0 1, 0 0))", 0.0, 0.0

    def _polygon_ring_to_wkt(self, ring: List) -> str:
        """Convert a polygon ring (list of [lon, lat] pairs) to WKT format"""
        wkt_points = []
        for coord in ring:
            lon, lat = coord[0], coord[1]
            wkt_points.append(f"{lon} {lat}")
        return ", ".join(wkt_points)

    def _calculate_centroid(self, ring: List) -> Tuple[float, float]:
        """Calculate centroid of a polygon ring"""
        if not ring:
            return 0.0, 0.0

        lons = [coord[0] for coord in ring]
        lats = [coord[1] for coord in ring]

        center_lon = sum(lons) / len(lons)
        center_lat = sum(lats) / len(lats)

        return center_lat, center_lon

    def populate_municipality_sensors(self):
        """Populate sensors for Italian municipalities from GeoJSON"""
        print(f"🏛️  Creating {len(self.municipalities)} municipality sensors...")

        for idx, muni in enumerate(self.municipalities):
            if (idx + 1) % 100 == 0:
                print(f"  📍 Processing {idx + 1}/{len(self.municipalities)}...")

            # Create URN format: urn:odh:municipality:<province_code>
            # Use ISTAT code as unique identifier
            province_code = muni['province_code'].replace('/', '_')
            sensor_urn = f"urn:odh:municipality:{province_code}"

            # Convert geometry to WKT and get centroid
            polygon_wkt, center_lat, center_lon = self._geometry_to_wkt(muni['geometry'])

            # Create metadata with Geometry field for gen_position
            metadata = {
                "type": "municipality",
                "province": muni['province'],
                "province_code": muni['province_code'],
                "region": muni['region'],
                "country": "Italy",
                "province_code": muni['province_code'],
                "Geometry": polygon_wkt,  # WKT geometry for gen_position
                "center_coordinates": {
                    "lat": center_lat,
                    "lon": center_lon
                },
                "data_collection_start": "2024-01-01"
            }

            # Insert into timeseries database
            self.cursor.execute(f"""
                INSERT INTO {SCHEMA}.sensors (name, parent_id, metadata)
                VALUES (%s, NULL, %s) RETURNING id
            """, (sensor_urn, json.dumps(metadata)))

            sensor_id = self.cursor.fetchone()['id']
            self.sensor_ids.append(sensor_id)
            self.sensor_urns.append(sensor_urn)

            # Insert into content database
            self._insert_municipality_to_content_db(
                sensor_urn=sensor_urn,
                muni=muni,
                metadata=metadata,
                polygon_wkt=polygon_wkt,
                center_lat=center_lat,
                center_lon=center_lon
            )

            # Commit in batches to avoid memory issues
            if (idx + 1) % 100 == 0:
                self.conn.commit()

        self.conn.commit()
        print(f"✅ Created {len(self.sensor_ids)} municipality sensors")

    def _insert_municipality_to_content_db(self, sensor_urn: str, muni: dict, metadata: dict,
                                          polygon_wkt: str, center_lat: float, center_lon: float):
        """Insert municipality sensor into content database"""
        try:
            sensor_data = {
                "Id": sensor_urn,
                "Active": True,
                "SmgActive": True,
                "_Meta": {
                    "Type": "sensor",
                    "LastUpdate": datetime.now(timezone.utc).isoformat(),
                    "Source": "municipality_populate_script"
                },
                "LicenseInfo": {
                    "Author": "Open Data Hub",
                    "License": "CC0",
                    "ClosedData": False
                },
                "Source": "municipality_populate_script",
                "FirstImport": datetime.now(timezone.utc).isoformat(),
                "LastChange": datetime.now(timezone.utc).isoformat(),
                "SensorType": "MUNICIPALITY",
                "SensorName": muni['name'],
                "Geometry": polygon_wkt,  # WKT geometry for gen_position
                "Latitude": center_lat,
                "Longitude": center_lon,
                "Municipality": muni['name'],
                "Province": muni['province'],
                "Region": muni['region'],
                "Country": "Italy",
                "IstatCode": muni['province_code'],
                "GeometryType": "Polygon",
                "GeometryWKT": polygon_wkt,
                "Detail": {
                    "en": {
                        "Title": f"Municipality of {muni['name']}",
                        "Header": f"Environmental Monitoring - {muni['name']}",
                        "BaseText": f"Aggregated environmental data for {muni['name']}, {muni['province']}, {muni['region']}",
                        "AdditionalText": f"This sensor provides air quality and temperature measurements aggregated across the municipality area.",
                        "GetThereText": f"{muni['name']}, {muni['province']}, {muni['region']}"
                    },
                    "it": {
                        "Title": f"Comune di {muni['name']}",
                        "Header": f"Monitoraggio Ambientale - {muni['name']}",
                        "BaseText": f"Dati ambientali aggregati per {muni['name']}, {muni['province']}, {muni['region']}",
                        "AdditionalText": f"Questo sensore fornisce misurazioni di qualità dell'aria e temperatura aggregate sull'area comunale.",
                        "GetThereText": f"{muni['name']}, {muni['province']}, {muni['region']}"
                    }
                },
                "HasLanguage": ["en", "it"],
                "SmgTags": ["municipality", "environmental", "air_quality", "temperature", muni['name'].lower()],
                "PublishedOn": ["odh"],
                "AdditionalProperties": {
                    "category": "municipality",
                    "monitoring_type": "area_based",
                    "aggregation_level": "municipal"
                }
            }

            self.content_cursor.execute("""
                INSERT INTO public.sensors (id, data)
                VALUES (%s, %s)
                ON CONFLICT (id) DO UPDATE SET data = EXCLUDED.data
            """, (sensor_urn, json.dumps(sensor_data)))

            self.content_conn.commit()
        except Exception as e:
            print(f"⚠️  Warning: Could not insert municipality {sensor_urn} to content database: {e}")
            self.content_conn.rollback()

    def create_timeseries(self):
        """Create timeseries for temperature and pollution for each municipality"""
        print(f"📈 Creating timeseries for {len(self.sensor_ids)} municipalities...")

        for idx, sensor_id in enumerate(self.sensor_ids):
            if (idx + 1) % 100 == 0:
                print(f"  ⏳ Processing {idx + 1}/{len(self.sensor_ids)}...")

            # Create temperature timeseries
            self.cursor.execute(f"""
                INSERT INTO {SCHEMA}.timeseries (sensor_id, type_id)
                VALUES (%s, %s) RETURNING id
            """, (sensor_id, self.type_ids["air_temperature"]))
            temp_ts_id = self.cursor.fetchone()['id']

            # Create pollution timeseries
            self.cursor.execute(f"""
                INSERT INTO {SCHEMA}.timeseries (sensor_id, type_id)
                VALUES (%s, %s) RETURNING id
            """, (sensor_id, self.type_ids["pollution_level"]))
            pollution_ts_id = self.cursor.fetchone()['id']

            self.timeseries_map[sensor_id] = {
                'temperature': temp_ts_id,
                'pollution': pollution_ts_id
            }

            # Commit in batches
            if (idx + 1) % 100 == 0:
                self.conn.commit()

        self.conn.commit()
        print(f"✅ Created {len(self.sensor_ids) * 2} timeseries")

    def populate_correlated_measurements(self, days_back: int = 30):
        """
        Populate measurements with correlated values.
        Each measurement depends on the previous value + small random variation.
        """
        print(f"📏 Creating correlated measurements for last {days_back} days...")

        end_date = datetime.now(timezone.utc)
        start_date = end_date - timedelta(days=days_back)

        print(f"🕐 Timestamp bounds (UTC):")
        print(f"   Start: {start_date.isoformat()}")
        print(f"   End:   {end_date.isoformat()}")

        total_measurements = 0

        # Process each municipality
        for idx, sensor_id in enumerate(self.sensor_ids):
            if (idx + 1) % 50 == 0:
                print(f"  📊 Processing municipality {idx + 1}/{len(self.sensor_ids)}...")

            muni = self.municipalities[idx]
            ts_ids = self.timeseries_map[sensor_id]

            # Initialize starting values for this municipality
            # Temperature varies by latitude (north-south gradient in Italy)
            geometry_type = muni['geometry']['type']
            if geometry_type == 'MultiPolygon':
                # Both types will have the first coordinate pair at this depth for their first boundary.
                center_lat = float(muni['geometry']['coordinates'][0][0][0][1])
            elif geometry_type == 'Polygon':
                # Both types will have the first coordinate pair at this depth for their first boundary.
                center_lat = float(muni['geometry']['coordinates'][0][0][1])
            else:
                # Handle other types gracefully
                center_lat = 0.0 # Or use a more appropriate default/error handling

            # Italy ranges from ~36°N (Sicily) to ~47°N (Alps)
            # Temperature baseline: warmer in south, cooler in north
            temp_baseline = 22.0 - (center_lat - 36.0) * 0.8  # ~22°C in Sicily, ~13°C in Alps

            current_temp = random.uniform(temp_baseline - 3.0, temp_baseline + 3.0)
            current_pollution = random.uniform(20.0, 60.0)

            # Generate hourly measurements
            current_date = start_date
            temp_measurements = []
            pollution_measurements = []

            while current_date <= end_date:
                hour = current_date.hour
                day_of_year = current_date.timetuple().tm_yday

                # Temperature: correlated variation with daily and seasonal patterns
                daily_variation = 3.0 * ((hour - 6) / 12.0) if 6 <= hour <= 18 else -2.0
                seasonal_variation = 10.0 * (0.5 - abs((day_of_year - 182) / 365.0))
                random_change = random.uniform(-0.5, 0.5)

                base_temp = temp_baseline + seasonal_variation
                current_temp = current_temp + random_change + 0.05 * (base_temp - current_temp) + daily_variation * 0.1
                current_temp = max(-10.0, min(40.0, current_temp))

                temp_measurements.append((
                    ts_ids['temperature'],
                    current_date,
                    round(current_temp, 2),
                    self.provenance_id
                ))

                # Pollution: correlated variation with traffic patterns
                rush_hour_factor = 1.3 if hour in [7, 8, 17, 18, 19] else 1.0
                winter_factor = 1.4 if day_of_year < 90 or day_of_year > 300 else 1.0

                pollution_change = random.uniform(-5.0, 5.0) * rush_hour_factor * winter_factor
                base_pollution = 40.0
                current_pollution = current_pollution + pollution_change + 0.1 * (base_pollution - current_pollution)
                current_pollution = max(0.0, min(300.0, current_pollution))

                pollution_measurements.append((
                    ts_ids['pollution'],
                    current_date,
                    round(current_pollution, 2),
                    self.provenance_id
                ))

                # Insert in batches
                if len(temp_measurements) >= self.batch_size:
                    self._insert_numeric_batch(temp_measurements)
                    total_measurements += len(temp_measurements)
                    temp_measurements = []

                if len(pollution_measurements) >= self.batch_size:
                    self._insert_numeric_batch(pollution_measurements)
                    total_measurements += len(pollution_measurements)
                    pollution_measurements = []

                # Move to next hour
                current_date += timedelta(hours=6)

            # Insert remaining measurements
            if temp_measurements:
                self._insert_numeric_batch(temp_measurements)
                total_measurements += len(temp_measurements)

            if pollution_measurements:
                self._insert_numeric_batch(pollution_measurements)
                total_measurements += len(pollution_measurements)

        print(f"✅ Created {total_measurements:,} correlated measurements")

    def _insert_numeric_batch(self, measurements: List[Tuple]):
        """Insert a batch of numeric measurements"""
        if not measurements:
            return

        query = f"""
            INSERT INTO {SCHEMA}.measurements_numeric (timeseries_id, timestamp, value, provenance_id)
            VALUES (%s, %s, %s, %s)
            ON CONFLICT DO NOTHING
        """

        try:
            self.cursor.executemany(query, measurements)
            self.conn.commit()
        except Exception as e:
            print(f"  ⚠️  Error inserting numeric batch: {e}")
            self.conn.rollback()

    def print_summary(self):
        """Print summary of populated municipality data"""
        print("\n" + "="*60)
        print("🏛️  MUNICIPALITY DATA POPULATION SUMMARY")
        print("="*60)

        # Count sensors
        self.cursor.execute(f"""
            SELECT COUNT(*) as count FROM {SCHEMA}.sensors
            WHERE name LIKE 'urn:odh:municipality:%'
        """)
        sensor_count = self.cursor.fetchone()['count']
        print(f"{'Municipality Sensors':.<30} {sensor_count:>8,} records")

        # Count timeseries
        self.cursor.execute(f"""
            SELECT COUNT(*) as count FROM {SCHEMA}.timeseries
            WHERE sensor_id IN (
                SELECT id FROM {SCHEMA}.sensors
                WHERE name LIKE 'urn:odh:municipality:%'
            )
        """)
        timeseries_count = self.cursor.fetchone()['count']
        print(f"{'Municipality Timeseries':.<30} {timeseries_count:>8,} records")

        # Count measurements
        self.cursor.execute(f"""
            SELECT COUNT(*) as count FROM {SCHEMA}.measurements_numeric
            WHERE timeseries_id IN (
                SELECT ts.id FROM {SCHEMA}.timeseries ts
                JOIN {SCHEMA}.sensors s ON ts.sensor_id = s.id
                WHERE s.name LIKE 'urn:odh:municipality:%'
            )
        """)
        numeric_count = self.cursor.fetchone()['count']
        print(f"{'Numeric Measurements':.<30} {numeric_count:>8,} records")

        print("="*60)

        # Sample queries
        print("\n🔍 SAMPLE QUERIES TO TRY:")
        print("-" * 60)
        print("# Get all municipality sensors:")
        print("curl 'http://localhost:8080/api/v1/sensors?name_pattern=urn:odh:municipality:%'")
        print("\n# Get sensors by province:")
        print("curl 'http://localhost:8080/api/v1/sensors' | jq '.[] | select(.metadata.province == \"Bolzano/Bozen\")'")
        print("\n# Get temperature measurements:")
        print("curl 'http://localhost:8080/api/v1/measurements/latest?type_names=air_temperature'")
        print("\n# Get pollution data:")
        print("curl 'http://localhost:8080/api/v1/measurements/latest?type_names=pollution_level'")
        print("="*60)


def main():
    parser = argparse.ArgumentParser(description='Populate municipality sensors with correlated measurements')
    parser.add_argument('--clean', action='store_true', help='Clean existing municipality data before populating')
    parser.add_argument('--batch-size', type=int, default=1000, help='Batch size for measurements')
    parser.add_argument('--days', type=int, default=30, help='Days of historical measurements')
    parser.add_argument('--limit', type=int, default=None, help='Limit number of municipalities (for testing)')

    args = parser.parse_args()

    print("🚀 Starting municipality population...")
    print(f"Database: {DB_CONFIG['dbname']}@{DB_CONFIG['host']}")
    print(f"Schema: {SCHEMA}")

    populator = MunicipalityPopulator(batch_size=args.batch_size)

    try:
        populator.connect()
        populator.load_geojson(limit=args.limit)

        if args.clean:
            populator.clean_municipality_data()
        populator.clean_municipality_data()

        # Populate in dependency order
        populator.create_provenance()
        populator.create_or_get_types()
        populator.create_dataset()
        populator.populate_municipality_sensors()
        populator.create_timeseries()
        populator.populate_correlated_measurements(days_back=args.days)

        populator.print_summary()

    except KeyboardInterrupt:
        print("\n⚠️  Population interrupted by user")
    except Exception as e:
        print(f"❌ Error during population: {e}")
        import traceback
        traceback.print_exc()
        raise
    finally:
        populator.disconnect()

    print("\n✅ Municipality population completed!")


if __name__ == '__main__':
    main()
