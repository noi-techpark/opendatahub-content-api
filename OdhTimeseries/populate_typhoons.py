#!/usr/bin/env python3
"""
Typhoon Database Population Script for Timeseries API

This script populates the database with realistic typhoon sensors:
- Sensors represent typhoons without fixed positions
- Creates interconnected timeseries for position, wind, radius, and predictions
- Generates realistic typhoon movement patterns with lifecycle stages

Usage:
    python populate_typhoons.py [--clean] [--days 7] [--count 5]
"""

import os
import sys
import json
import random
import argparse
import math
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

# Typhoon names (Western Pacific naming convention)
TYPHOON_NAMES = [
    "Haikui", "Kirogi", "Yun-yeung", "Koinu", "Bolaven",
    "Sanba", "Jelawat", "Ewiniar", "Maliksi", "Gaemi",
    "Prapiroon", "Maria", "Son-Tinh", "Ampil", "Wukong",
    "Jongdari", "Shanshan", "Yagi", "Leepi", "Bebinca"
]

# Typhoon lifecycle stages
STAGE_FORMING = "forming"
STAGE_INTENSIFYING = "intensifying"
STAGE_MATURE = "mature"
STAGE_WEAKENING = "weakening"
STAGE_DISSIPATING = "dissipating"


class TyphoonPopulator:
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
        self.timeseries_map = {}  # sensor_id -> {timeseries_type: ts_id}
        self.typhoons = []  # List of typhoon configurations

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

    def clean_typhoon_data(self):
        """Clean existing typhoon data"""
        print("🧹 Cleaning existing typhoon data...")

        try:
            # Delete measurements for typhoon sensors
            self.cursor.execute(f"""
                DELETE FROM {SCHEMA}.measurements_numeric
                WHERE timeseries_id IN (
                    SELECT ts.id FROM {SCHEMA}.timeseries ts
                    JOIN {SCHEMA}.sensors s ON ts.sensor_id = s.id
                    WHERE s.name LIKE 'urn:odh:typhoon:%'
                )
            """)
            print(f"  🗑️  Cleaned typhoon numeric measurements")

            self.cursor.execute(f"""
                DELETE FROM {SCHEMA}.measurements_geoposition
                WHERE timeseries_id IN (
                    SELECT ts.id FROM {SCHEMA}.timeseries ts
                    JOIN {SCHEMA}.sensors s ON ts.sensor_id = s.id
                    WHERE s.name LIKE 'urn:odh:typhoon:%'
                )
            """)
            print(f"  🗑️  Cleaned typhoon geoposition measurements")

            self.cursor.execute(f"""
                DELETE FROM {SCHEMA}.timeseries
                WHERE sensor_id IN (
                    SELECT id FROM {SCHEMA}.sensors
                    WHERE name LIKE 'urn:odh:typhoon:%'
                )
            """)
            print(f"  🗑️  Cleaned typhoon timeseries")

            self.cursor.execute(f"""
                DELETE FROM {SCHEMA}.sensors
                WHERE name LIKE 'urn:odh:typhoon:%'
            """)
            print(f"  🗑️  Cleaned typhoon sensors")

            # Clean from content database
            self.content_cursor.execute("""
                DELETE FROM public.sensors
                WHERE id LIKE 'urn:odh:typhoon:%'
            """)
            print(f"  🗑️  Cleaned typhoon sensors from content DB")

            self.conn.commit()
            self.content_conn.commit()
            print("✅ Typhoon data cleaned successfully")

        except Exception as e:
            print(f"⚠️  Warning during cleanup: {e}")
            self.conn.rollback()
            self.content_conn.rollback()

    def create_provenance(self):
        """Create or get provenance record for typhoons"""
        print("📋 Creating provenance record...")

        provenance_uuid = str(uuid.uuid4())
        lineage = "typhoon_tracking"
        collector = "TyphoonTracker"
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
        """Create or get measurement types for typhoons"""
        print("📊 Creating/retrieving measurement types...")

        types_to_create = [
            ("typhoon_position", "Typhoon Current Position", "", "geoposition"),
            ("typhoon_radius", "Typhoon Radius", "km", "numeric"),
            ("typhoon_wind_speed", "Typhoon Wind Speed", "km/h", "numeric"),
            ("typhoon_prediction_30min", "Predicted Position (+30 min)", "", "geoposition"),
            ("typhoon_prediction_60min", "Predicted Position (+60 min)", "", "geoposition"),
            ("typhoon_prediction_120min", "Predicted Position (+120 min)", "", "geoposition")
        ]

        for name, description, unit, data_type in types_to_create:
            metadata = {
                "category": "weather",
                "precision": 2 if data_type == "numeric" else 6,
                "sampling_rate": "10min"
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
        """Create dataset for typhoon tracking"""
        print("📦 Creating typhoon dataset...")

        dataset_name = "typhoon_tracking"
        dataset_description = "Real-time typhoon tracking with position, wind speed, radius, and predictions"

        self.cursor.execute(f"""
            INSERT INTO {SCHEMA}.datasets (name, description)
            VALUES (%s, %s)
            ON CONFLICT (name) DO UPDATE SET
                description = EXCLUDED.description
            RETURNING id
        """, (dataset_name, dataset_description))

        self.dataset_id = self.cursor.fetchone()['id']

        # Associate types with dataset
        for type_name in self.type_ids.keys():
            self.cursor.execute(f"""
                INSERT INTO {SCHEMA}.dataset_types (dataset_id, type_id, is_required)
                VALUES (%s, %s, %s)
                ON CONFLICT (dataset_id, type_id) DO NOTHING
            """, (self.dataset_id, self.type_ids[type_name], True))

        self.conn.commit()
        print(f"✅ Created dataset (id: {self.dataset_id})")

    def generate_typhoon_configs(self, count: int):
        """Generate configurations for realistic typhoons"""
        print(f"🌀 Generating {count} typhoon configurations...")

        for i in range(count):
            name = TYPHOON_NAMES[i % len(TYPHOON_NAMES)]
            year = 2025
            typhoon_id = f"{year}{(i+1):02d}"

            # Random starting position in Western Pacific
            # Typhoons typically form between 5°N-20°N and 120°E-180°E
            start_lat = random.uniform(8.0, 18.0)
            start_lon = random.uniform(125.0, 160.0)

            # Movement direction (typically northwest)
            # Angle in degrees (0 = north, 90 = east)
            base_direction = random.uniform(280, 350)  # Northwest to north

            # Movement speed (km/h)
            base_speed = random.uniform(15, 35)

            # Lifecycle duration (hours)
            total_duration = random.randint(120, 240)  # 5-10 days

            self.typhoons.append({
                'name': name,
                'id': typhoon_id,
                'start_lat': start_lat,
                'start_lon': start_lon,
                'base_direction': base_direction,
                'base_speed': base_speed,
                'total_duration': total_duration
            })

        print(f"✅ Generated {len(self.typhoons)} typhoon configurations")

    def populate_typhoon_sensors(self):
        """Populate sensors for typhoons"""
        print(f"🌀 Creating {len(self.typhoons)} typhoon sensors...")

        for typhoon in self.typhoons:
            sensor_urn = f"urn:odh:typhoon:{typhoon['id']}"

            # Create metadata (no fixed position)
            metadata = {
                "typhoon_name": typhoon['name'],
                "typhoon_id": typhoon['id'],
                "type": "typhoon",
                "basin": "Western Pacific",
                "year": 2025,
                "status": "active"
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
            self._insert_typhoon_to_content_db(sensor_urn, typhoon, metadata)

        self.conn.commit()
        print(f"✅ Created {len(self.sensor_ids)} typhoon sensors")

    def _insert_typhoon_to_content_db(self, sensor_urn: str, typhoon: dict, metadata: dict):
        """Insert typhoon sensor into content database"""
        try:
            sensor_data = {
                "Id": sensor_urn,
                "Active": True,
                "SmgActive": True,
                "_Meta": {
                    "Type": "sensor",
                    "LastUpdate": datetime.now(timezone.utc).isoformat(),
                    "Source": "typhoon_populate_script"
                },
                "LicenseInfo": {
                    "Author": "Open Data Hub",
                    "License": "CC0",
                    "ClosedData": False
                },
                "Source": "typhoon_populate_script",
                "FirstImport": datetime.now(timezone.utc).isoformat(),
                "LastChange": datetime.now(timezone.utc).isoformat(),
                "SensorType": "TYPHOON",
                "SensorName": f"Typhoon {typhoon['name']} ({typhoon['id']})",
                "Detail": {
                    "en": {
                        "Title": f"Typhoon {typhoon['name']}",
                        "Header": f"Tropical Cyclone {typhoon['id']}",
                        "BaseText": f"Real-time tracking of Typhoon {typhoon['name']} in the Western Pacific",
                        "AdditionalText": "Includes current position, wind speed, radius, and predicted trajectory"
                    }
                },
                "HasLanguage": ["en"],
                "SmgTags": ["typhoon", "weather", "tracking", typhoon['name'].lower()],
                "PublishedOn": ["odh"],
                "AdditionalProperties": {
                    "category": "weather",
                    "tracking_type": "real_time",
                    "basin": "Western Pacific"
                }
            }

            self.content_cursor.execute("""
                INSERT INTO public.sensors (id, data)
                VALUES (%s, %s)
                ON CONFLICT (id) DO UPDATE SET data = EXCLUDED.data
            """, (sensor_urn, json.dumps(sensor_data)))

            self.content_conn.commit()
        except Exception as e:
            print(f"⚠️  Warning: Could not insert typhoon {sensor_urn} to content database: {e}")
            self.content_conn.rollback()

    def create_timeseries(self):
        """Create timeseries for all typhoon measurement types"""
        print(f"📈 Creating timeseries for {len(self.sensor_ids)} typhoons...")

        for sensor_id in self.sensor_ids:
            self.timeseries_map[sensor_id] = {}

            for type_name in self.type_ids.keys():
                self.cursor.execute(f"""
                    INSERT INTO {SCHEMA}.timeseries (sensor_id, type_id)
                    VALUES (%s, %s) RETURNING id
                """, (sensor_id, self.type_ids[type_name]))

                ts_id = self.cursor.fetchone()['id']
                self.timeseries_map[sensor_id][type_name] = ts_id

        self.conn.commit()
        print(f"✅ Created {len(self.sensor_ids) * len(self.type_ids)} timeseries")

    def populate_typhoon_measurements(self, days_back: int = 7):
        """
        Populate measurements with realistic typhoon movement and predictions
        """
        print(f"📏 Creating typhoon measurements for last {days_back} days...")

        end_date = datetime.now(timezone.utc)
        start_date = end_date - timedelta(days=days_back)

        print(f"🕐 Timestamp bounds (UTC):")
        print(f"   Start: {start_date.isoformat()}")
        print(f"   End:   {end_date.isoformat()}")

        total_measurements = 0

        # Process each typhoon
        for idx, sensor_id in enumerate(self.sensor_ids):
            typhoon = self.typhoons[idx]
            print(f"  🌀 Processing Typhoon {typhoon['name']}...")

            ts_ids = self.timeseries_map[sensor_id]

            # Generate measurements every 10 minutes
            current_date = start_date
            measurements_position = []
            measurements_radius = []
            measurements_wind = []
            measurements_pred_30 = []
            measurements_pred_60 = []
            measurements_pred_120 = []

            # Typhoon state
            current_lat = typhoon['start_lat']
            current_lon = typhoon['start_lon']
            current_direction = typhoon['base_direction']
            current_speed = typhoon['base_speed']

            # Lifecycle tracking
            duration_so_far = 0
            total_duration = typhoon['total_duration']

            while current_date <= end_date and duration_so_far < total_duration:
                # Determine lifecycle stage
                progress = duration_so_far / total_duration

                if progress < 0.15:
                    stage = STAGE_FORMING
                elif progress < 0.35:
                    stage = STAGE_INTENSIFYING
                elif progress < 0.65:
                    stage = STAGE_MATURE
                elif progress < 0.85:
                    stage = STAGE_WEAKENING
                else:
                    stage = STAGE_DISSIPATING

                # Update typhoon parameters based on stage
                if stage == STAGE_FORMING:
                    wind_speed = 80 + progress * 50  # 80-130 km/h
                    radius = 100 + progress * 100  # 100-200 km
                elif stage == STAGE_INTENSIFYING:
                    wind_speed = 130 + (progress - 0.15) * 200  # 130-170 km/h
                    radius = 200 + (progress - 0.15) * 150  # 200-230 km
                elif stage == STAGE_MATURE:
                    wind_speed = 170 + random.uniform(-10, 10)  # ~170 km/h
                    radius = 230 + random.uniform(-20, 20)  # ~230 km
                elif stage == STAGE_WEAKENING:
                    wind_speed = 170 - (progress - 0.65) * 200  # 170-130 km/h
                    radius = 230 - (progress - 0.65) * 100  # 230-210 km
                else:  # DISSIPATING
                    wind_speed = 130 - (progress - 0.85) * 300  # 130-85 km/h
                    radius = 210 - (progress - 0.85) * 150  # 210-185 km

                # Add some randomness
                wind_speed += random.uniform(-5, 5)
                radius += random.uniform(-10, 10)

                # Movement (every 10 minutes = 1/6 hour)
                time_delta_hours = 1/6

                # Direction changes slightly (Coriolis effect and steering currents)
                direction_change = random.uniform(-3, 3)
                current_direction += direction_change

                # Speed varies slightly
                speed_change = random.uniform(-2, 2)
                current_speed += speed_change
                current_speed = max(10, min(50, current_speed))

                # Calculate new position
                distance_km = current_speed * time_delta_hours
                current_lat, current_lon = self._move_position(
                    current_lat, current_lon, current_direction, distance_km
                )

                # Current position
                position_wkt = f"POINT({current_lon} {current_lat})"
                measurements_position.append((
                    ts_ids['typhoon_position'],
                    current_date,
                    position_wkt,
                    self.provenance_id
                ))

                # Current radius
                measurements_radius.append((
                    ts_ids['typhoon_radius'],
                    current_date,
                    round(radius, 2),
                    self.provenance_id
                ))

                # Current wind speed
                measurements_wind.append((
                    ts_ids['typhoon_wind_speed'],
                    current_date,
                    round(wind_speed, 2),
                    self.provenance_id
                ))

                # Predictions (assume continued movement in current direction)
                pred_30_lat, pred_30_lon = self._move_position(
                    current_lat, current_lon, current_direction, current_speed * 0.5
                )
                measurements_pred_30.append((
                    ts_ids['typhoon_prediction_30min'],
                    current_date,
                    f"POINT({pred_30_lon} {pred_30_lat})",
                    self.provenance_id
                ))

                pred_60_lat, pred_60_lon = self._move_position(
                    current_lat, current_lon, current_direction, current_speed * 1.0
                )
                measurements_pred_60.append((
                    ts_ids['typhoon_prediction_60min'],
                    current_date,
                    f"POINT({pred_60_lon} {pred_60_lat})",
                    self.provenance_id
                ))

                pred_120_lat, pred_120_lon = self._move_position(
                    current_lat, current_lon, current_direction, current_speed * 2.0
                )
                measurements_pred_120.append((
                    ts_ids['typhoon_prediction_120min'],
                    current_date,
                    f"POINT({pred_120_lon} {pred_120_lat})",
                    self.provenance_id
                ))

                # Insert in batches
                if len(measurements_position) >= self.batch_size:
                    self._insert_geoposition_batch(measurements_position)
                    self._insert_numeric_batch(measurements_radius)
                    self._insert_numeric_batch(measurements_wind)
                    self._insert_geoposition_batch(measurements_pred_30)
                    self._insert_geoposition_batch(measurements_pred_60)
                    self._insert_geoposition_batch(measurements_pred_120)

                    total_measurements += len(measurements_position) * 6
                    measurements_position = []
                    measurements_radius = []
                    measurements_wind = []
                    measurements_pred_30 = []
                    measurements_pred_60 = []
                    measurements_pred_120 = []

                # Move to next timestamp (10 minutes)
                current_date += timedelta(minutes=10)
                duration_so_far += 1/6  # hours

            # Insert remaining measurements
            if measurements_position:
                self._insert_geoposition_batch(measurements_position)
                self._insert_numeric_batch(measurements_radius)
                self._insert_numeric_batch(measurements_wind)
                self._insert_geoposition_batch(measurements_pred_30)
                self._insert_geoposition_batch(measurements_pred_60)
                self._insert_geoposition_batch(measurements_pred_120)
                total_measurements += len(measurements_position) * 6

        print(f"✅ Created {total_measurements:,} measurements")

    def _move_position(self, lat: float, lon: float, direction_deg: float, distance_km: float) -> Tuple[float, float]:
        """
        Calculate new position given starting point, direction, and distance
        Uses simple spherical geometry approximation
        """
        # Earth radius in km
        R = 6371.0

        # Convert to radians
        lat_rad = math.radians(lat)
        lon_rad = math.radians(lon)
        direction_rad = math.radians(direction_deg)

        # Angular distance
        angular_distance = distance_km / R

        # Calculate new latitude
        new_lat_rad = math.asin(
            math.sin(lat_rad) * math.cos(angular_distance) +
            math.cos(lat_rad) * math.sin(angular_distance) * math.cos(direction_rad)
        )

        # Calculate new longitude
        new_lon_rad = lon_rad + math.atan2(
            math.sin(direction_rad) * math.sin(angular_distance) * math.cos(lat_rad),
            math.cos(angular_distance) - math.sin(lat_rad) * math.sin(new_lat_rad)
        )

        # Convert back to degrees
        new_lat = math.degrees(new_lat_rad)
        new_lon = math.degrees(new_lon_rad)

        return new_lat, new_lon

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

    def _insert_geoposition_batch(self, measurements: List[Tuple]):
        """Insert a batch of geoposition measurements"""
        if not measurements:
            return

        query = f"""
            INSERT INTO {SCHEMA}.measurements_geoposition (timeseries_id, timestamp, value, provenance_id)
            VALUES (%s, %s, ST_GeomFromText(%s, 4326), %s)
            ON CONFLICT DO NOTHING
        """

        try:
            self.cursor.executemany(query, measurements)
            self.conn.commit()
        except Exception as e:
            print(f"  ⚠️  Error inserting geoposition batch: {e}")
            self.conn.rollback()

    def print_summary(self):
        """Print summary of populated typhoon data"""
        print("\n" + "="*60)
        print("🌀 TYPHOON DATA POPULATION SUMMARY")
        print("="*60)

        # Count sensors
        self.cursor.execute(f"""
            SELECT COUNT(*) as count FROM {SCHEMA}.sensors
            WHERE name LIKE 'urn:odh:typhoon:%'
        """)
        sensor_count = self.cursor.fetchone()['count']
        print(f"{'Typhoon Sensors':.<30} {sensor_count:>8,} records")

        # Count timeseries
        self.cursor.execute(f"""
            SELECT COUNT(*) as count FROM {SCHEMA}.timeseries
            WHERE sensor_id IN (
                SELECT id FROM {SCHEMA}.sensors
                WHERE name LIKE 'urn:odh:typhoon:%'
            )
        """)
        timeseries_count = self.cursor.fetchone()['count']
        print(f"{'Typhoon Timeseries':.<30} {timeseries_count:>8,} records")

        # Count measurements
        self.cursor.execute(f"""
            SELECT COUNT(*) as count FROM {SCHEMA}.measurements_numeric
            WHERE timeseries_id IN (
                SELECT ts.id FROM {SCHEMA}.timeseries ts
                JOIN {SCHEMA}.sensors s ON ts.sensor_id = s.id
                WHERE s.name LIKE 'urn:odh:typhoon:%'
            )
        """)
        numeric_count = self.cursor.fetchone()['count']
        print(f"{'Numeric Measurements':.<30} {numeric_count:>8,} records")

        self.cursor.execute(f"""
            SELECT COUNT(*) as count FROM {SCHEMA}.measurements_geoposition
            WHERE timeseries_id IN (
                SELECT ts.id FROM {SCHEMA}.timeseries ts
                JOIN {SCHEMA}.sensors s ON ts.sensor_id = s.id
                WHERE s.name LIKE 'urn:odh:typhoon:%'
            )
        """)
        geoposition_count = self.cursor.fetchone()['count']
        print(f"{'Geoposition Measurements':.<30} {geoposition_count:>8,} records")

        print("="*60)

        # Sample queries
        print("\n🔍 SAMPLE QUERIES TO TRY:")
        print("-" * 60)
        print("# Get all typhoon sensors:")
        print("curl 'http://localhost:8080/api/v1/sensors?name_pattern=urn:odh:typhoon:%'")
        print("\n# Get latest typhoon positions:")
        print("curl 'http://localhost:8080/api/v1/measurements/latest?type_names=typhoon_position'")
        print("="*60)


def main():
    parser = argparse.ArgumentParser(description='Populate typhoon sensors with realistic tracking data')
    parser.add_argument('--clean', action='store_true', help='Clean existing typhoon data before populating')
    parser.add_argument('--batch-size', type=int, default=1000, help='Batch size for measurements')
    parser.add_argument('--days', type=int, default=7, help='Days of historical measurements')
    parser.add_argument('--count', type=int, default=5, help='Number of typhoons to create')

    args = parser.parse_args()

    print("🚀 Starting typhoon population...")
    print(f"Database: {DB_CONFIG['dbname']}@{DB_CONFIG['host']}")
    print(f"Schema: {SCHEMA}")
    print(f"Typhoons: {args.count}")

    populator = TyphoonPopulator(batch_size=args.batch_size)

    try:
        populator.connect()

        if args.clean:
            populator.clean_typhoon_data()

        # Populate in dependency order
        populator.create_provenance()
        populator.create_or_get_types()
        populator.create_dataset()
        populator.generate_typhoon_configs(count=args.count)
        populator.populate_typhoon_sensors()
        populator.create_timeseries()
        populator.populate_typhoon_measurements(days_back=args.days)

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

    print("\n✅ Typhoon population completed!")


if __name__ == '__main__':
    main()
