#!/bin/bash

# Fix all missing using UnityEngine.UI directives
# This script adds the missing using statement to all scripts that need it

echo "=========================================="
echo "Fixing Missing Using Directives"
echo "=========================================="
echo ""

PROJECT_DIR="/Users/frankvanlaarhoven/Desktop/anything-mcp/NAVA-AI/nava-ai"
cd "$PROJECT_DIR" || exit 1

# Find all scripts that use Image or Text but don't have using UnityEngine.UI
SCRIPTS_TO_FIX=$(find Assets/Scripts -name "*.cs" -type f -exec grep -l "Text[^M]\|Image[^M]" {} \; | xargs grep -L "using UnityEngine.UI" 2>/dev/null)

FIXED_COUNT=0
SKIPPED_COUNT=0

for script in $SCRIPTS_TO_FIX; do
    if [ -f "$script" ]; then
        # Check if it already has using UnityEngine.UI
        if ! grep -q "using UnityEngine.UI" "$script"; then
            # Check if it uses Image or Text
            if grep -q "Image[^M]\|Text[^M]" "$script"; then
                # Add using UnityEngine.UI after using UnityEngine
                if grep -q "^using UnityEngine;" "$script"; then
                    # Insert after using UnityEngine;
                    sed -i '' '/^using UnityEngine;$/a\
using UnityEngine.UI;
' "$script"
                    echo "✅ Fixed: $script"
                    ((FIXED_COUNT++))
                else
                    # Add at the top if no using UnityEngine
                    sed -i '' '1i\
using UnityEngine.UI;
' "$script"
                    echo "✅ Fixed: $script"
                    ((FIXED_COUNT++))
                fi
            else
                ((SKIPPED_COUNT++))
            fi
        else
            ((SKIPPED_COUNT++))
        fi
    fi
done

echo ""
echo "=========================================="
echo "✅ Fix Complete!"
echo "=========================================="
echo "Fixed: $FIXED_COUNT scripts"
echo "Skipped: $SKIPPED_COUNT scripts"
echo ""
echo "All scripts using Image or Text now have 'using UnityEngine.UI;'"
