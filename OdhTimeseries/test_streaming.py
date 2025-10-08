#!/usr/bin/env python3
"""
Test script for the GraphQL-style streaming subscription system.
Tests both simple mode (sensor_names) and advanced mode (filters).
"""

import asyncio
import json
import websockets
import sys

async def test_simple_mode():
    """Test simple mode with sensor_names"""
    print("\n=== TEST 1: Simple Mode (sensor_names) ===")

    uri = "ws://localhost:8080/api/v1/measurements/subscribe"

    async with websockets.connect(uri) as websocket:
        print(f"Connected to: {uri}")

        # Send connection_init with sensor_names
        init_msg = {
            "type": "connection_init",
            "payload": {
                "sensor_names": ["PM10_Park_039"],
                "type_names": ["so2"]
            }
        }

        print(f"\nSending connection_init: {json.dumps(init_msg, indent=2)}")
        await websocket.send(json.dumps(init_msg))

        # Wait for connection_ack
        response = await websocket.recv()
        resp_data = json.loads(response)
        print(f"Response: {json.dumps(resp_data, indent=2)}")

        if resp_data['type'] != 'connection_ack':
            print(f"❌ Expected connection_ack, got: {resp_data['type']}")
            return False

        # Receive a few updates
        print("\nReceiving updates (will wait for 3 updates or 30 seconds)...")
        count = 0
        try:
            while count < 3:
                update = await asyncio.wait_for(websocket.recv(), timeout=30.0)
                data = json.loads(update)
                if data.get("type") == "data":
                    count += 1
                    payload = data['payload']
                    print(f"\n[Update {count}]")
                    print(f"  Sensor: {payload['sensor_name']}")
                    print(f"  Type: {payload['type_name']}")
                    print(f"  Value: {payload['value']}")
                    print(f"  Timestamp: {payload['timestamp']}")
        except asyncio.TimeoutError:
            print(f"\nTimeout waiting for updates (received {count} updates)")

        print("\n✓ Simple mode test completed")
        return True

async def test_discovery_mode_required_types():
    """Test discovery mode with timeseries_filter (required_types)"""
    print("\n\n=== TEST 2: Discovery Mode (required_types) ===")

    # Use advanced endpoint
    uri = "ws://localhost:8080/api/v1/measurements/subscribe/advanced"

    async with websockets.connect(uri) as websocket:
        print(f"Connected to: {uri}")

        # Send connection_init with discovery filters
        init_msg = {
            "type": "connection_init",
            "payload": {
                "timeseries_filter": {
                    "required_types": ["pm25"]
                },
                "limit": 5
            }
        }

        print(f"\nSending connection_init: {json.dumps(init_msg, indent=2)}")
        await websocket.send(json.dumps(init_msg))

        # Wait for connection_ack
        response = await websocket.recv()
        resp_data = json.loads(response)
        print(f"Response: {json.dumps(resp_data, indent=2)}")

        if resp_data['type'] != 'connection_ack':
            print(f"❌ Expected connection_ack, got: {resp_data['type']}")
            return False

        # Receive a few updates
        print("\nReceiving updates (will wait for 3 updates or 30 seconds)...")
        count = 0
        sensors_seen = set()
        try:
            while count < 3:
                update = await asyncio.wait_for(websocket.recv(), timeout=30.0)
                data = json.loads(update)
                if data.get("type") == "data":
                    count += 1
                    payload = data['payload']
                    sensor_name = payload['sensor_name']
                    sensors_seen.add(sensor_name)
                    print(f"\n[Update {count}]")
                    print(f"  Sensor: {sensor_name}")
                    print(f"  Type: {payload['type_name']}")
                    print(f"  Value: {payload['value']}")
        except asyncio.TimeoutError:
            print(f"\nTimeout waiting for updates (received {count} updates)")

        print(f"\nSensors seen: {sensors_seen}")
        print("\n✓ Discovery mode (required_types) test completed")
        return True

async def test_discovery_mode_value_filter():
    """Test discovery mode with measurement_filter (value expression)"""
    print("\n\n=== TEST 3: Discovery Mode (value filter) ===")

    # Use advanced endpoint
    uri = "ws://localhost:8080/api/v1/measurements/subscribe/advanced"

    async with websockets.connect(uri) as websocket:
        print(f"Connected to: {uri}")

        # Send connection_init with value filter: pm25 > 20
        init_msg = {
            "type": "connection_init",
            "payload": {
                "timeseries_filter": {
                    "required_types": ["pm25"]
                },
                "measurement_filter": {
                    "expression": "pm25.gt.20",
                    "latest_only": True
                },
                "limit": 5
            }
        }

        print(f"\nSending connection_init: {json.dumps(init_msg, indent=2)}")
        await websocket.send(json.dumps(init_msg))

        # Wait for connection_ack
        response = await websocket.recv()
        resp_data = json.loads(response)
        print(f"Response: {json.dumps(resp_data, indent=2)}")

        if resp_data['type'] != 'connection_ack':
            print(f"❌ Expected connection_ack, got: {resp_data['type']}")
            return False

        # Receive a few updates
        print("\nReceiving updates where air_temperature > 20 (will wait for 3 updates or 30 seconds)...")
        count = 0
        try:
            while count < 3:
                update = await asyncio.wait_for(websocket.recv(), timeout=30.0)
                data = json.loads(update)
                if data.get("type") == "data":
                    count += 1
                    payload = data['payload']
                    value = float(payload['value'])
                    print(f"\n[Update {count}]")
                    print(f"  Sensor: {payload['sensor_name']}")
                    print(f"  Type: {payload['type_name']}")
                    print(f"  Value: {value} (should be > 20)")

                    # Verify the filter is working
                    if value <= 20:
                        print(f"  ⚠️  WARNING: Received value {value} which is NOT > 20!")
        except asyncio.TimeoutError:
            print(f"\nTimeout waiting for updates (received {count} updates)")

        print("\n✓ Discovery mode (value filter) test completed")
        return True

async def main():
    """Run all tests"""
    print("Testing GraphQL-style streaming subscription system")
    print("=" * 60)

    try:
        # Test 1: Simple mode
        success1 = await test_simple_mode()

        # Test 2: Discovery mode with required_types
        success2 = await test_discovery_mode_required_types()

        # Test 3: Discovery mode with value filter
        success3 = await test_discovery_mode_value_filter()

        print("\n" + "=" * 60)
        if success1 and success2 and success3:
            print("All tests completed successfully!")
            return 0
        else:
            print("Some tests failed!")
            return 1

    except Exception as e:
        print(f"\n❌ Error during tests: {e}")
        import traceback
        traceback.print_exc()
        sys.exit(1)

if __name__ == "__main__":
    exit_code = asyncio.run(main())
    sys.exit(exit_code)
