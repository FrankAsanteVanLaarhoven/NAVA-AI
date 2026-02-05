#!/bin/bash

# Comprehensive ROS TCP Connector DLL Fix
# This script addresses the standard DLL loading error

echo "=========================================="
echo "Fix ROS TCP Connector DLL Loading Error"
echo "=========================================="
echo ""

PROJECT_DIR="/Users/frankvanlaarhoven/Desktop/anything-mcp/NAVA-AI/nava-ai"
cd "$PROJECT_DIR" || exit 1

# Step 1: Check Unity status
echo "Step 1: Checking Unity Editor status..."
if ps aux | grep -i "Unity.*projectPath.*nava-ai" | grep -v grep > /dev/null; then
    UNITY_PID=$(ps aux | grep -i "Unity.*projectPath.*nava-ai" | grep -v grep | awk '{print $2}')
    echo "⚠️  Unity Editor is running (PID: $UNITY_PID)"
    echo ""
    echo "Please close Unity Editor to clear Library folder"
    echo "Or continue - you can reimport package in Unity Package Manager"
    echo ""
    read -p "Continue anyway? (y/n) " -n 1 -r
    echo ""
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
    UNITY_RUNNING=true
else
    echo "✅ Unity Editor is not running"
    UNITY_RUNNING=false
fi
echo ""

# Step 2: Verify GodModeRigor.cs
echo "Step 2: Verifying GodModeRigor.cs..."
if [ -f "Assets/Scripts/GodModeRigor.cs" ]; then
    LINE_COUNT=$(wc -l < "Assets/Scripts/GodModeRigor.cs")
    echo "✅ GodModeRigor.cs found ($LINE_COUNT lines)"
    
    if [ "$LINE_COUNT" -lt 200 ]; then
        echo "⚠️  Warning: File seems shorter than expected (should be ~223 lines)"
    fi
else
    echo "❌ ERROR: GodModeRigor.cs NOT FOUND!"
    echo "   This file should exist at: Assets/Scripts/GodModeRigor.cs"
    echo "   You may need to restore it from backup"
fi
echo ""

# Step 3: Check ROS TCP Connector in manifest
echo "Step 3: Checking ROS TCP Connector package..."
if grep -q "ros-tcp-connector" "Packages/manifest.json"; then
    echo "✅ ROS TCP Connector found in manifest.json"
    echo "   Package URL:"
    grep "ros-tcp-connector" "Packages/manifest.json" | sed 's/^/   /'
else
    echo "❌ ROS TCP Connector NOT in manifest.json!"
    echo "   You need to add it in Unity Package Manager"
fi
echo ""

# Step 4: Check for package cache
echo "Step 4: Checking package cache..."
PACKAGE_CACHE="$HOME/Library/Unity/cache/packages/packages.unity.com"
if [ -d "$PACKAGE_CACHE" ]; then
    ROS_CACHE=$(find "$PACKAGE_CACHE" -name "*ros*tcp*" -type d 2>/dev/null | head -1)
    if [ -n "$ROS_CACHE" ]; then
        echo "⚠️  Found ROS TCP Connector in cache: $ROS_CACHE"
        echo "   This may be corrupted"
    else
        echo "✅ No cached ROS TCP Connector found (will download fresh)"
    fi
else
    echo "⚠️  Package cache directory not found"
fi
echo ""

# Step 5: Clear Library (if Unity is closed)
if [ "$UNITY_RUNNING" = false ]; then
    echo "Step 5: Clearing Library folder..."
    if [ -d "Library" ]; then
        BACKUP_NAME="Library_backup_$(date +%Y%m%d_%H%M%S)"
        echo "   Backing up to: $BACKUP_NAME"
        mv Library "$BACKUP_NAME" 2>/dev/null || rm -rf Library
        echo "✅ Library folder cleared"
    else
        echo "⚠️  Library folder not found (already cleared?)"
    fi
    echo ""
    
    # Clear package cache
    echo "Step 6: Clearing ROS TCP Connector cache..."
    if [ -n "$ROS_CACHE" ] && [ -d "$ROS_CACHE" ]; then
        rm -rf "$ROS_CACHE" 2>/dev/null
        echo "✅ ROS TCP Connector cache cleared"
    fi
    echo ""
    
    # Clear Temp
    echo "Step 7: Clearing temporary folders..."
    rm -rf Temp 2>/dev/null && echo "✅ Temp cleared" || echo "⚠️  Temp not found"
    rm -rf obj 2>/dev/null && echo "✅ obj cleared" || echo "⚠️  obj not found"
    echo ""
else
    echo "Step 5: Skipping Library clear (Unity is running)"
    echo "   You can clear it manually after closing Unity"
    echo ""
fi

# Step 8: Summary and instructions
echo "=========================================="
echo "✅ Fix Script Complete!"
echo "=========================================="
echo ""
echo "Next Steps in Unity Editor:"
echo ""
echo "1. Open Unity Editor (if not already open)"
echo "   Project: $PROJECT_DIR"
echo ""
echo "2. Open Package Manager:"
echo "   Window > Package Manager"
echo "   Wait for refresh to complete (watch spinner)"
echo ""
echo "3. Find ROS TCP Connector:"
echo "   - Scroll down in package list"
echo "   - Look for 'ROS TCP Connector' or 'NavΛ 2.0.3'"
echo "   - Check for blue checkmark"
echo ""
echo "4. Reimport Package:"
echo "   - If checkmark present: Click 'Reimport' button"
echo "   - If missing: Click '+' > 'Add package from git URL'"
echo "   - URL: https://github.com/Unity-Technologies/ROS-TCP-Connector.git?path=/com.unity.robotics.ros-tcp-connector"
echo ""
echo "5. Wait for Import:"
echo "   - Watch progress in Package Manager"
echo "   - Wait for 'Import complete' message"
echo ""
echo "6. Verify GodModeRigor.cs:"
echo "   - Check Assets/Scripts/GodModeRigor.cs exists"
echo "   - Should have ~223 lines"
echo "   - If missing, restore from backup"
echo ""
echo "7. Check Console:"
echo "   - Window > General > Console"
echo "   - Should see no DLL loading errors"
echo "   - ROS TCP Connector should load successfully"
echo ""
echo "=========================================="
echo ""
echo "If Antivirus is Blocking:"
echo "- Check antivirus quarantine logs"
echo "- Look for 'UnityEngine.Robotics' or 'ROSTCPConnector'"
echo "- Restore if found, add Unity folder to exclusions"
echo "- Disable 'Game Mode' if it blocks network apps"
echo ""
echo "Package URL (if needed):"
echo "https://github.com/Unity-Technologies/ROS-TCP-Connector.git?path=/com.unity.robotics.ros-tcp-connector"
echo ""
