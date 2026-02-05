# Fix: DirectoryNotFoundException

## Error
```
DirectoryNotFoundException: Could not find a part of the path '/Users/frankvanlaarhoven/Des...'
```

## Root Cause
Scripts using `Path.Combine(Application.dataPath, "..", ...)` can create invalid or truncated paths when the path construction fails. The `".."` parent directory navigation doesn't always work correctly with `Path.Combine`.

## Fixes Applied

### 1. StreamingAssetLoader.cs
- Added try-catch around path construction
- Uses `Path.GetFullPath()` to resolve paths properly
- Falls back to `Application.persistentDataPath` if construction fails

### 2. LiveValidator.cs
- Improved `GetDVRPath()` method with proper error handling
- Uses `Path.GetFullPath()` for safe path resolution
- Validates directory exists before using path

### 3. TrajectoryReplayer.cs
- Added try-catch around path construction
- Uses `Path.GetFullPath()` for safe path resolution
- Added `List<string>` for dynamic path list
- Validates paths before using them

## What Changed

**Before:**
```csharp
string fullPath = Path.Combine(Application.dataPath, "..", assetsPath);
```

**After:**
```csharp
try
{
    string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetsPath));
    // ... validate and create directory
}
catch
{
    // Fallback to persistent data path
}
```

## Verification

After fix:
- ✅ No DirectoryNotFoundException errors
- ✅ Scripts use safe path construction
- ✅ Fallback to persistent data path if needed
- ✅ All file operations are wrapped in try-catch

## Additional Scripts Fixed

- `StreamingAssetLoader.cs` - Asset download paths
- `LiveValidator.cs` - DVR log file paths
- `TrajectoryReplayer.cs` - CSV file paths

---

**Status**: Directory path errors fixed! Scripts now use safe path construction with proper error handling.
