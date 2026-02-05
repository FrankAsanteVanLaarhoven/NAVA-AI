#!/bin/bash

# Ensure Required Packages are in manifest.json
# This script verifies and adds missing packages if needed

echo "=========================================="
echo "Ensuring Required Packages"
echo "=========================================="
echo ""

PROJECT_DIR="/Users/frankvanlaarhoven/Desktop/anything-mcp/NAVA-AI/nava-ai"
cd "$PROJECT_DIR" || exit 1

MANIFEST="Packages/manifest.json"

# Required packages
REQUIRED_PACKAGES=(
    'com.unity.ugui:2.0.0'
    'com.unity.robotics.ros-tcp-connector:https://github.com/Unity-Technologies/ROS-TCP-Connector.git?path=/com.unity.robotics.ros-tcp-connector'
)

echo "Checking manifest.json..."
echo ""

# Check UGUI
if grep -q '"com.unity.ugui"' "$MANIFEST"; then
    UGUI_VERSION=$(grep '"com.unity.ugui"' "$MANIFEST" | sed 's/.*"com.unity.ugui": *"\([^"]*\)".*/\1/')
    echo "✅ UGUI package found: version $UGUI_VERSION"
else
    echo "❌ UGUI package MISSING - adding..."
    # This would require JSON manipulation - better to do manually
    echo "   Please add manually: \"com.unity.ugui\": \"2.0.0\""
fi

# Check ROS TCP Connector
if grep -q '"com.unity.robotics.ros-tcp-connector"' "$MANIFEST"; then
    ROS_URL=$(grep '"com.unity.robotics.ros-tcp-connector"' "$MANIFEST" | sed 's/.*"com.unity.robotics.ros-tcp-connector": *"\([^"]*\)".*/\1/')
    echo "✅ ROS TCP Connector found: $ROS_URL"
else
    echo "❌ ROS TCP Connector MISSING - adding..."
    echo "   Please add manually"
fi

echo ""
echo "=========================================="
echo "Package Verification Complete"
echo "=========================================="
echo ""
echo "If packages are missing, add them to Packages/manifest.json:"
echo ""
echo "Required entries:"
echo '  "com.unity.ugui": "2.0.0",'
echo '  "com.unity.robotics.ros-tcp-connector": "https://github.com/Unity-Technologies/ROS-TCP-Connector.git?path=/com.unity.robotics.ros-tcp-connector",'
echo ""
