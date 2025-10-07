#!/usr/bin/env python3
"""
Continuous measurement generator for all sensor-timeseries pairs.
Generates realistic measurements every 10 seconds with verisimilar changes.
"""

import psycopg2
import requests
import time
import random
import json
from datetime import datetime, timezone
from typing import Dict, List, Any, Optional
import sys

# Configuration
DB_HOST = "localhost"
DB_PORT = 5556
DB_NAME = "timeseries"
DB_USER = "bdp"
DB_PASSWORD = "password"
API_URL = "http://localhost:8080/api/v1/measurements/batch"
INTERVAL_SECONDS = 10

# State storage for continuous evolution
class MeasurementState:
    def __init__(self):
        self.last_values: Dict[int, Any] = {}  # timeseries_id -> last_value
        self.iteration = 0

    def get_last_value(self, timeseries_id: int) -> Optional[Any]:
        return self.last_values.get(timeseries_id)

    def set_last_value(self, timeseries_id: int, value: Any):
        self.last_values[timeseries_id] = value

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

def parse_wkt_point(wkt: str) -> tuple:
    """Parse WKT POINT to (lon, lat)"""
    # POINT(11.356 46.498) -> (11.356, 46.498)
    coords = wkt.replace("POINT(", "").replace(")", "").split()
    return (float(coords[0]), float(coords[1]))

def parse_wkt_polygon(wkt: str) -> List[List[float]]:
    """Parse WKT POLYGON to list of [lon, lat] pairs"""
    # POLYGON((11.3 46.5, 11.4 46.5, 11.4 46.6, 11.3 46.6, 11.3 46.5))
    coords_str = wkt.replace("POLYGON((", "").replace("))", "")
    points = []
    for coord in coords_str.split(", "):
        lon, lat = coord.split()
        points.append([float(lon), float(lat)])
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
    new_points = []
    for point in points[:-1]:  # Exclude last point (it's same as first)
        dlon = random.uniform(-max_change, max_change)
        dlat = random.uniform(-max_change, max_change)
        new_points.append([point[0] + dlon, point[1] + dlat])

    # Close the polygon
    new_points.append(new_points[0].copy())
    return new_points

def generate_value(timeseries_id: int, data_type: str, type_name: str, unit: Optional[str]) -> Any:
    """Generate a realistic value based on data type and previous value"""
    last_value = state.get_last_value(timeseries_id)

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
        # Move point slightly to simulate movement
        if last_value is None:
            # Initialize in South Tyrol area
            lon = random.uniform(10.5, 12.5)
            lat = random.uniform(46.2, 47.2)
        else:
            # Parse last position and move it
            if isinstance(last_value, dict):
                # Already GeoJSON format
                lon, lat = last_value["coordinates"]
            elif isinstance(last_value, str):
                # WKT format
                lon, lat = parse_wkt_point(last_value)
            else:
                lon = random.uniform(10.5, 12.5)
                lat = random.uniform(46.2, 47.2)

            # Move the point
            lon, lat = move_point(lon, lat, max_distance_km=0.05)

        # Return GeoJSON Point
        return {
            "type": "Point",
            "coordinates": [round(lon, 6), round(lat, 6)]
        }

    elif data_type == "geoshape":
        # Evolve polygon shape slightly
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
            else:
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

            # Evolve the polygon
            points = evolve_polygon(points, max_change=0.001)

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

    for ts in timeseries_list:
        value = generate_value(
            ts['timeseries_id'],
            ts['data_type'],
            ts['type_name'],
            ts.get('unit')
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
    print("=" * 70)
    print()

    print("📊 Loading timeseries from database...")
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
