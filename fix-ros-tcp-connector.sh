#!/bin/bash

# Fix ROS TCP Connector DLL Loading Error
# This script fixes the Unity.Robotics.ROSTCPConnector plugin loading issue

echo "=========================================="
echo "Fix ROS TCP Connector DLL Error"
echo "=========================================="
echo ""

PROJECT_DIR="/Users/frankvanlaarhoven/Desktop/anything-mcp/NAVA-AI/nava-ai"

cd "$PROJECT_DIR" || exit 1

# Step 1: Check if Unity is running
echo "Step 1: Checking Unity Editor status..."
if ps aux | grep -i "Unity.*projectPath.*nava-ai" | grep -v grep > /dev/null; then
    UNITY_PID=$(ps aux | grep -i "Unity.*projectPath.*nava-ai" | grep -v grep | awk '{print $2}')
    echo "⚠️  Unity Editor is running (PID: $UNITY_PID)"
    echo ""
    echo "Please close Unity Editor first, then run this script again."
    echo ""
    echo "Or, if you want to continue with Unity open:"
    echo "  1. In Unity: Window > Package Manager"
    echo "  2. Find 'ROS TCP Connector'"
    echo "  3. Click 'Reimport' or 'Remove' then 'Add'"
    exit 1
fi

echo "✅ Unity Editor is not running"
echo ""

# Step 2: Verify GodModeRigor.cs exists
echo "Step 2: Verifying GodModeRigor.cs..."
if [ -f "Assets/Scripts/GodModeRigor.cs" ]; then
    echo "✅ GodModeRigor.cs found"
    FILE_SIZE=$(wc -l < "Assets/Scripts/GodModeRigor.cs")
    echo "   Lines: $FILE_SIZE"
else
    echo "⚠️  GodModeRigor.cs not found!"
    echo "   This file should exist at: Assets/Scripts/GodModeRigor.cs"
fi
echo ""

# Step 3: Check ROS TCP Connector in manifest
echo "Step 3: Checking ROS TCP Connector package..."
if grep -q "ros-tcp-connector" "Packages/manifest.json"; then
    echo "✅ ROS TCP Connector found in manifest.json"
    grep "ros-tcp-connector" "Packages/manifest.json"
else
    echo "⚠️  ROS TCP Connector NOT found in manifest.json!"
    echo "   This needs to be added manually in Unity Package Manager"
fi
echo ""

# Step 4: Backup and clear Library
echo "Step 4: Clearing Library folder (fixes DLL loading)..."
if [ -d "Library" ]; then
    BACKUP_NAME="Library_backup_$(date +%Y%m%d_%H%M%S)"
    echo "   Backing up to: $BACKUP_NAME"
    mv Library "$BACKUP_NAME" 2>/dev/null || {
        echo "   ⚠️  Could not backup (may be in use)"
        echo "   Attempting to remove without backup..."
        rm -rf Library
    }
    echo "✅ Library folder cleared"
else
    echo "⚠️  Library folder not found (already cleared?)"
fi
echo ""

# Step 5: Clear Package Cache for ROS TCP Connector
echo "Step 5: Clearing ROS TCP Connector cache..."
PACKAGE_CACHE="$HOME/Library/Unity/cache/packages/packages.unity.com"
if [ -d "$PACKAGE_CACHE" ]; then
    find "$PACKAGE_CACHE" -name "*ros*tcp*" -type d 2>/dev/null | while read -r dir; do
        echo "   Removing: $dir"
        rm -rf "$dir" 2>/dev/null
    done
    echo "✅ Package cache cleared"
else
    echo "⚠️  Package cache not found at: $PACKAGE_CACHE"
fi
echo ""

# Step 6: Clear Temp folders
echo "Step 6: Clearing temporary folders..."
rm -rf Temp 2>/dev/null && echo "✅ Temp cleared" || echo "⚠️  Temp not found"
rm -rf obj 2>/dev/null && echo "✅ obj cleared" || echo "⚠️  obj not found"
echo ""

echo "=========================================="
echo "✅ Cleanup Complete!"
echo "=========================================="
echo ""
echo "Next Steps in Unity Editor:"
echo ""
echo "1. Open Unity Editor"
echo "2. Open project: $PROJECT_DIR"
echo "3. Wait for Unity to reimport (5-10 minutes)"
echo ""
echo "4. In Unity Package Manager:"
echo "   - Window > Package Manager"
echo "   - Wait for refresh to complete"
echo "   - Find 'ROS TCP Connector' in the list"
echo "   - If it shows a blue checkmark: Click 'Reimport'"
echo "   - If it's missing: Click '+' > 'Add package from git URL'"
echo "   - URL: https://github.com/Unity-Technologies/ROS-TCP-Connector.git?path=/com.unity.robotics.ros-tcp-connector"
echo ""
echo "5. Verify GodModeRigor.cs:"
echo "   - Check Assets/Scripts/GodModeRigor.cs exists"
echo "   - Should have ~223 lines"
echo "   - If missing, it may need to be restored from backup"
echo ""
echo "6. Check Console for errors:"
echo "   - Should see no DLL loading errors"
echo "   - ROS TCP Connector should load successfully"
echo ""
echo "=========================================="
echo ""
echo "If antivirus is blocking:"
echo "- Check antivirus quarantine logs"
echo "- Add Unity folder to exclusions"
echo "- Disable 'Game Mode' if it blocks network apps"
echo ""
