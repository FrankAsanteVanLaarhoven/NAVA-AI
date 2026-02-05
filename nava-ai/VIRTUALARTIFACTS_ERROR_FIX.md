# Fix: Unity VirtualArtifacts Error

## Error Message
```
Opening file failed
Opening file VirtualArtifacts/Primary/8aafaa78fe944854997fef757ff4ba72: No such file or directory
```

## What is VirtualArtifacts?

`VirtualArtifacts` is Unity's **internal cache system** for:
- Package metadata
- Asset pipeline data
- Build artifacts
- Temporary compilation files

This error occurs when Unity tries to access a cached file that:
- Was deleted during Library folder clearing
- Is corrupted
- Doesn't exist due to incomplete package import

## Root Cause

After clearing the `Library` folder, Unity is rebuilding its cache. During this process:
1. Unity creates `VirtualArtifacts` entries
2. Some files may not exist yet (still importing)
3. Unity tries to access them → **Error**

## Solution

### Option 1: Wait for Import (Recommended)

If Unity is still importing packages:
1. **Click "Cancel"** on the error dialog
2. **Wait for package import** to complete
3. **Error should resolve** once packages finish loading

### Option 2: Clear All Caches (If Error Persists)

If the error keeps appearing:

1. **Close Unity Editor completely**

2. **Run the fix script**:
   ```bash
   cd /Users/frankvanlaarhoven/Desktop/anything-mcp/NAVA-AI/nava-ai
   ./fix-virtualartifacts-error.sh
   ```

3. **Reopen Unity Editor**

4. **Wait for packages to import** (5-10 minutes)

### Option 3: Manual Cache Clear

If script doesn't work:

1. **Close Unity Editor**

2. **Delete these folders**:
   ```bash
   rm -rf Library
   rm -rf Temp
   rm -rf obj
   rm -rf ~/Library/Unity/cache
   ```

3. **Reopen Unity Editor**

4. **Wait for import**

## What the Script Does

The `fix-virtualartifacts-error.sh` script:
- ✅ Checks if Unity is running (must be closed)
- ✅ Clears `Library` folder
- ✅ Clears `Temp` folder
- ✅ Clears `obj` folder
- ✅ Clears Unity global package cache
- ✅ Verifies `manifest.json` is intact
- ✅ Ensures packages are configured

## Expected Behavior After Fix

1. Unity Editor opens without errors
2. Packages start importing automatically
3. Burst compilation begins
4. `VirtualArtifacts` rebuilds correctly
5. No more "Opening file failed" errors

## If Error Persists

If the error continues after clearing caches:

1. **Check Unity Editor version**:
   - Should be Unity 6.3 LTS
   - Verify in Unity Hub

2. **Check disk space**:
   ```bash
   df -h
   ```
   - Need at least 10GB free

3. **Check permissions**:
   ```bash
   ls -la NAVA-AI/nava-ai/
   ```
   - Should have read/write access

4. **Reinstall packages**:
   - Window > Package Manager
   - Find each package
   - Click "Reimport"

## Prevention

To avoid this error in the future:
- ✅ Don't manually delete files from `Library` while Unity is running
- ✅ Always close Unity before clearing caches
- ✅ Wait for package imports to complete before building
- ✅ Use Unity's built-in "Clear Cache" options when available

---

**Status**: This is a **temporary cache issue**. Clearing caches and waiting for Unity to rebuild should resolve it.
