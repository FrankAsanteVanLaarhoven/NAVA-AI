# Quick Fix: Burst Assembly Error

## The Problem
Burst compiler can't find `Assembly-CSharp-Editor.dll` because it hasn't been compiled yet.

## Fastest Fix

**Close Unity Editor, then run:**

```bash
cd /Users/frankvanlaarhoven/Desktop/anything-mcp/NAVA-AI/nava-ai
rm -rf Library
```

**Then reopen Unity Editor** - it will rebuild everything.

## Alternative: If Unity is Running

1. In Unity: **Assets > Reimport All**
2. Wait for compilation
3. If still fails, close Unity and delete Library folder

## Why This Works
Clearing Library forces Unity to:
1. Recompile Editor scripts first
2. Then compile Runtime scripts
3. Burst can now find the Editor assembly

---

**Time**: 5-10 minutes for full reimport
**Result**: Burst error resolved, project compiles successfully
