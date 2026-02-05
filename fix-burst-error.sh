#!/bin/bash

# Fix Burst Assembly Resolution Error
# This script clears Unity's cache and forces a clean rebuild

echo "=========================================="
echo "Fixing Burst Assembly Resolution Error"
echo "=========================================="
echo ""

PROJECT_DIR="/Users/frankvanlaarhoven/Desktop/anything-mcp/NAVA-AI/nava-ai"

cd "$PROJECT_DIR" || exit 1

echo "⚠️  WARNING: This will clear Unity's Library folder"
echo "   Unity will need to reimport all assets (may take a few minutes)"
echo ""
read -p "Continue? (y/n) " -n 1 -r
echo ""

if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "Cancelled."
    exit 1
fi

echo ""
echo "Step 1: Checking Unity Editor status..."
if ps aux | grep -i "Unity.*projectPath.*nava-ai" | grep -v grep > /dev/null; then
    echo "⚠️  Unity Editor is running!"
    echo "   Please close Unity Editor first, then run this script again."
    exit 1
fi

echo "✅ Unity Editor is not running"
echo ""

echo "Step 2: Backing up Library folder..."
if [ -d "Library" ]; then
    BACKUP_NAME="Library_backup_$(date +%Y%m%d_%H%M%S)"
    mv Library "$BACKUP_NAME"
    echo "✅ Backed up to: $BACKUP_NAME"
else
    echo "⚠️  Library folder not found (already cleared?)"
fi

echo ""
echo "Step 3: Clearing Temp folder..."
rm -rf Temp 2>/dev/null
echo "✅ Temp folder cleared"

echo ""
echo "Step 4: Clearing obj folder..."
rm -rf obj 2>/dev/null
echo "✅ obj folder cleared"

echo ""
echo "=========================================="
echo "✅ Cleanup Complete!"
echo "=========================================="
echo ""
echo "Next steps:"
echo "1. Open Unity Editor"
echo "2. Open this project: $PROJECT_DIR"
echo "3. Wait for Unity to reimport assets (watch progress bar)"
echo "4. Wait for scripts to compile"
echo "5. The Burst error should be resolved"
echo ""
echo "Note: First import may take 5-10 minutes"
echo ""
