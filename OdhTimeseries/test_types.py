#!/usr/bin/env python3
"""Test that values have correct native types"""
import asyncio
import json
import websockets

async def test():
    uri = "ws://localhost:8080/api/v1/measurements/subscribe"

    async with websockets.connect(uri) as ws:
        # Send connection_init for a sensor with multiple data types
        await ws.send(json.dumps({
            "type": "connection_init",
            "payload": {
                "sensor_names": ["RAIN_Downtown_089"]
            }
        }))

        # Receive connection_ack
        msg = json.loads(await ws.recv())
        print(f"✓ Connected: {msg}")

        # Receive multiple updates to check types
        for i in range(6):
            msg = json.loads(await asyncio.wait_for(ws.recv(), timeout=5.0))
            if msg['type'] == 'data':
                payload = msg['payload']
                data_type = payload['data_type']
                value = payload['value']
                type_name = payload['type_name']

                print(f"\n[{i+1}] {type_name} ({data_type}):")
                print(f"  Value: {value}")
                print(f"  Python type: {type(value).__name__}")

                # Validate types
                if data_type == 'numeric':
                    assert isinstance(value, (int, float)), f"Expected number, got {type(value)}"
                    print("  ✓ Correct type: number")
                elif data_type == 'json':
                    assert isinstance(value, (dict, list)), f"Expected dict/list (parsed JSON), got {type(value)}"
                    print("  ✓ Correct type: parsed JSON object")
                elif data_type == 'geoshape' or data_type == 'geoposition':
                    assert isinstance(value, str), f"Expected string (WKT), got {type(value)}"
                    assert value.startswith('0103') or value.startswith('0101'), "Expected WKT hex format"
                    print("  ✓ Correct type: WKT string")

        print("\n✓ All types validated successfully!")

asyncio.run(test())
