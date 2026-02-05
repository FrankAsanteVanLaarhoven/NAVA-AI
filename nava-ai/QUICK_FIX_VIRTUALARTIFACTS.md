# Quick Fix: VirtualArtifacts Error

## Error
```
Opening file failed
Opening file VirtualArtifacts/Primary/8aafaa78fe944854997fef757ff4ba72: No such file or directory
```

## What This Means

Unity is trying to access a **Visual Effect Graph** texture file that hasn't finished importing yet. This is a **temporary error** during package import.

## Quick Solution

### Option 1: Wait (Recommended)
1. **Click "Cancel"** on the error dialog
2. **Wait 2-5 minutes** for packages to finish importing
3. **Error will disappear** once Visual Effect Graph package loads

### Option 2: If Error Keeps Appearing

1. **Close Unity Editor completely**

2. **Run the fix script**:
   ```bash
   cd /Users/frankvanlaarhoven/Desktop/anything-mcp/NAVA-AI/nava-ai
   ./fix-virtualartifacts-error.sh
   ```

3. **Reopen Unity Editor**

4. **Wait for import** (5-10 minutes)

## Why This Happens

- **Visual Effect Graph package** is still importing
- Unity tries to access texture file (`8aafaa78fe944854997fef757ff4ba72`)
- File doesn't exist yet → Error
- Once package finishes → File exists → Error gone

## The GUID

`8aafaa78fe944854997fef757ff4ba72` is a texture GUID from:
- **Package**: `com.unity.visualeffectgraph`
- **File**: `DefaultDot.tga` (default texture)
- **Location**: Visual Effect Graph package assets

## Status Check

Your `manifest.json` includes:
```json
"com.unity.visualeffectgraph": "17.3.0"
```

This package is **correctly configured** - it just needs time to import.

---

**Action**: Click "Cancel" and wait for package import to complete. This is a normal temporary error during Unity's package import process.
