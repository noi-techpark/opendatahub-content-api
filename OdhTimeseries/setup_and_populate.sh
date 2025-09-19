#!/bin/bash

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
VENV_DIR="$SCRIPT_DIR/venv"

echo "=== Database Population Setup and Execution ==="
echo "Script directory: $SCRIPT_DIR"

if [ ! -f "$SCRIPT_DIR/.env" ]; then
    echo "Warning: .env file not found. Please create it with database connection details."
    echo "Required variables: DB_HOST, DB_PORT, DB_NAME, DB_USER, DB_PASSWORD, DB_SCHEMA"
    echo "Example .env content:"
    echo "DB_HOST=localhost"
    echo "DB_PORT=5432"
    echo "DB_NAME=timeseries"
    echo "DB_USER=timeseries_user"
    echo "DB_PASSWORD=your_password"
    echo "DB_SCHEMA=intimev3"
    echo ""
    read -p "Continue anyway? (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
fi

echo "1. Creating Python virtual environment..."
if [ -d "$VENV_DIR" ]; then
    echo "   Virtual environment already exists, removing old one..."
    rm -rf "$VENV_DIR"
fi

python3 -m venv "$VENV_DIR"
echo "   ✓ Virtual environment created"

echo "2. Activating virtual environment..."
source "$VENV_DIR/bin/activate"
echo "   ✓ Virtual environment activated"

echo "3. Installing dependencies..."
pip install --upgrade pip
pip install -r "$SCRIPT_DIR/requirements.txt"
echo "   ✓ Dependencies installed"

echo "4. Running database population script..."
echo "   This will create realistic test data for the timeseries database"
echo "   Default: 50 sensors, 20 types, 5 datasets, 10000 measurements"
echo ""

python "$SCRIPT_DIR/populate_db.py" "$@"

echo ""
echo "=== Population Complete ==="
echo "Virtual environment location: $VENV_DIR"
echo "To rerun the script manually:"
echo "  source $VENV_DIR/bin/activate"
echo "  python $SCRIPT_DIR/populate_db.py"
echo ""
echo "To customize the population (edit populate_db.py parameters):"
echo "  NUM_SENSORS, NUM_TYPES, NUM_DATASETS, MEASUREMENTS_PER_SENSOR"