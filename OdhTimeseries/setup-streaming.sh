#!/bin/bash

# Streaming setup script for Materialize integration
# This script sets up the PostgreSQL publication and Materialize source/views

set -e

echo "===================================="
echo "Streaming Setup Script"
echo "===================================="
echo ""

# Configuration
DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-5556}"
DB_NAME="${DB_NAME:-timeseries}"
DB_USER="${DB_USER:-bdp}"
DB_PASSWORD="${DB_PASSWORD:-password}"
DB_SCHEMA="${DB_SCHEMA:-intimev3}"

MZ_HOST="${MZ_HOST:-localhost}"
MZ_PORT="${MZ_PORT:-6875}"
MZ_USER="${MZ_USER:-materialize}"

echo "Step 1: Setting up PostgreSQL publication"
echo "-------------------------------------------"
PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -f sql-scripts/pg-publication-setup.sql
if [ $? -eq 0 ]; then
    echo "✓ PostgreSQL publication created successfully"
else
    echo "✗ Failed to create PostgreSQL publication"
    exit 1
fi

echo ""
echo "Step 2: Waiting for Materialize to be ready"
echo "-------------------------------------------"
max_attempts=30
attempt=0
while [ $attempt -lt $max_attempts ]; do
    if PGPASSWORD="" psql -h $MZ_HOST -p $MZ_PORT -U $MZ_USER -d materialize -c "SELECT 1" > /dev/null 2>&1; then
        echo "✓ Materialize is ready"
        break
    fi
    attempt=$((attempt + 1))
    echo "Waiting for Materialize... ($attempt/$max_attempts)"
    sleep 2
done

if [ $attempt -eq $max_attempts ]; then
    echo "✗ Materialize did not become ready in time"
    exit 1
fi

echo ""
echo "Step 3: Setting up Materialize source and views"
echo "-------------------------------------------"
PGPASSWORD="" psql -h $MZ_HOST -p $MZ_PORT -U $MZ_USER -d materialize -f sql-scripts/materialize-setup.sql
if [ $? -eq 0 ]; then
    echo "✓ Materialize source and views created successfully"
else
    echo "✗ Failed to create Materialize source and views"
    exit 1
fi

echo ""
echo "===================================="
echo "Setup completed successfully!"
echo "===================================="
echo ""
echo "Next steps:"
echo "1. Start the Go service: go run cmd/server/main.go"
echo "2. Connect to WebSocket endpoint: ws://localhost:8080/api/v1/measurements/subscribe"
echo "3. Send subscription request:"
echo '   {"action":"subscribe","sensor_names":["your-sensor"]}'
echo ""
