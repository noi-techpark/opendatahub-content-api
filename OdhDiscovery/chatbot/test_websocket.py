#!/usr/bin/env python3
"""
Simple WebSocket test client for ODH Chatbot
"""
import asyncio
import json
import websockets

async def test_chatbot():
    """Test chatbot with a simple query"""
    uri = "ws://localhost:8001/ws"

    print(f"Connecting to {uri}...")

    try:
        async with websockets.connect(uri) as websocket:
            print("Connected!")

            # Send a test query
            query = {
                "type": "query",
                "content": "i want to use the opendatahub to build an ap for wine"
            }

            print(f"\nSending query: {query['content']}")
            await websocket.send(json.dumps(query))

            # Receive responses
            print("\nReceiving responses:")
            print("-" * 60)

            while True:
                try:
                    response = await websocket.recv()
                    data = json.loads(response)

                    response_type = data.get("type")

                    if response_type == "status":
                        print(f"[STATUS] {data.get('content')}")
                        if data.get('content') == "Done":
                            print("-" * 60)
                            break

                    elif response_type == "message":
                        print(f"\n[AGENT RESPONSE]")
                        print(data.get('content'))

                    elif response_type == "navigation":
                        print(f"\n[NAVIGATION]")
                        print(json.dumps(data.get('data'), indent=2))

                    elif response_type == "error":
                        print(f"\n[ERROR] {data.get('content')}")
                        break

                except websockets.exceptions.ConnectionClosed:
                    print("\nConnection closed")
                    break
                except Exception as e:
                    print(f"\nError: {e}")
                    break

    except Exception as e:
        print(f"Connection failed: {e}")
        print("\nMake sure the backend is running:")
        print("  cd /home/mroggia/git/opendatahub-content-api/OdhDiscovery/chatbot")
        print("  docker-compose up -d")

if __name__ == "__main__":
    asyncio.run(test_chatbot())
