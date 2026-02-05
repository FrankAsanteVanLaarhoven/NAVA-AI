# Fix: Burst Assembly Resolution Error

## Error Message
```
Failed to resolve assembly: 'Assembly-CSharp-Editor, Version=0.0.0.0'
```

## Root Cause
The Burst compiler is trying to analyze code before Unity has compiled the Editor scripts. The `Assembly-CSharp-Editor.dll` doesn't exist yet, causing a resolution failure.

## Solution: Force Clean Rebuild

### Option 1: Quick Fix (Recommended)
1. **Close Unity Editor** (if running)
2. **Delete Library folder**:
   ```bash
   cd /Users/frankvanlaarhoven/Desktop/anything-mcp/NAVA-AI/nava-ai
   rm -rf Library
   ```
3. **Reopen Unity Editor**
4. **Wait for reimport** (5-10 minutes)
5. **Wait for compilation** to complete

### Option 2: Use Fix Script
```bash
cd /Users/frankvanlaarhoven/Desktop/anything-mcp/NAVA-AI
./fix-burst-error.sh
```

### Option 3: Manual Steps in Unity
1. **Assets > Reimport All** (may take time)
2. **Edit > Preferences > External Tools > Regenerate project files**
3. **Close and reopen Unity**

## Why This Happens
- Unity compiles scripts in order: Runtime → Editor
- Burst compiler runs during Runtime compilation
- If Burst tries to analyze code that references Editor assemblies before they're built, it fails
- Clearing Library forces a clean rebuild with proper compilation order

## Verification
After fix, check:
- ✅ No Burst errors in Console
- ✅ `Library/ScriptAssemblies/Assembly-CSharp-Editor.dll` exists
- ✅ Project compiles successfully
- ✅ Can enter Play mode

## If Error Persists
1. Check for scripts mixing runtime and Editor code
2. Ensure Editor scripts are in `Assets/Scripts/Editor/` folder
3. Verify `#if UNITY_EDITOR` guards are correct
4. Try disabling Burst temporarily: **Edit > Project Settings > Player > Burst AOT Settings**

---

**Status**: This is a Unity compilation order issue. Clearing Library and rebuilding fixes it.
