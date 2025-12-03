#!/bin/bash
set -e

echo "=================================================="
echo "ODH Chatbot Setup Script"
echo "=================================================="
echo ""

# Check if .env exists
if [ ! -f ".env" ]; then
    echo "⚠️  No .env file found"
    echo "📝 Creating .env from .env.example..."
    cp .env.example .env
    echo ""
    echo "✅ Created .env file"
    echo "❗ IMPORTANT: Edit .env and add your API key!"
    echo ""
    echo "Please set TOGETHER_API_KEY (or your provider's key) in .env"
    echo ""
    read -p "Press Enter when you've updated .env, or Ctrl+C to exit..."
fi

echo ""
echo "=================================================="
echo "Starting Services"
echo "=================================================="
echo ""

# Start Docker Compose
echo "🚀 Starting ChromaDB and Backend with Docker Compose..."
docker-compose up -d

echo ""
echo "⏳ Waiting for services to be ready..."
sleep 5

# Wait for Proxy
echo "⏳ Waiting for Proxy..."
until curl -s http://localhost:5000/health > /dev/null 2>&1; do
    echo "   Proxy not ready yet..."
    sleep 2
done
echo "✅ Proxy is ready"

# Wait for ChromaDB
echo "⏳ Waiting for ChromaDB..."
until curl -s http://localhost:8000/api/v1/heartbeat > /dev/null 2>&1; do
    echo "   ChromaDB not ready yet..."
    sleep 2
done
echo "✅ ChromaDB is ready"

# Wait for Backend
echo "⏳ Waiting for Backend..."
until curl -s http://localhost:8001/health > /dev/null 2>&1; do
    echo "   Backend not ready yet..."
    sleep 2
done
echo "✅ Backend is ready"

echo ""
echo "=================================================="
echo "Ingesting Documentation"
echo "=================================================="
echo ""

# Ingest docs
if [ -d "docs" ] && [ "$(ls -A docs/*.md 2>/dev/null)" ]; then
    echo "📚 Ingesting documentation from docs/ directory..."
    docker-compose exec -T backend python vector_store/ingest_docs.py /docs --clear
    echo "✅ Documentation ingested"
else
    echo "⚠️  No markdown files found in docs/ directory"
    echo "   Skipping documentation ingestion"
fi

echo ""
echo "=================================================="
echo "Setup Complete!"
echo "=================================================="
echo ""
echo "Services running:"
echo "  - Proxy:     http://localhost:5000 (routes API calls)"
echo "  - ChromaDB:  http://localhost:8000"
echo "  - Backend:   http://localhost:8001"
echo "  - Health:    http://localhost:8001/health"
echo "  - WebSocket: ws://localhost:8001/ws"
echo ""
echo "View logs:"
echo "  docker-compose logs -f proxy"
echo "  docker-compose logs -f backend"
echo "  docker-compose logs -f chromadb"
echo ""
echo "Stop services:"
echo "  docker-compose down"
echo ""
echo "Happy chatting! 🤖"
