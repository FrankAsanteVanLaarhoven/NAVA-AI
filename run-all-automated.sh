#!/bin/bash

# Master script to run all automated setup scripts for NAVA-AI Dashboard
# This script runs all automated setup and verification scripts

set -e  # Exit on error

echo "=========================================="
echo "NAVA-AI Automated Setup - Running All Scripts"
echo "=========================================="
echo ""

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Step 1: Unity Cloud Setup Verification
echo -e "${BLUE}Step 1: Verifying Unity Cloud Configuration...${NC}"
if [ -f "nava-ai/setup-unity-cloud.sh" ]; then
    cd nava-ai
    chmod +x setup-unity-cloud.sh
    ./setup-unity-cloud.sh
    cd ..
    echo -e "${GREEN}✓ Unity Cloud setup verified${NC}"
else
    echo -e "${YELLOW}⚠ Unity Cloud setup script not found${NC}"
fi
echo ""

# Step 2: Make ROS2 scripts executable
echo -e "${BLUE}Step 2: Setting up ROS2 scripts...${NC}"
if [ -f "ros2_scripts/mock_jetson_node.py" ]; then
    chmod +x ros2_scripts/mock_jetson_node.py
    echo -e "${GREEN}✓ ROS2 mock node script is executable${NC}"
    
    # Check if ROS2 is available
    if command -v ros2 &> /dev/null; then
        echo -e "${GREEN}✓ ROS2 is installed${NC}"
    else
        echo -e "${YELLOW}⚠ ROS2 not found in PATH (optional for testing)${NC}"
    fi
else
    echo -e "${YELLOW}⚠ ROS2 mock node script not found${NC}"
fi
echo ""

# Step 3: Make build scripts executable
echo -e "${BLUE}Step 3: Setting up build scripts...${NC}"
if [ -f "build_webgl.sh" ]; then
    chmod +x build_webgl.sh
    echo -e "${GREEN}✓ WebGL build script is executable${NC}"
else
    echo -e "${YELLOW}⚠ WebGL build script not found${NC}"
fi

if [ -f "run_webgl.sh" ]; then
    chmod +x run_webgl.sh
    echo -e "${GREEN}✓ WebGL run script is executable${NC}"
else
    echo -e "${YELLOW}⚠ WebGL run script not found${NC}"
fi
echo ""

# Step 4: Check Unity Editor status
echo -e "${BLUE}Step 4: Checking Unity Editor status...${NC}"
if ps aux | grep -i "Unity.*projectPath" | grep -v grep > /dev/null; then
    echo -e "${GREEN}✓ Unity Editor is running${NC}"
    UNITY_PID=$(ps aux | grep -i "Unity.*projectPath" | grep -v grep | awk '{print $2}')
    echo "  Unity PID: $UNITY_PID"
else
    echo -e "${YELLOW}⚠ Unity Editor is not running${NC}"
    echo "  To start Unity Editor, run:"
    echo "    cd nava-ai"
    echo "    /Applications/Unity/Hub/Editor/2022.3.9f1/6000.3.4f1/Unity.app/Contents/MacOS/Unity -projectPath ."
fi
echo ""

# Step 5: Verify project structure
echo -e "${BLUE}Step 5: Verifying project structure...${NC}"
REQUIRED_FILES=(
    "nava-ai/ProjectSettings/ProjectSettings.asset"
    "nava-ai/Packages/manifest.json"
    "nava-ai/Assets/Scripts/ROS2DashboardManager.cs"
    "ros2_scripts/mock_jetson_node.py"
)

ALL_GOOD=true
for file in "${REQUIRED_FILES[@]}"; do
    if [ -f "$file" ]; then
        echo -e "${GREEN}✓ Found: $file${NC}"
    else
        echo -e "${YELLOW}✗ Missing: $file${NC}"
        ALL_GOOD=false
    fi
done

if [ "$ALL_GOOD" = true ]; then
    echo -e "${GREEN}✓ All required files present${NC}"
else
    echo -e "${YELLOW}⚠ Some required files are missing${NC}"
fi
echo ""

# Step 6: Summary
echo "=========================================="
echo -e "${GREEN}Automated Setup Complete!${NC}"
echo "=========================================="
echo ""
echo "Next Steps:"
echo "1. If Unity Editor is running, wait for it to fully load"
echo "2. In Unity Editor menu: NAVA-AI Dashboard > Setup ROS2 Scene"
echo "3. Click 'Auto-Setup Complete Scene'"
echo "4. Press Play ▶️ to run the dashboard"
echo ""
echo "Optional: Test with ROS2 mock node:"
echo "  Terminal 1: ros2 daemon start"
echo "  Terminal 2: cd ros2_scripts && python3 mock_jetson_node.py"
echo ""
echo "=========================================="
