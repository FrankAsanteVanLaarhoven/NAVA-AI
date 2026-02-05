#!/bin/bash

# Fix Unity VirtualArtifacts Error
# This error occurs when Unity's internal cache is corrupted

echo "=========================================="
echo "Fixing Unity VirtualArtifacts Error"
echo "=========================================="
echo ""

PROJECT_DIR="/Users/frankvanlaarhoven/Desktop/anything-mcp/NAVA-AI/nava-ai"
cd "$PROJECT_DIR" || exit 1

echo "Step 1: Checking Unity Editor status..."
if pgrep -f "Unity.*nava-ai" > /dev/null; then
    echo "⚠️  Unity Editor is running"
    echo "   Please CLOSE Unity Editor before running this script"
    echo ""
    read -p "Press Enter after closing Unity Editor, or Ctrl+C to cancel..."
else
    echo "✅ Unity Editor is closed"
fi

echo ""
echo "Step 2: Clearing Unity caches..."
echo ""

# Clear Library folder (but keep manifest)
if [ -d "Library" ]; then
    echo "   Removing Library folder..."
    rm -rf Library
    echo "   ✅ Library folder cleared"
else
    echo "   ℹ️  Library folder not found (already cleared)"
fi

# Clear Temp folder
if [ -d "Temp" ]; then
    echo "   Removing Temp folder..."
    rm -rf Temp
    echo "   ✅ Temp folder cleared"
else
    echo "   ℹ️  Temp folder not found"
fi

# Clear obj folder (if exists)
if [ -d "obj" ]; then
    echo "   Removing obj folder..."
    rm -rf obj
    echo "   ✅ obj folder cleared"
else
    echo "   ℹ️  obj folder not found"
fi

# Clear Unity global package cache (macOS)
UNITY_CACHE="$HOME/Library/Unity/cache"
if [ -d "$UNITY_CACHE" ]; then
    echo "   Clearing Unity global package cache..."
    rm -rf "$UNITY_CACHE"
    echo "   ✅ Unity global cache cleared"
else
    echo "   ℹ️  Unity global cache not found"
fi

# Clear Unity package cache in project
if [ -d "Library/PackageCache" ]; then
    echo "   Removing PackageCache..."
    rm -rf Library/PackageCache
    echo "   ✅ PackageCache cleared"
fi

echo ""
echo "Step 3: Verifying manifest.json..."
if [ -f "Packages/manifest.json" ]; then
    echo "   ✅ manifest.json exists"
    if grep -q "com.unity.ugui" "Packages/manifest.json"; then
        echo "   ✅ UGUI package configured"
    fi
    if grep -q "com.unity.robotics.ros-tcp-connector" "Packages/manifest.json"; then
        echo "   ✅ ROS TCP Connector configured"
    fi
else
    echo "   ❌ manifest.json NOT FOUND!"
    exit 1
fi

echo ""
echo "=========================================="
echo "✅ Cache Clearing Complete"
echo "=========================================="
echo ""
echo "Next Steps:"
echo "1. Open Unity Editor"
echo "2. Wait for packages to import (5-10 minutes)"
echo "3. If VirtualArtifacts error appears again:"
echo "   - Close Unity"
echo "   - Run this script again"
echo "   - Reopen Unity"
echo ""
echo "The VirtualArtifacts error should be resolved after"
echo "Unity rebuilds its internal cache."
echo ""
