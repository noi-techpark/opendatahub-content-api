#!/usr/bin/env python3
"""
Continuous measurement generator for all sensor-timeseries pairs.
Generates realistic measurements every 10 seconds with verisimilar changes.
Uses historical data from the database for continuity.
"""

import psycopg2
import requests
import time
import random
import json
import math
from datetime import datetime, timezone
from typing import Dict, List, Any, Optional, Tuple
import sys

# Configuration
DB_HOST = "localhost"
DB_PORT = 5556
DB_NAME = "timeseries"
DB_USER = "bdp"
DB_PASSWORD = "password"
SCHEMA = "intimev3"
API_URL = "http://localhost:8080/api/v1/measurements/batch"
INTERVAL_SECONDS = 10

# State storage for continuous evolution
class MeasurementState:
    def __init__(self):
        self.last_values: Dict[int, Any] = {}  # timeseries_id -> last_value
        self.typhoon_states: Dict[str, Dict] = {}  # sensor_name -> {direction, speed, position}
        self.iteration = 0
        self.db_conn = None

    def get_last_value(self, timeseries_id: int) -> Optional[Any]:
        return self.last_values.get(timeseries_id)

    def set_last_value(self, timeseries_id: int, value: Any):
        self.last_values[timeseries_id] = value

    def connect_db(self):
        """Connect to database for fetching historical data"""
        if self.db_conn is None or self.db_conn.closed:
            self.db_conn = psycopg2.connect(
                host=DB_HOST,
                port=DB_PORT,
                dbname=DB_NAME,
                user=DB_USER,
                password=DB_PASSWORD
            )

    def fetch_latest_from_db(self, timeseries_id: int, data_type: str) -> Optional[Any]:
        """Fetch the latest measurement value from the database"""
        self.connect_db()
        cursor = self.db_conn.cursor()

        table_name = f"{SCHEMA}.measurements_{data_type}"

        try:
            if data_type in ['geoposition', 'geoshape']:
                cursor.execute(f"""
                    SELECT ST_AsText(value) as value
                    FROM {table_name}
                    WHERE timeseries_id = %s
                    ORDER BY timestamp DESC
                    LIMIT 1
                """, (timeseries_id,))
            else:
                cursor.execute(f"""
                    SELECT value
                    FROM {table_name}
                    WHERE timeseries_id = %s
                    ORDER BY timestamp DESC
                    LIMIT 1
                """, (timeseries_id,))

            row = cursor.fetchone()
            cursor.close()

            if row:
                value = row[0]
                # Parse JSON if needed
                if data_type == 'json' and isinstance(value, str):
                    return json.loads(value)
                return value
            return None
        except Exception as e:
            print(f"  ⚠️  Error fetching latest value: {e}")
            cursor.close()
            return None

state = MeasurementState()

def get_all_timeseries() -> List[Dict]:
    """Fetch all sensor-timeseries pairs from the database"""
    conn = psycopg2.connect(
        host=DB_HOST,
        port=DB_PORT,
        dbname=DB_NAME,
        user=DB_USER,
        password=DB_PASSWORD
    )

    cursor = conn.cursor()
    query = """
        SELECT
            ts.id as timeseries_id,
            s.name as sensor_name,
            t.name as type_name,
            t.data_type,
            t.unit
        FROM intimev3.timeseries ts
        JOIN intimev3.sensors s ON ts.sensor_id = s.id
        JOIN intimev3.types t ON ts.type_id = t.id
        WHERE s.is_active = true
        ORDER BY ts.id
    """

    cursor.execute(query)
    columns = [desc[0] for desc in cursor.description]
    results = []

    for row in cursor.fetchall():
        results.append(dict(zip(columns, row)))

    cursor.close()
    conn.close()

    return results

def parse_wkt_point(wkt: str) -> Tuple[float, float]:
    """Parse WKT POINT to (lon, lat)"""
    # POINT(11.356 46.498) -> (11.356, 46.498)
    coords = wkt.replace("POINT(", "").replace(")", "").split()
    return (float(coords[0]), float(coords[1]))

def move_position_spherical(lat: float, lon: float, direction_deg: float, distance_km: float) -> Tuple[float, float]:
    """
    Calculate new position using spherical geometry (same as populate_typhoons.py)

    Args:
        lat: Starting latitude in degrees
        lon: Starting longitude in degrees
        direction_deg: Direction in degrees (0 = north, 90 = east)
        distance_km: Distance to move in kilometers

    Returns:
        Tuple of (new_lat, new_lon) in degrees
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

def calculate_bearing(lat1: float, lon1: float, lat2: float, lon2: float) -> float:
    """Calculate bearing between two points in degrees"""
    lat1_rad = math.radians(lat1)
    lat2_rad = math.radians(lat2)
    lon_diff = math.radians(lon2 - lon1)

    x = math.sin(lon_diff) * math.cos(lat2_rad)
    y = math.cos(lat1_rad) * math.sin(lat2_rad) - math.sin(lat1_rad) * math.cos(lat2_rad) * math.cos(lon_diff)

    bearing = math.atan2(x, y)
    return (math.degrees(bearing) + 360) % 360

def get_typhoon_movement_state(sensor_name: str, timeseries_id: int) -> Dict:
    """Get or initialize typhoon movement state"""
    if sensor_name not in state.typhoon_states:
        # Try to fetch last 2 positions to calculate direction and speed
        cursor = state.db_conn.cursor()

        try:
            cursor.execute(f"""
                SELECT ST_AsText(value) as value, timestamp
                FROM {SCHEMA}.measurements_geoposition
                WHERE timeseries_id = %s
                ORDER BY timestamp DESC
                LIMIT 2
            """, (timeseries_id,))

            rows = cursor.fetchall()
            cursor.close()

            if len(rows) >= 2:
                # Calculate direction and speed from last two positions
                pos1_wkt, time1 = rows[1]
                pos2_wkt, time2 = rows[0]

                lon1, lat1 = parse_wkt_point(pos1_wkt)
                lon2, lat2 = parse_wkt_point(pos2_wkt)

                # Calculate bearing
                direction = calculate_bearing(lat1, lon1, lat2, lon2)

                # Calculate speed (assuming 10 minute interval)
                time_diff = (time2 - time1).total_seconds() / 3600  # hours
                distance_km = haversine_distance(lat1, lon1, lat2, lon2)
                speed = distance_km / time_diff if time_diff > 0 else 25.0

                state.typhoon_states[sensor_name] = {
                    'direction': direction,
                    'speed': speed,
                    'lat': lat2,
                    'lon': lon2
                }
            else:
                # Initialize with default values
                state.typhoon_states[sensor_name] = {
                    'direction': random.uniform(280, 350),  # Northwest
                    'speed': random.uniform(20, 35),  # km/h
                    'lat': None,
                    'lon': None
                }
        except Exception as e:
            print(f"  ⚠️  Error initializing typhoon state: {e}")
            cursor.close()
            state.typhoon_states[sensor_name] = {
                'direction': random.uniform(280, 350),
                'speed': random.uniform(20, 35),
                'lat': None,
                'lon': None
            }

    return state.typhoon_states[sensor_name]

def haversine_distance(lat1: float, lon1: float, lat2: float, lon2: float) -> float:
    """Calculate distance between two points in km"""
    R = 6371.0  # Earth radius in km

    lat1_rad = math.radians(lat1)
    lat2_rad = math.radians(lat2)
    dlat = math.radians(lat2 - lat1)
    dlon = math.radians(lon2 - lon1)

    a = math.sin(dlat/2)**2 + math.cos(lat1_rad) * math.cos(lat2_rad) * math.sin(dlon/2)**2
    c = 2 * math.atan2(math.sqrt(a), math.sqrt(1-a))

    return R * c

def parse_wkt_polygon(wkt: str) -> List[List[float]]:
    """Parse WKT POLYGON to list of [lon, lat] pairs"""
    # Handle different formats:
    # POLYGON((11.3 46.5, 11.4 46.5, ...))  <- space-separated
    # POLYGON((46.7,10.5 46.8,10.6, ...))   <- comma within point, space between points
    # POLYGON((46.7,10.5,46.8,10.6,...))    <- all comma-separated

    # Strip POLYGON wrapper
    coords_str = wkt.upper()
    for prefix in ["POLYGON Z((", "POLYGON M((", "POLYGON(("]:
        if coords_str.startswith(prefix):
            coords_str = wkt[len(prefix):-2]
            break

    points = []

    # Try standard WKT format first: "lon1 lat1, lon2 lat2, ..."
    if ',' in coords_str and ' ' in coords_str:
        for coord_pair in coords_str.split(','):
            coord_pair = coord_pair.strip()
            if not coord_pair:
                continue
            parts = coord_pair.split()
            if len(parts) >= 2:
                try:
                    lon, lat = float(parts[0]), float(parts[1])
                    points.append([lon, lat])
                except ValueError:
                    continue
    else:
        # Fallback: try comma-separated within each coordinate
        for coord in coords_str.split():
            coord = coord.strip(',')
            if not coord:
                continue

            # Check if coord contains comma (lat,lon format)
            if ',' in coord:
                parts = coord.split(',')
            else:
                parts = [coord]

            if len(parts) >= 2:
                try:
                    lon, lat = float(parts[0]), float(parts[1])
                    points.append([lon, lat])
                except ValueError:
                    continue

    # Validate: must have at least 4 points for a valid polygon (3 unique + 1 closing)
    if len(points) < 4:
        print(f"  ⚠️  Warning: Parsed polygon has only {len(points)} points, need at least 4")
        return []

    return points

def move_point(lon: float, lat: float, max_distance_km: float = 0.1) -> tuple:
    """Move a point by a small random distance (simulates movement)"""
    # Convert km to degrees (approximate)
    max_deg = max_distance_km / 111.0  # 1 degree ≈ 111 km

    dlat = random.uniform(-max_deg, max_deg)
    dlon = random.uniform(-max_deg, max_deg)

    return (lon + dlon, lat + dlat)

def evolve_polygon(points: List[List[float]], max_change: float = 0.01) -> List[List[float]]:
    """Evolve a polygon shape slightly (simulates area change)"""
    if len(points) < 4:
        # Not enough points, return as-is
        return points

    new_points = []
    # Exclude last point (it's same as first in closed polygons)
    unique_points = points[:-1] if len(points) > 0 and points[0] == points[-1] else points

    for point in unique_points:
        dlon = random.uniform(-max_change, max_change)
        dlat = random.uniform(-max_change, max_change)
        new_points.append([point[0] + dlon, point[1] + dlat])

    # Close the polygon (first point = last point)
    if len(new_points) > 0:
        new_points.append(new_points[0].copy())

    return new_points

def generate_value(timeseries_id: int, data_type: str, type_name: str, unit: Optional[str], sensor_name: str) -> Any:
    """Generate a realistic value based on data type and previous value"""
    last_value = state.get_last_value(timeseries_id)

    # If no value in memory, fetch from database
    if last_value is None:
        last_value = state.fetch_latest_from_db(timeseries_id, data_type)
        if last_value is not None:
            state.set_last_value(timeseries_id, last_value)

    if data_type == "numeric":
        if last_value is None:
            # Initialize with realistic base values based on type/unit
            if "temperature" in type_name.lower():
                base = random.uniform(15.0, 25.0)
            elif "humidity" in type_name.lower():
                base = random.uniform(40.0, 70.0)
            elif "power" in type_name.lower() or "generation" in type_name.lower():
                base = random.uniform(100.0, 500.0)
            elif "speed" in type_name.lower():
                base = random.uniform(50.0, 120.0)
            else:
                base = random.uniform(10.0, 100.0)
        else:
            # Evolve from previous value with small random walk
            base = last_value

        # Add small variation
        variation = base * random.uniform(-0.05, 0.05)  # ±5% change
        new_value = round(base + variation, 2)

        # Apply realistic bounds
        if "temperature" in type_name.lower():
            new_value = max(-10.0, min(40.0, new_value))
        elif "humidity" in type_name.lower():
            new_value = max(0.0, min(100.0, new_value))
        elif "power" in type_name.lower():
            new_value = max(0.0, new_value)

        return new_value

    elif data_type == "string":
        # Cycle through realistic status values
        statuses = ["active", "idle", "warning", "offline", "ok", "error"]
        if last_value in statuses:
            # 80% chance to stay same, 20% chance to change
            if random.random() < 0.8:
                return last_value
            else:
                return random.choice([s for s in statuses if s != last_value])
        return random.choice(statuses)

    elif data_type == "boolean":
        # 90% chance to stay same, 10% chance to flip
        if last_value is None:
            return random.choice([True, False])
        elif random.random() < 0.9:
            return last_value
        else:
            return not last_value

    elif data_type == "json":
        # Generate realistic JSON data with evolution
        if last_value is None:
            base_data = {
                "status": "operational",
                "counter": 0,
                "metrics": {
                    "avg": random.uniform(50.0, 150.0),
                    "max": random.uniform(150.0, 200.0)
                }
            }
        else:
            # Evolve previous JSON
            base_data = last_value.copy() if isinstance(last_value, dict) else {"counter": 0}
            base_data["counter"] = base_data.get("counter", 0) + 1
            if "metrics" in base_data:
                base_data["metrics"]["avg"] = round(
                    base_data["metrics"]["avg"] * random.uniform(0.95, 1.05), 2
                )

        return base_data

    elif data_type == "geoposition":
        # Check if this is a typhoon-related type
        is_typhoon = "typhoon" in type_name.lower()

        if is_typhoon:
            # Special handling for typhoons with realistic movement
            typhoon_state = get_typhoon_movement_state(sensor_name, timeseries_id)

            # Get current position
            if last_value is None:
                # Initialize in Western Pacific
                lat = random.uniform(8.0, 18.0)
                lon = random.uniform(125.0, 160.0)
            else:
                if isinstance(last_value, dict):
                    lon, lat = last_value["coordinates"]
                elif isinstance(last_value, str):
                    lon, lat = parse_wkt_point(last_value)
                else:
                    lat = random.uniform(8.0, 18.0)
                    lon = random.uniform(125.0, 160.0)

            # Update typhoon state position
            if typhoon_state['lat'] is None:
                typhoon_state['lat'] = lat
                typhoon_state['lon'] = lon

            # For predictions, return predicted position
            if "prediction" in type_name.lower():
                # Extract prediction time
                if "30min" in type_name:
                    hours = 0.5
                elif "60min" in type_name:
                    hours = 1.0
                elif "120min" in type_name:
                    hours = 2.0
                else:
                    hours = 1.0

                # Calculate predicted position
                distance_km = typhoon_state['speed'] * hours
                pred_lat, pred_lon = move_position_spherical(
                    typhoon_state['lat'],
                    typhoon_state['lon'],
                    typhoon_state['direction'],
                    distance_km
                )

                return {
                    "type": "Point",
                    "coordinates": [round(pred_lon, 6), round(pred_lat, 6)]
                }
            else:
                # This is the current position - update it
                # Movement in 10 minutes = 1/6 hour
                time_delta_hours = 1/6

                # Direction changes slightly
                direction_change = random.uniform(-3, 3)
                typhoon_state['direction'] += direction_change
                typhoon_state['direction'] = typhoon_state['direction'] % 360

                # Speed varies slightly
                speed_change = random.uniform(-2, 2)
                typhoon_state['speed'] += speed_change
                typhoon_state['speed'] = max(10, min(50, typhoon_state['speed']))

                # Calculate new position
                distance_km = typhoon_state['speed'] * time_delta_hours
                new_lat, new_lon = move_position_spherical(
                    typhoon_state['lat'],
                    typhoon_state['lon'],
                    typhoon_state['direction'],
                    distance_km
                )

                # Update state
                typhoon_state['lat'] = new_lat
                typhoon_state['lon'] = new_lon

                return {
                    "type": "Point",
                    "coordinates": [round(new_lon, 6), round(new_lat, 6)]
                }
        else:
            # Regular geoposition (non-typhoon) - small movements
            if last_value is None:
                # Initialize in South Tyrol area
                lon = random.uniform(10.5, 12.5)
                lat = random.uniform(46.2, 47.2)
            else:
                # Parse last position and move it
                if isinstance(last_value, dict):
                    lon, lat = last_value["coordinates"]
                elif isinstance(last_value, str):
                    lon, lat = parse_wkt_point(last_value)
                else:
                    lon = random.uniform(10.5, 12.5)
                    lat = random.uniform(46.2, 47.2)

                # Move the point
                lon, lat = move_point(lon, lat, max_distance_km=0.05)

            return {
                "type": "Point",
                "coordinates": [round(lon, 6), round(lat, 6)]
            }

    elif data_type == "geoshape":
        # Evolve polygon shape slightly
        points = None

        if last_value is None:
            # Initialize a small square polygon in South Tyrol
            base_lon = random.uniform(10.5, 12.5)
            base_lat = random.uniform(46.2, 47.2)
            size = 0.01  # ~1km

            points = [
                [base_lon, base_lat],
                [base_lon + size, base_lat],
                [base_lon + size, base_lat + size],
                [base_lon, base_lat + size],
                [base_lon, base_lat]  # Close the ring
            ]
        else:
            # Parse last shape and evolve it
            if isinstance(last_value, dict):
                # GeoJSON format
                points = last_value["coordinates"][0]
            elif isinstance(last_value, str):
                # WKT format
                points = parse_wkt_polygon(last_value)

                # If parsing failed or returned too few points, regenerate
                if not points or len(points) < 4:
                    print(f"  ⚠️  Failed to parse WKT polygon, regenerating")
                    points = None

            if points is None:
                # Fallback: create new polygon
                base_lon = random.uniform(10.5, 12.5)
                base_lat = random.uniform(46.2, 47.2)
                size = 0.01
                points = [
                    [base_lon, base_lat],
                    [base_lon + size, base_lat],
                    [base_lon + size, base_lat + size],
                    [base_lon, base_lat + size],
                    [base_lon, base_lat]
                ]
            else:
                # Evolve the polygon only if we have valid points
                points = evolve_polygon(points, max_change=0.001)

        # Final validation: ensure at least 4 points
        if len(points) < 4:
            print(f"  ⚠️  Polygon has too few points ({len(points)}), regenerating")
            base_lon = random.uniform(10.5, 12.5)
            base_lat = random.uniform(46.2, 47.2)
            size = 0.01
            points = [
                [base_lon, base_lat],
                [base_lon + size, base_lat],
                [base_lon + size, base_lat + size],
                [base_lon, base_lat + size],
                [base_lon, base_lat]
            ]

        # Return GeoJSON Polygon
        return {
            "type": "Polygon",
            "coordinates": [[[round(p[0], 6), round(p[1], 6)] for p in points]]
        }

    else:
        # Fallback for unknown types
        return random.uniform(0.0, 100.0)

def generate_measurements(timeseries_list: List[Dict]) -> List[Dict]:
    """Generate measurements for all timeseries"""
    measurements = []
    now = datetime.now(timezone.utc)

    # Group typhoon measurements by sensor to maintain consistency
    typhoon_sensors = {}
    non_typhoon_measurements = []

    for ts in timeseries_list:
        if "typhoon" in ts['sensor_name'].lower():
            sensor_name = ts['sensor_name']
            if sensor_name not in typhoon_sensors:
                typhoon_sensors[sensor_name] = []
            typhoon_sensors[sensor_name].append(ts)
        else:
            non_typhoon_measurements.append(ts)

    # Process typhoon measurements (position must be calculated before predictions)
    for sensor_name, sensor_timeseries in typhoon_sensors.items():
        # Sort to ensure position is calculated before predictions
        sensor_timeseries.sort(key=lambda x: (
            0 if x['type_name'] == 'typhoon_position' else
            1 if 'wind' in x['type_name'] or 'radius' in x['type_name'] else
            2
        ))

        for ts in sensor_timeseries:
            value = generate_value(
                ts['timeseries_id'],
                ts['data_type'],
                ts['type_name'],
                ts.get('unit'),
                ts['sensor_name']
            )

            # Store the generated value for next iteration
            state.set_last_value(ts['timeseries_id'], value)

            measurement = {
                "sensor_name": ts['sensor_name'],
                "type_name": ts['type_name'],
                "timestamp": now.isoformat(),
                "value": value
            }

            measurements.append(measurement)

    # Process non-typhoon measurements
    for ts in non_typhoon_measurements:
        value = generate_value(
            ts['timeseries_id'],
            ts['data_type'],
            ts['type_name'],
            ts.get('unit'),
            ts['sensor_name']
        )

        # Store the generated value for next iteration
        state.set_last_value(ts['timeseries_id'], value)

        measurement = {
            "sensor_name": ts['sensor_name'],
            "type_name": ts['type_name'],
            "timestamp": now.isoformat(),
            "value": value
        }

        measurements.append(measurement)

    return measurements

def send_batch(measurements: List[Dict]) -> bool:
    """Send measurements via batch API"""
    payload = {
        "provenance": {
            "lineage": "measurement_generator",
            "data_collector": "continuous_generator",
            "data_collector_version": "1.0.0"
        },
        "measurements": measurements
    }

    try:
        response = requests.post(API_URL, json=payload, timeout=30)
        if response.status_code == 200:
            return True
        else:
            print(f"❌ Batch insert failed: {response.status_code} - {response.text}")
            return False
    except Exception as e:
        print(f"❌ Error sending batch: {e}")
        return False

def main():
    print("=" * 70)
    print("  CONTINUOUS MEASUREMENT GENERATOR")
    print("  (Uses historical data for continuity)")
    print("=" * 70)
    print()

    print("📊 Loading timeseries from database...")
    state.connect_db()
    timeseries_list = get_all_timeseries()
    print(f"✓ Found {len(timeseries_list)} timeseries")
    print()

    # Show breakdown by data type
    type_counts = {}
    for ts in timeseries_list:
        dtype = ts['data_type']
        type_counts[dtype] = type_counts.get(dtype, 0) + 1

    print("Data type distribution:")
    for dtype, count in sorted(type_counts.items()):
        print(f"  - {dtype}: {count}")
    print()

    print(f"🔄 Generating measurements every {INTERVAL_SECONDS} seconds...")
    print(f"Press Ctrl+C to stop")
    print()

    try:
        while True:
            state.iteration += 1
            start_time = time.time()

            # Generate measurements
            measurements = generate_measurements(timeseries_list)

            # Send batch
            success = send_batch(measurements)

            elapsed = time.time() - start_time

            if success:
                timestamp = datetime.now().strftime("%H:%M:%S")
                print(f"[{timestamp}] Iteration {state.iteration}: ✓ Sent {len(measurements)} measurements ({elapsed:.2f}s)")
            else:
                timestamp = datetime.now().strftime("%H:%M:%S")
                print(f"[{timestamp}] Iteration {state.iteration}: ❌ Failed to send measurements")

            # Wait for next interval
            sleep_time = max(0, INTERVAL_SECONDS - elapsed)
            if sleep_time > 0:
                time.sleep(sleep_time)

    except KeyboardInterrupt:
        print()
        print()
        print("=" * 70)
        print(f"  STOPPED after {state.iteration} iterations")
        print("=" * 70)
        print()
        sys.exit(0)

if __name__ == "__main__":
    main()
