#!/bin/bash
# Quick script to run WebGL build locally

BUILD_DIR="build/webgl"

if [ ! -d "$BUILD_DIR" ]; then
    echo "ERROR: WebGL build not found!"
    echo "Please build first using:"
    echo "  - Unity Editor: NAVA-AI Dashboard > Build > Quick WebGL Build"
    echo "  - Or: ./build_webgl.sh"
    exit 1
fi

echo "Starting local web server..."
echo "Opening NAVΛ Dashboard in browser..."
echo ""
echo "Server running at: http://localhost:8000"
echo "Press Ctrl+C to stop"
echo ""

cd "$BUILD_DIR"

# Try Python 3 first, then Python 2
if command -v python3 &> /dev/null; then
    python3 -m http.server 8000
elif command -v python &> /dev/null; then
    python -m SimpleHTTPServer 8000
else
    echo "ERROR: Python not found!"
    echo "Please install Python 3 to run the web server"
    exit 1
fi
