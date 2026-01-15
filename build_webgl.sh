#!/bin/bash
# Build script for NAVΛ Dashboard WebGL

set -e

echo "Building NAVΛ Dashboard for WebGL..."

# Check if Unity is installed
UNITY_PATH=""
if [ -d "/Applications/Unity/Hub/Editor" ]; then
    # Find latest Unity version
    UNITY_PATH=$(ls -td /Applications/Unity/Hub/Editor/*/Unity.app/Contents/MacOS/Unity 2>/dev/null | head -1)
elif [ -d "/Applications/Unity" ]; then
    UNITY_PATH="/Applications/Unity/Unity.app/Contents/MacOS/Unity"
fi

if [ -z "$UNITY_PATH" ] || [ ! -f "$UNITY_PATH" ]; then
    echo "ERROR: Unity Editor not found!"
    echo "Please install Unity Editor or specify UNITY_PATH environment variable"
    exit 1
fi

echo "Using Unity: $UNITY_PATH"

# Build directory
BUILD_DIR="$(pwd)/build/webgl"
mkdir -p "$BUILD_DIR"

# Build WebGL
echo "Starting WebGL build..."
"$UNITY_PATH" \
    -batchmode \
    -quit \
    -projectPath "$(pwd)/nava-ai" \
    -buildTarget WebGL \
    -buildPath "$BUILD_DIR" \
    -logFile "$BUILD_DIR/build.log"

if [ $? -eq 0 ]; then
    echo "✓ WebGL build successful!"
    echo "Build output: $BUILD_DIR"
    echo ""
    echo "To run locally:"
    echo "  cd $BUILD_DIR"
    echo "  python3 -m http.server 8000"
    echo "  Then open: http://localhost:8000"
else
    echo "✗ Build failed. Check $BUILD_DIR/build.log"
    exit 1
fi
