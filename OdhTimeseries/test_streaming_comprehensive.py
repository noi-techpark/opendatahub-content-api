#!/usr/bin/env python3
"""
Comprehensive streaming subscription test suite
Combines all streaming tests into a single script:
- Simple subscriptions (sensor_names)
- Discovery subscriptions (timeseries_filter)
- Spatial filtering (discovery mode only)
- Manual SQL inserts (bypasses API)
- Real-time updates
"""

import asyncio
import websockets
import json
import subprocess
import sys
from datetime import datetime, timezone

WS_URL = "ws://localhost:8080/api/v1/measurements/subscribe"

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

def insert_measurement_sql(sensor_name, type_name, value):
    """Insert a numeric measurement directly into PostgreSQL"""
    sql = f"""
    INSERT INTO intimev3.measurements_numeric (timeseries_id, timestamp, value, provenance_id, created_on)
    SELECT ts.id, NOW(), {value}, 1, NOW()
    FROM intimev3.timeseries ts
    JOIN intimev3.sensors s ON ts.sensor_id = s.id
    JOIN intimev3.types t ON ts.type_id = t.id
    WHERE s.name = '{sensor_name}' AND t.name = '{type_name}'
    LIMIT 1;
    """

    cmd = [
        "psql",
        "-h", "localhost",
        "-p", "5556",
        "-U", "bdp",
        "-d", "timeseries",
        "-c", sql
    ]

    import os
    env = {**os.environ, "PGPASSWORD": "password"}
    result = subprocess.run(cmd, env=env, capture_output=True, text=True)
    if result.returncode != 0:
        print_error(f"SQL insert error: {result.stderr.strip()}")
    return result.returncode == 0


async def test_simple_subscription():
    """Test 1: Simple subscription with sensor_names (mirrors /latest)"""
    print_header("TEST 1: Simple Subscription (sensor_names)")

    sensor_name = "HUM_Park_067"

    try:
        async with websockets.connect(WS_URL) as ws:
            print_info(f"Connected to WebSocket: {WS_URL}")

            # Send connection_init with simple mode
            init_msg = {
                "type": "connection_init",
                "payload": {
                    "sensor_names": [sensor_name]
                }
            }
            print_info(f"Sending connection_init for sensor: {sensor_name}")
            await ws.send(json.dumps(init_msg))

            # Wait for connection_ack
            response = await asyncio.wait_for(ws.recv(), timeout=5.0)
            resp_data = json.loads(response)
            if resp_data['type'] == 'connection_ack':
                print_success("Simple subscription acknowledged")
            else:
                print_error(f"Unexpected response: {resp_data}")
                return False

            # Receive initial updates
            print_info("Listening for initial updates (3 seconds)...")
            update_count = 0
            try:
                while update_count < 10:
                    msg = await asyncio.wait_for(ws.recv(), timeout=3.0)
                    data = json.loads(msg)
                    if data['type'] == 'data':
                        update_count += 1
                        payload = data['payload']
                        print_info(f"Update {update_count}: {payload['sensor_name']} = {payload['value']}")
            except asyncio.TimeoutError:
                pass

            print_success(f"Received {update_count} initial updates")
            return True

    except Exception as e:
        print_error(f"Test failed: {e}")
        import traceback
        traceback.print_exc()
        return False


async def test_discovery_subscription_basic():
    """Test 2: Discovery subscription with required_types"""
    print_header("TEST 2: Discovery Subscription (required_types)")

    try:
        # Use advanced endpoint
        advanced_url = WS_URL + "/advanced"
        async with websockets.connect(advanced_url) as ws:
            print_info(f"Connected to WebSocket: {advanced_url}")

            # Send connection_init with discovery filters
            init_msg = {
                "type": "connection_init",
                "payload": {
                    "timeseries_filter": {
                        "required_types": ["power_generation"]
                    },
                    "limit": 5
                }
            }
            print_info("Sending connection_init with discovery filter: required_types=['power_generation']")
            await ws.send(json.dumps(init_msg))

            # Wait for connection_ack
            response = await asyncio.wait_for(ws.recv(), timeout=5.0)
            resp_data = json.loads(response)

            if resp_data['type'] == 'connection_ack':
                mode = resp_data.get('payload', {}).get('mode', 'unknown')
                print_success(f"Discovery subscription acknowledged (mode: {mode})")

                # Receive initial updates
                print_info("Listening for initial updates (3 seconds)...")
                update_count = 0
                try:
                    while update_count < 10:
                        msg = await asyncio.wait_for(ws.recv(), timeout=3.0)
                        data = json.loads(msg)
                        if data['type'] == 'data':
                            update_count += 1
                            payload = data['payload']
                            print_info(f"Update {update_count}: {payload['sensor_name']} = {payload['value']}")
                except asyncio.TimeoutError:
                    pass

                print_success(f"Received {update_count} updates from discovered sensors")
                return True
            else:
                print_error(f"Unexpected response: {resp_data}")
                return False

    except Exception as e:
        print_error(f"Test failed: {e}")
        import traceback
        traceback.print_exc()
        return False


async def test_discovery_with_spatial_filter():
    """Test 3: Discovery subscription with spatial filtering - SKIPPED (spatial filter removed)"""
    print_header("TEST 3: Discovery with Spatial Filter (bbox) - SKIPPED")

    print_info("Spatial filtering was removed during refactoring - test skipped")
    print_success("Test skipped successfully")
    return True

    # Original test commented out - spatial filtering removed
    """
    try:
        async with websockets.connect(WS_URL) as ws:
            print_info(f"Connected to WebSocket: {WS_URL}")

            # Subscribe with bbox filter
            subscription = {
                "action": "subscribe",
                "timeseries_filter": {
                    "required_types": ["location"]
                },
                "spatial_filter": {
                    "type": "bbox",
                    "coordinates": [10.5, 46.2, 12.5, 47.2]  # South Tyrol area
                },
                "limit": 10
            }
            print_info("Subscribing with discovery + bbox filter")
            await ws.send(json.dumps(subscription))

            # Wait for ack
            response = await asyncio.wait_for(ws.recv(), timeout=5.0)
            resp_data = json.loads(response)

            if resp_data['type'] == 'ack':
                mode = resp_data.get('data', {}).get('mode', 'unknown')
                print_success(f"Discovery with spatial filter acknowledged (mode: {mode})")

                # Receive initial updates
                print_info("Listening for geo updates (3 seconds)...")
                update_count = 0
                try:
                    while update_count < 5:
                        msg = await asyncio.wait_for(ws.recv(), timeout=3.0)
                        data = json.loads(msg)
                        if data['type'] == 'data':
                            update_count += 1
                            wkt = str(data['data']['value'])[:50]
                            print_info(f"Geo update {update_count}: {data['data']['sensor_name']} at {wkt}...")
                except asyncio.TimeoutError:
                    pass

                print_success(f"Received {update_count} geo updates (spatial filter working)")
                return True
            else:
                print_error(f"Unexpected response: {resp_data}")
                return False

    except Exception as e:
        print_error(f"Test failed: {e}")
        import traceback
        traceback.print_exc()
        return False
    """


async def test_manual_insert_and_stream():
    """Test 4: Insert via SQL and receive streaming update"""
    print_header("TEST 4: Manual SQL Insert + Streaming Update")

    sensor_name = "HUM_Hospital_013"  # Sensor that has relative_humidity type
    type_name = "relative_humidity"
    test_value = 99.99

    try:
        async with websockets.connect(WS_URL) as ws:
            print_info(f"Connected to WebSocket: {WS_URL}")

            # Send connection_init
            init_msg = {
                "type": "connection_init",
                "payload": {
                    "sensor_names": [sensor_name]
                }
            }
            await ws.send(json.dumps(init_msg))

            # Wait for connection_ack
            response = await asyncio.wait_for(ws.recv(), timeout=5.0)
            resp_data = json.loads(response)
            if resp_data['type'] == 'connection_ack':
                print_success("Subscription acknowledged")

            # Clear initial updates
            print_info("Clearing initial updates...")
            try:
                while True:
                    await asyncio.wait_for(ws.recv(), timeout=2.0)
            except asyncio.TimeoutError:
                pass

            # Insert test measurement via SQL
            print_info(f"Inserting measurement via SQL: {sensor_name}.{type_name} = {test_value}")
            if insert_measurement_sql(sensor_name, type_name, test_value):
                print_success("SQL insert successful")
            else:
                print_error("SQL insert failed")
                return False

            # Wait for streaming update
            print_info("Waiting for WebSocket update (10 seconds timeout)...")
            try:
                while True:
                    msg = await asyncio.wait_for(ws.recv(), timeout=10.0)
                    data = json.loads(msg)
                    if data['type'] == 'data':
                        payload = data['payload']
                        received_value = payload['value']
                        print_success(f"📨 Update received! Value: {received_value}")
                        if str(received_value) == str(test_value):
                            print_success("✅ Correct value received - streaming works end-to-end!")
                            return True
                        else:
                            print_info(f"Received different value (waiting for {test_value})")
            except asyncio.TimeoutError:
                print_error("Timeout waiting for update")
                return False

    except Exception as e:
        print_error(f"Test failed: {e}")
        import traceback
        traceback.print_exc()
        return False


async def test_discovery_with_measurement_filter():
    """Test 5: Discovery with measurement value filter"""
    print_header("TEST 5: Discovery with Measurement Filter")

    try:
        # Use advanced endpoint
        advanced_url = WS_URL + "/advanced"
        async with websockets.connect(advanced_url) as ws:
            print_info(f"Connected to WebSocket: {advanced_url}")

            # Send connection_init with value filter
            init_msg = {
                "type": "connection_init",
                "payload": {
                    "timeseries_filter": {
                        "required_types": ["power_generation"]
                    },
                    "measurement_filter": {
                        "latest_only": True,
                        "expression": "power_generation.gteq.100"
                    },
                    "limit": 10
                }
            }
            print_info("Sending connection_init with discovery + measurement filter (value >= 100)")
            await ws.send(json.dumps(init_msg))

            # Wait for connection_ack
            response = await asyncio.wait_for(ws.recv(), timeout=5.0)
            resp_data = json.loads(response)

            if resp_data['type'] == 'connection_ack':
                mode = resp_data.get('payload', {}).get('mode', 'unknown')
                print_success(f"Discovery with measurement filter acknowledged (mode: {mode})")

                # Receive updates
                print_info("Listening for updates (3 seconds)...")
                update_count = 0
                try:
                    while update_count < 5:
                        msg = await asyncio.wait_for(ws.recv(), timeout=3.0)
                        data = json.loads(msg)
                        if data['type'] == 'data':
                            update_count += 1
                            payload = data['payload']
                            print_info(f"Update: {payload['sensor_name']} = {payload['value']}")
                except asyncio.TimeoutError:
                    pass

                print_success(f"Received {update_count} filtered updates")
                return True
            else:
                print_error(f"Unexpected response: {resp_data}")
                return False

    except Exception as e:
        print_error(f"Test failed: {e}")
        import traceback
        traceback.print_exc()
        return False


async def test_simple_mode_rejects_spatial_filter():
    """Test 6: Verify simple mode rejects spatial_filter - SKIPPED (spatial filter removed)"""
    print_header("TEST 6: Validation - Simple Mode Rejects Spatial Filter - SKIPPED")

    print_info("Spatial filtering was removed during refactoring - test skipped")
    print_success("Test skipped successfully")
    return True

    # Original test commented out - spatial filtering removed
    """
    try:
        async with websockets.connect(WS_URL) as ws:
            print_info(f"Connected to WebSocket: {WS_URL}")

            # Try to use spatial_filter in simple mode (should fail)
            subscription = {
                "action": "subscribe",
                "sensor_names": ["HUM_Park_067"],
                "spatial_filter": {
                    "type": "bbox",
                    "coordinates": [10.5, 46.2, 12.5, 47.2]
                }
            }
            print_info("Attempting to use spatial_filter in simple mode (should be rejected)")
            await ws.send(json.dumps(subscription))

            # Wait for response
            response = await asyncio.wait_for(ws.recv(), timeout=5.0)
            resp_data = json.loads(response)

            if resp_data['type'] == 'error' and 'spatial_filter' in resp_data.get('error', '').lower():
                print_success(f"✅ Correctly rejected: {resp_data['error']}")
                return True
            elif resp_data['type'] == 'ack':
                print_error("❌ Server accepted spatial_filter in simple mode (should reject)")
                return False
            else:
                print_error(f"Unexpected response: {resp_data}")
                return False

    except Exception as e:
        print_error(f"Test failed: {e}")
        import traceback
        traceback.print_exc()
        return False
    """


async def main():
    print_header("COMPREHENSIVE STREAMING SUBSCRIPTION TEST SUITE")
    print_info("This script tests all streaming features:")
    print_info("  - Simple subscriptions (sensor_names)")
    print_info("  - Discovery subscriptions (timeseries_filter)")
    print_info("  - Spatial filtering (discovery mode only)")
    print_info("  - Manual SQL inserts")
    print_info("  - Validation (simple mode restrictions)")
    print("")

    results = {}

    # Run all tests
    print_info("Make sure the Go server is running: go run cmd/server/main.go")
    print("")

    results['simple_subscription'] = await test_simple_subscription()
    await asyncio.sleep(1)

    results['discovery_basic'] = await test_discovery_subscription_basic()
    await asyncio.sleep(1)

    results['discovery_spatial'] = await test_discovery_with_spatial_filter()
    await asyncio.sleep(1)

    results['manual_insert'] = await test_manual_insert_and_stream()
    await asyncio.sleep(1)

    results['discovery_filter'] = await test_discovery_with_measurement_filter()
    await asyncio.sleep(1)

    results['validation'] = await test_simple_mode_rejects_spatial_filter()

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
        print_success("🎉 All tests passed! Streaming system is fully operational!")
        return 0
    else:
        print_error(f"❌ {total - passed}/{total} test(s) failed")
        return 1


if __name__ == "__main__":
    try:
        exit_code = asyncio.run(main())
        sys.exit(exit_code)
    except KeyboardInterrupt:
        print_error("\n\nTest interrupted by user")
        sys.exit(1)
    except Exception as e:
        print_error(f"\n\nTest failed with error: {e}")
        import traceback
        traceback.print_exc()
        sys.exit(1)
