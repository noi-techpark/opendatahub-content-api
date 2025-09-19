#!/usr/bin/env python3
"""
Database Population Script for Timeseries API

This script populates the database with realistic test data including:
- Provenance records
- Sensor definitions
- Measurement types
- Datasets with type associations
- Timeseries
- Sample measurements across all data types

Usage:
    python populate_db.py [--clean] [--batch-size 1000]
"""

import os
import sys
import json
import random
import argparse
from datetime import datetime, timedelta
from typing import List, Dict, Any, Optional
import uuid

import psycopg
from faker import Faker
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

SCHEMA = os.getenv('DB_SCHEMA', 'intimev3')

fake = Faker()

class DatabasePopulator:
    def __init__(self, batch_size: int = 1000):
        self.batch_size = batch_size
        self.conn = None
        self.cursor = None

        # Data containers
        self.provenance_ids = []
        self.sensor_ids = []
        self.type_ids = []
        self.dataset_ids = []
        self.timeseries_ids = []

    def create_partitions(self):
        """Create partition tables for measurements if they don't exist"""
        print("🔧 Creating measurement partitions for 2025...")

        partition_configs = [
            ('numeric', 'FLOAT8'),
            ('string', 'VARCHAR(255)'),
            ('json', 'JSONB'),
            ('geoposition', 'public.geometry(Point, 4326)'),
            ('geoshape', 'public.geometry(Polygon, 4326)'),
            ('boolean', 'BOOLEAN')
        ]

        for data_type, value_type in partition_configs:
            partition_name = f"{SCHEMA}.measurements_{data_type}_2025"

            # Check if partition already exists
            self.cursor.execute(f"""
                SELECT EXISTS (
                    SELECT 1 FROM information_schema.tables
                    WHERE table_schema = %s AND table_name = %s
                )
            """, (SCHEMA, f"measurements_{data_type}_2025"))

            exists = self.cursor.fetchone()['exists']

            if not exists:
                # Create partition
                self.cursor.execute(f"""
                    CREATE TABLE {partition_name} PARTITION OF {SCHEMA}.measurements_{data_type}
                        FOR VALUES WITH (modulus 4, remainder 0)
                """)

                # Add primary key
                self.cursor.execute(f"""
                    ALTER TABLE {partition_name} ADD PRIMARY KEY (timeseries_id, timestamp)
                """)

                # Create indexes
                self.cursor.execute(f"""
                    CREATE INDEX idx_{data_type}_timestamp_2025 ON {partition_name} (timestamp DESC)
                """)
                self.cursor.execute(f"""
                    CREATE INDEX idx_{data_type}_timeseries_id_2025 ON {partition_name} (timeseries_id)
                """)

                if data_type in ['geoposition', 'geoshape']:
                    self.cursor.execute(f"""
                        CREATE INDEX idx_{data_type}_value_2025 ON {partition_name} USING GIST (value)
                    """)

                print(f"  ✅ Created partition: measurements_{data_type}_2025")
            else:
                print(f"  ⚪ Partition already exists: measurements_{data_type}_2025")

        # Create additional partitions for better distribution
        for remainder in [1, 2, 3]:
            for data_type, value_type in partition_configs:
                partition_name = f"{SCHEMA}.measurements_{data_type}_2025_p{remainder}"

                self.cursor.execute(f"""
                    SELECT EXISTS (
                        SELECT 1 FROM information_schema.tables
                        WHERE table_schema = %s AND table_name = %s
                    )
                """, (SCHEMA, f"measurements_{data_type}_2025_p{remainder}"))

                exists = self.cursor.fetchone()['exists']

                if not exists:
                    self.cursor.execute(f"""
                        CREATE TABLE {partition_name} PARTITION OF {SCHEMA}.measurements_{data_type}
                            FOR VALUES WITH (modulus 4, remainder {remainder})
                    """)

                    self.cursor.execute(f"""
                        ALTER TABLE {partition_name} ADD PRIMARY KEY (timeseries_id, timestamp)
                    """)

        self.conn.commit()
        print("✅ Partition creation completed")

    def connect(self):
        """Connect to PostgreSQL database"""
        try:
            self.conn = psycopg.connect(**DB_CONFIG)
            self.cursor = self.conn.cursor(row_factory=psycopg.rows.dict_row)
            print(f"✅ Connected to database: {DB_CONFIG['dbname']}")
        except Exception as e:
            print(f"❌ Database connection failed: {e}")
            sys.exit(1)

    def disconnect(self):
        """Close database connection"""
        if self.cursor:
            self.cursor.close()
        if self.conn:
            self.conn.close()
        print("✅ Database connection closed")

    def clean_database(self):
        """Clean all data from tables (in correct order)"""
        print("🧹 Cleaning existing data...")

        tables = [
            f'{SCHEMA}.measurements_numeric',
            f'{SCHEMA}.measurements_string',
            f'{SCHEMA}.measurements_json',
            f'{SCHEMA}.measurements_geoposition',
            f'{SCHEMA}.measurements_geoshape',
            f'{SCHEMA}.measurements_boolean',
            f'{SCHEMA}.timeseries',
            f'{SCHEMA}.dataset_types',
            f'{SCHEMA}.datasets',
            f'{SCHEMA}.types',
            f'{SCHEMA}.sensors',
            f'{SCHEMA}.provenance'
        ]

        for table in tables:
            self.cursor.execute(f"DELETE FROM {table}")
            print(f"  🗑️  Cleaned {table}")

        self.conn.commit()
        print("✅ Database cleaned successfully")

    def populate_provenance(self, count: int = 10):
        """Populate provenance table"""
        print(f"📋 Creating {count} provenance records...")

        data_sources = [
            ("weather_stations", "WeatherCollector", "v2.1.0"),
            ("air_quality", "AQMonitor", "v1.5.2"),
            ("traffic_sensors", "TrafficAPI", "v3.0.1"),
            ("iot_devices", "IoTHub", "v1.2.3"),
            ("environmental", "EnvSensor", "v2.0.0"),
            ("energy_meters", "EnergyCollector", "v1.8.1"),
            ("parking_sensors", "ParkingAPI", "v2.2.0"),
            ("noise_monitors", "SoundCollector", "v1.3.4"),
            ("water_quality", "H2OMonitor", "v1.7.2"),
            ("solar_panels", "SolarTracker", "v2.5.1")
        ]

        for i, (lineage, collector, version) in enumerate(data_sources[:count]):
            provenance_uuid = str(uuid.uuid4())

            self.cursor.execute(f"""
                INSERT INTO {SCHEMA}.provenance (uuid, lineage, data_collector, data_collector_version)
                VALUES (%s, %s, %s, %s)
                ON CONFLICT (lineage, data_collector, data_collector_version) DO UPDATE SET
                    uuid = EXCLUDED.uuid
                RETURNING id
            """, (provenance_uuid, lineage, collector, version))

            self.provenance_ids.append(self.cursor.fetchone()['id'])

        self.conn.commit()
        print(f"✅ Created {len(self.provenance_ids)} provenance records")

    def populate_sensors(self, count: int = 100):
        """Populate sensors table"""
        print(f"🌡️  Creating {count} sensors...")

        sensor_prefixes = [
            "TEMP", "HUM", "PRESS", "PM25", "PM10", "NO2", "CO2",
            "TRAFFIC", "PARK", "NOISE", "WATER", "SOLAR", "WIND", "RAIN"
        ]

        locations = [
            "Downtown", "Airport", "Industrial", "Residential", "Park",
            "Highway", "Shopping", "University", "Hospital", "Station"
        ]

        for i in range(count):
            prefix = random.choice(sensor_prefixes)
            location = random.choice(locations)
            sensor_name = f"{prefix}_{location}_{i+1:03d}"

            # Create realistic metadata
            metadata = {
                "location": location,
                "installation_date": fake.date_between(start_date='-2y', end_date='today').isoformat(),
                "manufacturer": random.choice(["SensorTech", "EnviroSys", "DataPro", "IoTSolutions"]),
                "model": f"Model-{random.randint(100, 999)}",
                "firmware_version": f"{random.randint(1,3)}.{random.randint(0,9)}.{random.randint(0,9)}",
                "calibration_date": fake.date_between(start_date='-6m', end_date='today').isoformat(),
                "coordinates": {
                    "lat": round(random.uniform(46.0, 47.0), 6),  # South Tyrol-ish coordinates
                    "lon": round(random.uniform(10.5, 12.5), 6)
                }
            }

            parent_id = random.choice(self.sensor_ids) if self.sensor_ids and random.random() < 0.3 else None

            self.cursor.execute(f"""
                INSERT INTO {SCHEMA}.sensors (name, parent_id, metadata)
                VALUES (%s, %s, %s) RETURNING id
            """, (sensor_name, parent_id, json.dumps(metadata)))

            self.sensor_ids.append(self.cursor.fetchone()['id'])

        self.conn.commit()
        print(f"✅ Created {len(self.sensor_ids)} sensors")

    def populate_types(self):
        """Populate types table with realistic measurement types"""
        print("📊 Creating measurement types...")

        measurement_types = [
            # Environmental
            ("air_temperature", "Air Temperature", "°C", "numeric"),
            ("relative_humidity", "Relative Humidity", "%", "numeric"),
            ("atmospheric_pressure", "Atmospheric Pressure", "hPa", "numeric"),
            ("wind_speed", "Wind Speed", "m/s", "numeric"),
            ("wind_direction", "Wind Direction", "degrees", "numeric"),
            ("precipitation", "Precipitation", "mm", "numeric"),
            ("solar_radiation", "Solar Radiation", "W/m²", "numeric"),
            ("uv_index", "UV Index", "", "numeric"),

            # Air Quality
            ("pm25", "PM2.5 Concentration", "μg/m³", "numeric"),
            ("pm10", "PM10 Concentration", "μg/m³", "numeric"),
            ("no2", "Nitrogen Dioxide", "μg/m³", "numeric"),
            ("co2", "Carbon Dioxide", "ppm", "numeric"),
            ("o3", "Ozone", "μg/m³", "numeric"),
            ("co", "Carbon Monoxide", "mg/m³", "numeric"),
            ("so2", "Sulfur Dioxide", "μg/m³", "numeric"),

            # Traffic & Mobility
            ("vehicle_count", "Vehicle Count", "vehicles/hour", "numeric"),
            ("average_speed", "Average Speed", "km/h", "numeric"),
            ("traffic_density", "Traffic Density", "vehicles/km", "numeric"),
            ("parking_occupancy", "Parking Occupancy", "%", "numeric"),
            ("parking_availability", "Parking Availability", "spaces", "numeric"),

            # Energy
            ("power_consumption", "Power Consumption", "kWh", "numeric"),
            ("power_generation", "Power Generation", "kW", "numeric"),
            ("voltage", "Voltage", "V", "numeric"),
            ("current", "Current", "A", "numeric"),
            ("power_factor", "Power Factor", "", "numeric"),

            # Noise & Vibration
            ("noise_level", "Noise Level", "dB(A)", "numeric"),
            ("vibration", "Vibration Level", "m/s²", "numeric"),

            # Water Quality
            ("water_temperature", "Water Temperature", "°C", "numeric"),
            ("ph_level", "pH Level", "", "numeric"),
            ("dissolved_oxygen", "Dissolved Oxygen", "mg/L", "numeric"),
            ("turbidity", "Turbidity", "NTU", "numeric"),
            ("conductivity", "Electrical Conductivity", "μS/cm", "numeric"),

            # Status & Metadata
            ("device_status", "Device Status", "", "string"),
            ("error_code", "Error Code", "", "string"),
            ("maintenance_status", "Maintenance Status", "", "string"),
            ("battery_level", "Battery Level", "%", "numeric"),
            ("signal_strength", "Signal Strength", "dBm", "numeric"),

            # Boolean indicators
            ("is_online", "Device Online", "", "boolean"),
            ("alert_active", "Alert Active", "", "boolean"),
            ("maintenance_required", "Maintenance Required", "", "boolean"),

            # JSON data
            ("sensor_config", "Sensor Configuration", "", "json"),
            ("diagnostic_data", "Diagnostic Data", "", "json"),
            ("metadata", "Additional Metadata", "", "json"),

            # Geospatial
            ("location", "GPS Location", "", "geoposition"),
            ("coverage_area", "Coverage Area", "", "geoshape")
        ]

        for name, description, unit, data_type in measurement_types:
            metadata = {
                "category": self._get_category(name),
                "precision": random.randint(1, 4),
                "sampling_rate": random.choice(["1min", "5min", "15min", "1hour"]),
                "quality_flags": ["valid", "estimated", "missing", "error"]
            }

            self.cursor.execute(f"""
                INSERT INTO {SCHEMA}.types (name, description, unit, data_type, metadata)
                VALUES (%s, %s, %s, %s, %s) RETURNING id
            """, (name, description, unit, data_type, json.dumps(metadata)))

            self.type_ids.append(self.cursor.fetchone()['id'])

        self.conn.commit()
        print(f"✅ Created {len(self.type_ids)} measurement types")

    def _get_category(self, type_name: str) -> str:
        """Categorize measurement types"""
        if any(x in type_name for x in ["temperature", "humidity", "pressure", "wind", "rain", "solar", "uv"]):
            return "environmental"
        elif any(x in type_name for x in ["pm25", "pm10", "no2", "co2", "o3", "co", "so2"]):
            return "air_quality"
        elif any(x in type_name for x in ["traffic", "vehicle", "speed", "parking"]):
            return "mobility"
        elif any(x in type_name for x in ["power", "voltage", "current", "energy"]):
            return "energy"
        elif any(x in type_name for x in ["noise", "vibration"]):
            return "acoustic"
        elif any(x in type_name for x in ["water", "ph", "oxygen", "turbidity", "conductivity"]):
            return "water_quality"
        elif any(x in type_name for x in ["status", "error", "battery", "signal", "online", "alert", "maintenance"]):
            return "system"
        else:
            return "other"

    def populate_datasets(self, count: int = 15):
        """Populate datasets with realistic groupings"""
        print(f"📦 Creating {count} datasets...")

        dataset_definitions = [
            ("weather_monitoring", "Complete weather monitoring station data", ["air_temperature", "relative_humidity", "atmospheric_pressure", "wind_speed", "wind_direction", "precipitation", "coverage_area"]),
            ("air_quality_standard", "Standard air quality measurements", ["pm25", "pm10", "no2", "co2", "o3", "coverage_area"]),
            ("traffic_analysis", "Traffic monitoring and analysis", ["vehicle_count", "average_speed", "traffic_density", "coverage_area"]),
            ("parking_management", "Smart parking system data", ["parking_occupancy", "parking_availability", "coverage_area"]),
            ("energy_monitoring", "Energy consumption and generation", ["power_consumption", "power_generation", "voltage", "current"]),
            ("environmental_complete", "Complete environmental monitoring", ["air_temperature", "relative_humidity", "pm25", "pm10", "noise_level", "coverage_area"]),
            ("water_quality_basic", "Basic water quality parameters", ["water_temperature", "ph_level", "dissolved_oxygen", "turbidity"]),
            ("iot_device_health", "IoT device health monitoring", ["battery_level", "signal_strength", "is_online", "maintenance_required"]),
            ("urban_sensing", "Urban environment sensing", ["air_temperature", "noise_level", "pm25", "traffic_density", "coverage_area"]),
            ("renewable_energy", "Renewable energy monitoring", ["solar_radiation", "wind_speed", "power_generation"]),
            ("smart_city_basic", "Basic smart city indicators", ["air_temperature", "pm25", "vehicle_count", "noise_level", "coverage_area"]),
            ("industrial_monitoring", "Industrial area monitoring", ["air_temperature", "pm25", "pm10", "no2", "noise_level", "vibration", "coverage_area"]),
            ("public_health", "Public health relevant measurements", ["pm25", "pm10", "no2", "o3", "noise_level", "uv_index"]),
            ("climate_research", "Climate research dataset", ["air_temperature", "relative_humidity", "atmospheric_pressure", "precipitation", "wind_speed", "solar_radiation"]),
            ("mobility_analytics", "Transportation analytics", ["vehicle_count", "average_speed", "parking_occupancy", "traffic_density", "coverage_area"])
        ]

        type_name_to_id = {}
        self.cursor.execute(f"SELECT id, name FROM {SCHEMA}.types")
        for row in self.cursor.fetchall():
            type_name_to_id[row['name']] = row['id']

        for name, description, type_names in dataset_definitions[:count]:
            # Create dataset
            self.cursor.execute(f"""
                INSERT INTO {SCHEMA}.datasets (name, description)
                VALUES (%s, %s) RETURNING id
            """, (name, description))

            dataset_id = self.cursor.fetchone()['id']
            self.dataset_ids.append(dataset_id)

            # Associate types with dataset
            for type_name in type_names:
                if type_name in type_name_to_id:
                    is_required = random.choice([True, True, True, False])  # 75% required
                    self.cursor.execute(f"""
                        INSERT INTO {SCHEMA}.dataset_types (dataset_id, type_id, is_required)
                        VALUES (%s, %s, %s)
                    """, (dataset_id, type_name_to_id[type_name], is_required))

        self.conn.commit()
        print(f"✅ Created {len(self.dataset_ids)} datasets with type associations")

    def populate_timeseries(self, count: int = 500):
        """Populate timeseries table with better dataset associations"""
        print(f"📈 Creating {count} timeseries...")

        # Get type info for better pairing with sensors
        self.cursor.execute(f"SELECT id, name, data_type FROM {SCHEMA}.types")
        types_info = {row['id']: {'name': row['name'], 'data_type': row['data_type']} for row in self.cursor.fetchall()}

        # Get dataset-type associations for smarter assignment
        self.cursor.execute(f"""
            SELECT dt.dataset_id, dt.type_id, d.name as dataset_name
            FROM {SCHEMA}.dataset_types dt
            JOIN {SCHEMA}.datasets d ON dt.dataset_id = d.id
        """)
        dataset_type_associations = {}
        for row in self.cursor.fetchall():
            dataset_id = row['dataset_id']
            if dataset_id not in dataset_type_associations:
                dataset_type_associations[dataset_id] = {
                    'name': row['dataset_name'],
                    'type_ids': []
                }
            dataset_type_associations[dataset_id]['type_ids'].append(row['type_id'])

        created_count = 0
        attempts = 0
        max_attempts = count * 3

        # First, ensure each dataset has at least some sensors
        print("  🔗 Ensuring each dataset has associated sensors...")
        for dataset_id, info in dataset_type_associations.items():
            # Create 5-10 timeseries for each dataset with its associated types
            sensors_for_dataset = random.sample(self.sensor_ids, min(10, len(self.sensor_ids)))

            for sensor_id in sensors_for_dataset:
                # Pick 1-3 types from this dataset's types
                num_types = min(3, len(info['type_ids']))
                selected_types = random.sample(info['type_ids'], random.randint(1, num_types))

                for type_id in selected_types:
                    try:
                        self.cursor.execute(f"""
                            INSERT INTO {SCHEMA}.timeseries (sensor_id, type_id)
                            VALUES (%s, %s) RETURNING id
                        """, (sensor_id, type_id))

                        timeseries_id = self.cursor.fetchone()['id']
                        # Commit immediately to avoid rollback issues
                        self.conn.commit()

                        # Only add to memory after successful commit
                        self.timeseries_ids.append({
                            'id': timeseries_id,
                            'sensor_id': sensor_id,
                            'type_id': type_id,
                            'dataset_id': dataset_id,
                            'data_type': types_info[type_id]['data_type']
                        })
                        created_count += 1

                        if created_count >= count:
                            break

                    except psycopg.IntegrityError:
                        # Unique constraint violation - sensor already has this type
                        self.conn.rollback()
                        continue

        # Fill remaining count with random assignments
        print(f"  🎲 Creating additional random timeseries...")
        while created_count < count and attempts < max_attempts:
            sensor_id = random.choice(self.sensor_ids)
            type_id = random.choice(self.type_ids)

            attempts += 1

            try:
                self.cursor.execute(f"""
                    INSERT INTO {SCHEMA}.timeseries (sensor_id, type_id)
                    VALUES (%s, %s) RETURNING id
                """, (sensor_id, type_id))

                timeseries_id = self.cursor.fetchone()['id']
                # Commit immediately to avoid rollback issues
                self.conn.commit()

                # Only add to memory after successful commit
                self.timeseries_ids.append({
                    'id': timeseries_id,
                    'sensor_id': sensor_id,
                    'type_id': type_id,
                    'data_type': types_info[type_id]['data_type']
                })
                created_count += 1

            except psycopg.IntegrityError:
                # Unique constraint violation - sensor already has this type
                self.conn.rollback()
                continue

        print(f"✅ Created {created_count} timeseries (attempted {attempts} times)")
        print(f"🔍 DEBUG: Total timeseries in memory: {len(self.timeseries_ids)}")
        if self.timeseries_ids:
            print(f"🔍 DEBUG: First few timeseries: {self.timeseries_ids[:3]}")

    def populate_measurements(self, days_back: int = 30, measurements_per_day: int = 100):
        """Populate measurements across all data types"""
        print(f"📏 Creating measurements for last {days_back} days...")
        print(f"🔍 DEBUG: Found {len(self.timeseries_ids)} timeseries to process")

        if not self.timeseries_ids:
            print("❌ ERROR: No timeseries found! Measurements cannot be created.")
            return

        total_measurements = 0
        end_date = datetime.now()
        start_date = end_date - timedelta(days=days_back)

        # Group timeseries by data type for batch processing
        timeseries_by_type = {}
        for ts in self.timeseries_ids:
            data_type = ts['data_type']
            if data_type not in timeseries_by_type:
                timeseries_by_type[data_type] = []
            timeseries_by_type[data_type].append(ts)

        print(f"🔍 DEBUG: Timeseries grouped by type: {[(dt, len(tlist)) for dt, tlist in timeseries_by_type.items()]}")

        for data_type, timeseries_list in timeseries_by_type.items():
            print(f"  📊 Creating {data_type} measurements...")

            measurements = []
            for ts in timeseries_list:
                # Create measurements for each day
                current_date = start_date
                while current_date <= end_date:
                    for _ in range(random.randint(50, measurements_per_day)):
                        timestamp = current_date + timedelta(
                            hours=random.randint(0, 23),
                            minutes=random.randint(0, 59),
                            seconds=random.randint(0, 59),
                            microseconds=random.randint(0, 999999)
                        )

                        value = self._generate_measurement_value(data_type, ts['type_id'])
                        provenance_id = random.choice(self.provenance_ids) if random.random() < 0.8 else None

                        measurements.append((ts['id'], timestamp, value, provenance_id))

                        if len(measurements) >= self.batch_size:
                            self._insert_measurement_batch(data_type, measurements)
                            total_measurements += len(measurements)
                            measurements = []

                    current_date += timedelta(days=1)

            # Insert remaining measurements
            if measurements:
                self._insert_measurement_batch(data_type, measurements)
                total_measurements += len(measurements)

        print(f"✅ Created {total_measurements:,} measurements across all types")

    def _insert_measurement_batch(self, data_type: str, measurements: List):
        """Insert a batch of measurements for specific data type"""
        if not measurements:
            return

        table_name = f"{SCHEMA}.measurements_{data_type}"

        if data_type in ['geoposition', 'geoshape']:
            # Special handling for spatial data
            query = f"""
                INSERT INTO {table_name} (timeseries_id, timestamp, value, provenance_id)
                VALUES (%s, %s, ST_GeomFromText(%s, 4326), %s)
            """
        else:
            query = f"""
                INSERT INTO {table_name} (timeseries_id, timestamp, value, provenance_id)
                VALUES (%s, %s, %s, %s)
            """

        try:
            self.cursor.executemany(query, measurements)
            self.conn.commit()
        except Exception as e:
            print(f"  ⚠️  Error inserting {data_type} batch: {e}")
            self.conn.rollback()

    def _generate_measurement_value(self, data_type: str, type_id: int):
        """Generate realistic measurement values based on data type"""
        if data_type == 'numeric':
            # Get type name to generate realistic ranges
            type_name = None
            for ts in self.timeseries_ids:
                if ts['type_id'] == type_id:
                    # We need to look up the type name
                    break

            # Generate based on common patterns
            if type_id % 7 == 0:  # Temperature-like
                return round(random.uniform(-10, 35), 1)
            elif type_id % 7 == 1:  # Humidity-like
                return round(random.uniform(20, 95), 1)
            elif type_id % 7 == 2:  # Pressure-like
                return round(random.uniform(980, 1050), 1)
            elif type_id % 7 == 3:  # PM2.5-like
                return round(random.uniform(5, 150), 1)
            elif type_id % 7 == 4:  # Wind speed-like
                return round(random.uniform(0, 25), 1)
            elif type_id % 7 == 5:  # Battery level-like
                return round(random.uniform(10, 100), 1)
            else:  # General numeric
                return round(random.uniform(0, 1000), 2)

        elif data_type == 'string':
            statuses = ['online', 'offline', 'maintenance', 'error', 'calibrating', 'active', 'standby']
            error_codes = ['OK', 'E001', 'E002', 'WARN001', 'CAL_REQUIRED', 'LOW_BATTERY']
            return random.choice(statuses + error_codes)

        elif data_type == 'boolean':
            return random.choice([True, False])

        elif data_type == 'json':
            sample_configs = [
                {"sampling_interval": random.randint(1, 60), "threshold": random.uniform(0, 100)},
                {"calibration_offset": random.uniform(-5, 5), "filter_enabled": random.choice([True, False])},
                {"alert_levels": [random.uniform(0, 50), random.uniform(50, 100)], "units": "standard"},
                {"diagnostics": {"last_calibration": fake.date_time().isoformat(), "errors": random.randint(0, 5)}}
            ]
            return json.dumps(random.choice(sample_configs))

        elif data_type == 'geoposition':
            # Generate points in South Tyrol area
            lat = round(random.uniform(46.2, 47.1), 6)
            lon = round(random.uniform(10.4, 12.6), 6)
            return f"POINT({lon} {lat})"

        elif data_type == 'geoshape':
            # Generate simple polygons
            center_lat = round(random.uniform(46.2, 47.1), 6)
            center_lon = round(random.uniform(10.4, 12.6), 6)
            offset = 0.01

            points = [
                f"{center_lon - offset} {center_lat - offset}",
                f"{center_lon + offset} {center_lat - offset}",
                f"{center_lon + offset} {center_lat + offset}",
                f"{center_lon - offset} {center_lat + offset}",
                f"{center_lon - offset} {center_lat - offset}"  # Close the polygon
            ]
            return f"POLYGON(({', '.join(points)}))"

        return None

    def print_summary(self):
        """Print summary of populated data"""
        print("\n" + "="*60)
        print("📊 DATABASE POPULATION SUMMARY")
        print("="*60)

        tables = [
            (f'{SCHEMA}.provenance', 'Provenance Records'),
            (f'{SCHEMA}.sensors', 'Sensors'),
            (f'{SCHEMA}.types', 'Measurement Types'),
            (f'{SCHEMA}.datasets', 'Datasets'),
            (f'{SCHEMA}.dataset_types', 'Dataset-Type Relations'),
            (f'{SCHEMA}.timeseries', 'Timeseries'),
            (f'{SCHEMA}.measurements_numeric', 'Numeric Measurements'),
            (f'{SCHEMA}.measurements_string', 'String Measurements'),
            (f'{SCHEMA}.measurements_boolean', 'Boolean Measurements'),
            (f'{SCHEMA}.measurements_json', 'JSON Measurements'),
            (f'{SCHEMA}.measurements_geoposition', 'Geoposition Measurements'),
            (f'{SCHEMA}.measurements_geoshape', 'Geoshape Measurements'),
        ]

        total_records = 0
        for table_name, description in tables:
            try:
                self.cursor.execute(f"SELECT COUNT(*) as count FROM {table_name}")
                count = self.cursor.fetchone()['count']
                print(f"{description:.<30} {count:>8,} records")
                total_records += count
            except Exception as e:
                print(f"{description:.<30} {'ERROR':>8}")

        print("-" * 60)
        print(f"{'TOTAL RECORDS':.<30} {total_records:>8,}")
        print("="*60)

        # Sample queries with actual dataset IDs
        print("\n🔍 SAMPLE QUERIES TO TRY:")
        print("-" * 60)

        # Get first dataset ID for examples
        if self.dataset_ids:
            sample_dataset_id = self.dataset_ids[0]

            # Get dataset name for better examples
            self.cursor.execute(f"SELECT id, name FROM {SCHEMA}.datasets LIMIT 5")
            datasets = self.cursor.fetchall()

            print("# Test dataset retrieval:")
            for dataset in datasets:
                print(f"curl 'http://localhost:8080/api/v1/datasets/{dataset['id']}'  # {dataset['name']}")

            print(f"\n# Get sensors in a dataset:")
            print(f"curl 'http://localhost:8080/api/v1/datasets/{sample_dataset_id}/sensors'")

            print(f"\n# Find sensors by dataset ID:")
            print(f"curl 'http://localhost:8080/api/v1/sensors/dataset/{sample_dataset_id}'")

        print("\n# Find sensors with temperature measurements:")
        print("curl 'http://localhost:8080/api/v1/sensors/types?type_names=air_temperature'")
        print("\n# Find sensors with ALL weather types:")
        print("curl -X POST http://localhost:8080/api/v1/sensors/types -H 'Content-Type: application/json' -d '{\"type_names\": [\"air_temperature\", \"relative_humidity\", \"atmospheric_pressure\"], \"require_all\": true}'")
        print("\n# Get latest measurements:")
        print("curl 'http://localhost:8080/api/v1/measurements/latest?sensor_names=TEMP_Downtown_001&type_names=air_temperature'")
        print("\n# API Documentation:")
        print("curl 'http://localhost:8080/api/v1/info'")
        print("# Swagger UI: http://localhost:8080/api/swagger/index.html")

def main():
    parser = argparse.ArgumentParser(description='Populate timeseries database with test data')
    parser.add_argument('--clean', action='store_true', help='Clean existing data before populating')
    parser.add_argument('--batch-size', type=int, default=1000, help='Batch size for measurements')
    parser.add_argument('--sensors', type=int, default=100, help='Number of sensors to create')
    parser.add_argument('--timeseries', type=int, default=500, help='Number of timeseries to create')
    parser.add_argument('--days', type=int, default=30, help='Days of historical measurements')
    parser.add_argument('--measurements-per-day', type=int, default=100, help='Measurements per timeseries per day')

    args = parser.parse_args()

    print("🚀 Starting database population...")
    print(f"Database: {DB_CONFIG['dbname']}@{DB_CONFIG['host']}")
    print(f"Schema: {SCHEMA}")

    populator = DatabasePopulator(batch_size=args.batch_size)

    try:
        populator.connect()
        populator.create_partitions()

        if args.clean:
            populator.clean_database()

        # Populate in dependency order
        populator.populate_provenance(count=10)
        populator.populate_sensors(count=args.sensors)
        populator.populate_types()
        populator.populate_datasets(count=15)
        populator.populate_timeseries(count=args.timeseries)
        populator.populate_measurements(days_back=args.days, measurements_per_day=args.measurements_per_day)

        populator.print_summary()

    except KeyboardInterrupt:
        print("\n⚠️  Population interrupted by user")
    except Exception as e:
        print(f"❌ Error during population: {e}")
        raise
    finally:
        populator.disconnect()

    print("\n✅ Database population completed!")

if __name__ == '__main__':
    main()