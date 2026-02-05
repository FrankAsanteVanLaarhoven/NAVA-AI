# Fix: Burst Assembly Resolution Error

## Error
```
Failed to resolve assembly: 'Assembly-CSharp-Editor, Version=0.0.0.0'
```

## Root Cause
Burst compiler is trying to analyze code that references Editor assemblies before they're compiled. This is a timing/dependency issue in Unity's compilation pipeline.

## Solutions

### Solution 1: Clear Library and Reimport (Recommended)
1. Close Unity Editor
2. Delete `Library` folder in project
3. Reopen Unity - it will reimport everything
4. Wait for compilation to complete

### Solution 2: Disable Burst Temporarily
1. Edit > Project Settings > Player
2. Other Settings > Burst AOT Settings
3. Disable Burst compilation temporarily
4. Recompile

### Solution 3: Fix Script Compilation Order
- Ensure Editor scripts are in `Assets/Scripts/Editor/` folder
- Ensure runtime scripts don't reference Editor-only code
- Use `#if UNITY_EDITOR` guards properly

## Quick Fix Script

Run this to clear and rebuild:

```bash
cd /Users/frankvanlaarhoven/Desktop/anything-mcp/NAVA-AI/nava-ai
rm -rf Library
# Then reopen Unity
```

## Verification

After fix, check:
- Unity Console has no Burst errors
- Assembly-CSharp-Editor.dll exists in Library/ScriptAssemblies
- Project compiles successfully
