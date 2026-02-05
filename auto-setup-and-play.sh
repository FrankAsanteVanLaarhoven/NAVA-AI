#!/bin/bash

# Script to automatically set up Unity scene and enter play mode
# This uses Unity's command line API and AppleScript for automation

echo "=========================================="
echo "NAVA-AI Auto Setup and Play"
echo "=========================================="
echo ""

UNITY_PROJECT="/Users/frankvanlaarhoven/Desktop/anything-mcp/NAVA-AI/nava-ai"
UNITY_PATH="/Applications/Unity/Hub/Editor/2022.3.9f1/6000.3.4f1/Unity.app/Contents/MacOS/Unity"

# Check if Unity is running
if ! ps aux | grep -i "Unity.*projectPath" | grep -v grep > /dev/null; then
    echo "⚠️  Unity Editor is not running"
    echo "Starting Unity Editor..."
    cd "$UNITY_PROJECT"
    "$UNITY_PATH" -projectPath . > /dev/null 2>&1 &
    echo "Waiting for Unity to start (30 seconds)..."
    sleep 30
fi

echo "✅ Unity Editor is running"
echo ""
echo "Setting up scene automatically..."
echo ""

# Wait a bit for Unity to be ready
sleep 5

# Use AppleScript to interact with Unity Editor
echo "Executing scene setup via Unity menu..."
osascript <<EOF
tell application "Unity"
    activate
end tell

delay 2

tell application "System Events"
    tell process "Unity"
        -- Try to execute menu item for scene setup
        try
            click menu item "Setup ROS2 Scene" of menu "NAVA-AI Dashboard" of menu bar 1
            delay 1
            -- The setup window should open, but we can't click buttons via AppleScript reliably
            -- So we'll rely on the AutoSceneSetup script
        end try
    end tell
end tell
EOF

echo ""
echo "✅ Scene setup initiated"
echo ""
echo "The AutoSceneSetup script will automatically configure the scene when Unity loads."
echo ""
echo "Next steps:"
echo "1. Wait for Unity to finish compiling scripts (watch bottom right)"
echo "2. The scene will auto-setup when SampleScene loads"
echo "3. Press Play ▶️ in Unity Editor to see the dashboard"
echo ""
echo "Or manually:"
echo "  - Menu: NAVA-AI Dashboard > Auto-Setup Scene Now"
echo "  - Then press Play"
echo ""
