#!/usr/bin/env python3
"""
Comprehensive batch endpoint testing script
Tests /batch endpoint with various data types and validates performance
"""

import requests
import time
import json
from datetime import datetime, timezone, timedelta
import random
import sys

API_BASE_URL = "http://localhost:8080/api/v1"

class Colors:
    OKGREEN = '\033[92m'
    FAIL = '\033[91m'
    OKCYAN = '\033[96m'
    WARNING = '\033[93m'
    ENDC = '\033[0m'
    BOLD = '\033[1m'

def print_success(text):
    print(f"{Colors.OKGREEN}✓ {text}{Colors.ENDC}")

def print_error(text):
    print(f"{Colors.FAIL}✗ {text}{Colors.ENDC}")

def print_info(text):
    print(f"{Colors.OKCYAN}ℹ {text}{Colors.ENDC}")

def print_header(text):
    print(f"\n{Colors.BOLD}{'='*70}{Colors.ENDC}")
    print(f"{Colors.BOLD}{text:^70}{Colors.ENDC}")
    print(f"{Colors.BOLD}{'='*70}{Colors.ENDC}\n")

def test_batch_numeric():
    """Test batch insert with numeric measurements"""
    print_header("TEST 1: Numeric Measurements Batch Insert")

    payload = {
        "provenance": {
            "lineage": "test",
            "data_collector": "batch_test_script",
            "data_collector_version": "1.0.0"
        },
        "measurements": []
    }

    # Create 50 numeric measurements
    sensor_names = [f"HUM_Park_{str(i).zfill(3)}" for i in range(1, 11)]
    type_names = ["power_generation", "conductivity", "air_temperature"]

    now = datetime.now(timezone.utc)

    for i in range(50):
        measurement = {
            "sensor_name": random.choice(sensor_names),
            "type_name": random.choice(type_names),
            "timestamp": (now - timedelta(minutes=i)).isoformat(),
            "value": round(random.uniform(10.0, 50.0), 2)
        }
        payload["measurements"].append(measurement)

    print_info(f"Inserting {len(payload['measurements'])} numeric measurements...")

    start_time = time.time()
    try:
        response = requests.post(f"{API_BASE_URL}/measurements/batch", json=payload, timeout=30)
        elapsed = time.time() - start_time

        if response.status_code == 200:
            data = response.json()
            print_success(f"Batch insert successful in {elapsed:.3f}s")
            print_info(f"Response: {data}")
            print_info(f"Throughput: {len(payload['measurements']) / elapsed:.1f} measurements/sec")
            return True
        else:
            print_error(f"Failed with status {response.status_code}: {response.text}")
            return False
    except Exception as e:
        print_error(f"Request failed: {e}")
        return False


def test_batch_string():
    """Test batch insert with string measurements"""
    print_header("TEST 2: String Measurements Batch Insert")

    payload = {
        "provenance": {
            "lineage": "test",
            "data_collector": "batch_test_script",
            "data_collector_version": "1.0.0"
        },
        "measurements": []
    }

    sensor_names = [f"STATUS_Device_{str(i).zfill(3)}" for i in range(1, 6)]
    statuses = ["ACTIVE", "LOW_BATTERY", "MAINTENANCE", "OFFLINE", "ERROR"]

    now = datetime.now(timezone.utc)

    for i in range(20):
        measurement = {
            "sensor_name": random.choice(sensor_names),
            "type_name": "device_status",
            "timestamp": (now - timedelta(minutes=i)).isoformat(),
            "value": random.choice(statuses)
        }
        payload["measurements"].append(measurement)

    print_info(f"Inserting {len(payload['measurements'])} string measurements...")

    start_time = time.time()
    try:
        response = requests.post(f"{API_BASE_URL}/measurements/batch", json=payload, timeout=30)
        elapsed = time.time() - start_time

        if response.status_code == 200:
            data = response.json()
            print_success(f"Batch insert successful in {elapsed:.3f}s")
            print_info(f"Throughput: {len(payload['measurements']) / elapsed:.1f} measurements/sec")
            return True
        else:
            print_error(f"Failed with status {response.status_code}: {response.text}")
            return False
    except Exception as e:
        print_error(f"Request failed: {e}")
        return False


def test_batch_json():
    """Test batch insert with JSON measurements"""
    print_header("TEST 3: JSON Measurements Batch Insert")

    payload = {
        "provenance": {
            "lineage": "test",
            "data_collector": "batch_test_script",
            "data_collector_version": "1.0.0"
        },
        "measurements": []
    }

    sensor_names = [f"CONFIG_Sensor_{str(i).zfill(3)}" for i in range(1, 6)]

    now = datetime.now(timezone.utc)

    for i in range(15):
        config = {
            "sampling_interval": random.randint(10, 60),
            "filter_enabled": random.choice([True, False]),
            "threshold": random.randint(30, 90),
            "mode": random.choice(["normal", "eco", "performance"])
        }

        measurement = {
            "sensor_name": random.choice(sensor_names),
            "type_name": "sensor_config",
            "timestamp": (now - timedelta(minutes=i)).isoformat(),
            "value": config
        }
        payload["measurements"].append(measurement)

    print_info(f"Inserting {len(payload['measurements'])} JSON measurements...")

    start_time = time.time()
    try:
        response = requests.post(f"{API_BASE_URL}/measurements/batch", json=payload, timeout=30)
        elapsed = time.time() - start_time

        if response.status_code == 200:
            data = response.json()
            print_success(f"Batch insert successful in {elapsed:.3f}s")
            print_info(f"Throughput: {len(payload['measurements']) / elapsed:.1f} measurements/sec")
            return True
        else:
            print_error(f"Failed with status {response.status_code}: {response.text}")
            return False
    except Exception as e:
        print_error(f"Request failed: {e}")
        return False


def test_batch_geoposition():
    """Test batch insert with geoposition measurements"""
    print_header("TEST 4: Geoposition Measurements Batch Insert")

    payload = {
        "provenance": {
            "lineage": "test",
            "data_collector": "batch_test_script",
            "data_collector_version": "1.0.0"
        },
        "measurements": []
    }

    sensor_names = [f"PARK_Highway_{str(i).zfill(3)}" for i in range(1, 6)]

    now = datetime.now(timezone.utc)

    # Generate positions within South Tyrol area
    for i in range(10):
        lon = round(random.uniform(10.5, 12.5), 6)
        lat = round(random.uniform(46.2, 47.2), 6)

        measurement = {
            "sensor_name": random.choice(sensor_names),
            "type_name": "location",
            "timestamp": (now - timedelta(minutes=i)).isoformat(),
            "value": {
                "type": "Point",
                "coordinates": [lon, lat]
            }
        }
        payload["measurements"].append(measurement)

    print_info(f"Inserting {len(payload['measurements'])} geoposition measurements...")

    start_time = time.time()
    try:
        response = requests.post(f"{API_BASE_URL}/measurements/batch", json=payload, timeout=30)
        elapsed = time.time() - start_time

        if response.status_code == 200:
            data = response.json()
            print_success(f"Batch insert successful in {elapsed:.3f}s")
            print_info(f"Throughput: {len(payload['measurements']) / elapsed:.1f} measurements/sec")
            return True
        else:
            print_error(f"Failed with status {response.status_code}: {response.text}")
            return False
    except Exception as e:
        print_error(f"Request failed: {e}")
        return False


def test_batch_large():
    """Test batch insert with large number of measurements"""
    print_header("TEST 5: Large Batch Performance (500 measurements)")

    payload = {
        "provenance": {
            "lineage": "test",
            "data_collector": "batch_test_script",
            "data_collector_version": "1.0.0"
        },
        "measurements": []
    }

    sensor_names = [f"PERF_Test_{str(i).zfill(3)}" for i in range(1, 51)]
    type_names = ["power_generation", "conductivity", "air_temperature", "pm25"]

    now = datetime.now(timezone.utc)

    for i in range(500):
        measurement = {
            "sensor_name": random.choice(sensor_names),
            "type_name": random.choice(type_names),
            "timestamp": (now - timedelta(seconds=i)).isoformat(),
            "value": round(random.uniform(5.0, 100.0), 2)
        }
        payload["measurements"].append(measurement)

    print_info(f"Inserting {len(payload['measurements'])} measurements...")

    start_time = time.time()
    try:
        response = requests.post(f"{API_BASE_URL}/measurements/batch", json=payload, timeout=60)
        elapsed = time.time() - start_time

        if response.status_code == 200:
            data = response.json()
            print_success(f"Batch insert successful in {elapsed:.3f}s")
            print_info(f"Throughput: {len(payload['measurements']) / elapsed:.1f} measurements/sec")

            # Performance benchmarks
            throughput = len(payload['measurements']) / elapsed
            if throughput > 100:
                print_success(f"Excellent performance: {throughput:.1f} measurements/sec")
            elif throughput > 50:
                print_info(f"Good performance: {throughput:.1f} measurements/sec")
            else:
                print_error(f"Poor performance: {throughput:.1f} measurements/sec")

            return True
        else:
            print_error(f"Failed with status {response.status_code}: {response.text}")
            return False
    except Exception as e:
        print_error(f"Request failed: {e}")
        return False


def test_batch_mixed_types():
    """Test batch insert with mixed data types"""
    print_header("TEST 6: Mixed Data Types Batch Insert")

    payload = {
        "provenance": {
            "lineage": "test",
            "data_collector": "batch_test_script",
            "data_collector_version": "1.0.0"
        },
        "measurements": []
    }

    now = datetime.now(timezone.utc)

    # Numeric
    for i in range(10):
        payload["measurements"].append({
            "sensor_name": f"MIXED_Sensor_{str(i % 5).zfill(3)}",
            "type_name": "power_generation",
            "timestamp": (now - timedelta(minutes=i)).isoformat(),
            "value": round(random.uniform(100.0, 500.0), 2)
        })

    # String
    for i in range(5):
        payload["measurements"].append({
            "sensor_name": f"MIXED_Sensor_{str(i % 5).zfill(3)}",
            "type_name": "device_status",
            "timestamp": (now - timedelta(minutes=i)).isoformat(),
            "value": random.choice(["ACTIVE", "LOW_BATTERY"])
        })

    # JSON
    for i in range(5):
        payload["measurements"].append({
            "sensor_name": f"MIXED_Sensor_{str(i % 5).zfill(3)}",
            "type_name": "sensor_config",
            "timestamp": (now - timedelta(minutes=i)).isoformat(),
            "value": {
                "sampling_interval": random.randint(10, 60),
                "mode": "normal"
            }
        })

    print_info(f"Inserting {len(payload['measurements'])} mixed-type measurements...")

    start_time = time.time()
    try:
        response = requests.post(f"{API_BASE_URL}/measurements/batch", json=payload, timeout=30)
        elapsed = time.time() - start_time

        if response.status_code == 200:
            data = response.json()
            print_success(f"Batch insert successful in {elapsed:.3f}s")
            print_info(f"Throughput: {len(payload['measurements']) / elapsed:.1f} measurements/sec")
            return True
        else:
            print_error(f"Failed with status {response.status_code}: {response.text}")
            return False
    except Exception as e:
        print_error(f"Request failed: {e}")
        return False


def test_endpoint_availability():
    """Test if the batch endpoint is available"""
    print_header("TEST 0: Endpoint Availability Check")

    try:
        response = requests.get(f"{API_BASE_URL}/health", timeout=5)
        if response.status_code == 200:
            print_success("API server is reachable")
            return True
        else:
            print_error(f"API returned unexpected status: {response.status_code}")
            return False
    except Exception as e:
        print_error(f"Cannot reach API server: {e}")
        print_info("Make sure the server is running: go run cmd/server/main.go")
        return False


def main():
    print_header("BATCH ENDPOINT COMPREHENSIVE TEST SUITE")
    print_info("Testing /api/v1/measurements/batch endpoint")
    print_info(f"Target: {API_BASE_URL}")
    print("")

    # Check availability first
    if not test_endpoint_availability():
        print_error("\n❌ Cannot proceed - API server not available")
        sys.exit(1)

    results = {}

    # Run all tests
    results['numeric'] = test_batch_numeric()
    time.sleep(1)

    results['string'] = test_batch_string()
    time.sleep(1)

    results['json'] = test_batch_json()
    time.sleep(1)

    results['geoposition'] = test_batch_geoposition()
    time.sleep(1)

    results['large_batch'] = test_batch_large()
    time.sleep(1)

    results['mixed_types'] = test_batch_mixed_types()

    # Summary
    print_header("TEST RESULTS SUMMARY")

    passed = sum(1 for v in results.values() if v)
    total = len(results)

    for test_name, result in results.items():
        if result:
            print_success(f"{test_name}: PASSED")
        else:
            print_error(f"{test_name}: FAILED")

    print(f"\n{Colors.BOLD}Overall: {passed}/{total} tests passed{Colors.ENDC}\n")

    if passed == total:
        print_success("🎉 All tests passed! Batch endpoint is working correctly!")
        print_info("\nYou can now test streaming with the inserted data:")
        print_info("  python3 test_streaming.py")
        print_info("  python3 test_streaming_discovery.py")
        return 0
    else:
        print_error(f"❌ {total - passed} test(s) failed")
        return 1


if __name__ == "__main__":
    try:
        exit_code = main()
        sys.exit(exit_code)
    except KeyboardInterrupt:
        print_error("\n\nTest interrupted by user")
        sys.exit(1)
    except Exception as e:
        print_error(f"\n\nTest failed with error: {e}")
        import traceback
        traceback.print_exc()
        sys.exit(1)
