#!/bin/bash

# Unity Cloud Setup Script for NAVA-AI Dashboard
# Project ID: bd003673-9721-43af-9135-8dde92ffb263
# Organization ID: 5773978016957

echo "=========================================="
echo "NAVA-AI Unity Cloud Setup"
echo "=========================================="
echo ""

# Check if Unity is installed
if ! command -v Unity &> /dev/null; then
    echo "⚠️  Unity not found in PATH. Please ensure Unity 2022.3.9f1 is installed."
    echo "   You can download it from: https://unity.com/download"
    echo ""
fi

# Verify project settings
echo "✅ Checking project configuration..."
PROJECT_SETTINGS="ProjectSettings/ProjectSettings.asset"

if grep -q "cloudProjectId: bd003673-9721-43af-9135-8dde92ffb263" "$PROJECT_SETTINGS"; then
    echo "   ✓ Cloud Project ID configured correctly"
else
    echo "   ✗ Cloud Project ID not found. Please verify ProjectSettings.asset"
fi

if grep -q "cloudEnabled: 1" "$PROJECT_SETTINGS"; then
    echo "   ✓ Cloud services enabled"
else
    echo "   ✗ Cloud services not enabled"
fi

if grep -q "organizationId: 5773978016957" "$PROJECT_SETTINGS"; then
    echo "   ✓ Organization ID configured correctly"
else
    echo "   ✗ Organization ID not found"
fi

echo ""
echo "📦 Checking Unity packages..."

# Check for ROS-TCP-Connector
if grep -q "com.unity.robotics.ros-tcp-connector" "Packages/manifest.json"; then
    echo "   ✓ ROS-TCP-Connector package found"
else
    echo "   ✗ ROS-TCP-Connector package missing"
fi

# Check for Cloud Build
if grep -q "com.unity.services.cloud-build" "Packages/manifest.json"; then
    echo "   ✓ Unity Cloud Build package found"
else
    echo "   ✗ Unity Cloud Build package missing"
fi

echo ""
echo "=========================================="
echo "Next Steps:"
echo "=========================================="
echo ""
echo "1. Open Unity Hub"
echo "2. Add this project: $(pwd)"
echo "3. Open the project with Unity 2022.3.9f1"
echo "4. In Unity Editor:"
echo "   - Go to Edit > Project Settings > Services"
echo "   - Sign in with your Unity account"
echo "   - Verify the project is linked to:"
echo "     Project ID: bd003673-9721-43af-9135-8dde92ffb263"
echo "     Organization: 5773978016957"
echo "5. Enable Unity Cloud Build:"
echo "   - Go to Window > General > Services"
echo "   - Enable Cloud Build service"
echo "6. Configure ROS2 connection:"
echo "   - In the scene, find ROS_Manager GameObject"
echo "   - Set ROS IP (127.0.0.1 for local, or Jetson IP for hardware)"
echo "   - Set ROS Port (default: 10000)"
echo ""
echo "For Unity Cloud Build:"
echo "- Go to https://cloud.unity.com"
echo "- Select your project"
echo "- Configure build targets (Windows, Mac, Linux, WebGL)"
echo "- Set up build configurations"
echo ""
echo "=========================================="
