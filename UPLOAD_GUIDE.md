# Upload Guide - How to Upload Desktop Folder to Unity Dashboard

## Quick Start (3 Methods)

### Method 1: Small Files (< 100 MB) - Direct Unity Import

**For:** Textures, small models, scripts, etc.

1. **Open Unity Editor**
2. **Drag & Drop:**
   - Open Finder/File Explorer
   - Navigate to your Desktop folder
   - Drag files directly into Unity's `Assets` folder in Project window
3. **Unity auto-imports** the files

**That's it!** Unity handles everything automatically.

---

### Method 2: Large Files (> 100 MB) - Streaming (Recommended)

**For:** Large scenes, big textures, 3D models, etc.

#### Step 1: Start Rust Server

```bash
# Terminal 1: Start Rust Asset Server
cd nav_lambda_server
cargo run --release

# Server will start on http://127.0.0.1:8080
```

#### Step 2: Copy Files to Server Assets Folder

```bash
# Create Assets folder if it doesn't exist
mkdir -p nav_lambda_server/Assets

# Copy your Desktop files to server
cp ~/Desktop/your_file.fbx nav_lambda_server/Assets/
# Or copy entire folder
cp -r ~/Desktop/your_folder nav_lambda_server/Assets/
```

#### Step 3: In Unity - Use Streaming Asset Loader

1. **Add StreamingAssetLoader to Scene:**
   - Create empty GameObject: `GameObject > Create Empty`
   - Name it: `StreamingAssetLoader`
   - Add component: `Add Component > StreamingAssetLoader`

2. **Configure:**
   - **Server URL:** `http://127.0.0.1:8080`
   - **Assets Path:** `Assets/StreamedAssets` (default)

3. **Download File:**
   ```csharp
   // Option A: Use Inspector
   // - Set "Server URL" to http://127.0.0.1:8080
   // - Call DownloadLargeFile("your_file.fbx") from script/button

   // Option B: Use Code
   StreamingAssetLoader loader = FindObjectOfType<StreamingAssetLoader>();
   loader.DownloadLargeFile("your_file.fbx");
   ```

4. **File streams in chunks** - No memory crash!

---

### Method 3: Unity Editor Menu (Automated)

**For:** Quick uploads with progress tracking

1. **Open Unity Editor**
2. **Menu:** `NAVA-AI Dashboard > Tools > Upload Assets`
3. **Select files** from Desktop
4. **Unity automatically:**
   - Detects file size
   - Uses streaming for large files
   - Shows progress
   - Imports when complete

---

## Detailed Workflow

### Upload Single File

#### Small File (< 100 MB)

1. **In Unity:**
   - Open Project window
   - Navigate to `Assets` folder
   - Drag file from Desktop into Unity

2. **Unity imports automatically**

#### Large File (> 100 MB)

1. **Start Rust Server:**
   ```bash
   cd nav_lambda_server
   cargo run --release
   ```

2. **Copy file to server:**
   ```bash
   cp ~/Desktop/large_scene.fbx nav_lambda_server/Assets/
   ```

3. **In Unity:**
   - Add `StreamingAssetLoader` component to scene
   - Set server URL: `http://127.0.0.1:8080`
   - Call: `loader.DownloadLargeFile("large_scene.fbx")`

4. **Monitor progress:**
   - Check status text in Unity
   - Watch Rust server logs for progress

---

### Upload Entire Folder

#### Option A: Copy to Unity Assets (Small folders)

```bash
# Copy entire Desktop folder to Unity Assets
cp -r ~/Desktop/MyAssets nava-ai/Assets/
```

Then in Unity:
- Right-click in Project window
- `Refresh` (or Unity auto-refreshes)
- Files appear in Assets

#### Option B: Use Rust Server (Large folders)

```bash
# 1. Copy folder to server
cp -r ~/Desktop/MyAssets nav_lambda_server/Assets/

# 2. In Unity, download each file
# (Or create batch download script)
```

---

## Unity Editor Script (Automated Upload)

### Create Upload Helper Script

**File:** `nava-ai/Assets/Scripts/Editor/AssetUploadHelper.cs`

```csharp
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class AssetUploadHelper : EditorWindow
{
    [MenuItem("NAVA-AI Dashboard/Tools/Upload Assets from Desktop")]
    static void UploadFromDesktop()
    {
        string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        
        // Show file picker
        string[] files = EditorUtility.OpenFilePanel(
            "Select Files to Upload",
            desktopPath,
            ""
        );
        
        if (files.Length == 0) return;
        
        foreach (string file in files)
        {
            FileInfo fileInfo = new FileInfo(file);
            long fileSizeMB = fileInfo.Length / (1024 * 1024);
            
            if (fileSizeMB > 100)
            {
                // Use streaming for large files
                Debug.Log($"[Upload] Large file detected ({fileSizeMB} MB). Use StreamingAssetLoader.");
                EditorUtility.DisplayDialog(
                    "Large File Detected",
                    $"File {fileInfo.Name} is {fileSizeMB} MB.\n\nUse StreamingAssetLoader for this file.",
                    "OK"
                );
            }
            else
            {
                // Copy to Assets
                string destPath = Path.Combine(Application.dataPath, fileInfo.Name);
                File.Copy(file, destPath, true);
                AssetDatabase.Refresh();
                Debug.Log($"[Upload] Copied {fileInfo.Name} to Assets");
            }
        }
    }
}
#endif
```

---

## Step-by-Step: Upload Desktop Folder

### Example: Upload "MyModels" folder from Desktop

#### Step 1: Check Folder Size

```bash
# Check total size
du -sh ~/Desktop/MyModels

# If > 100 MB, use streaming
# If < 100 MB, use direct copy
```

#### Step 2A: Small Folder (< 100 MB) - Direct Copy

```bash
# Copy to Unity Assets
cp -r ~/Desktop/MyModels nava-ai/Assets/

# In Unity: Right-click > Refresh
```

#### Step 2B: Large Folder (> 100 MB) - Streaming

```bash
# 1. Start Rust server
cd nav_lambda_server
cargo run --release

# 2. Copy folder to server (in new terminal)
cp -r ~/Desktop/MyModels nav_lambda_server/Assets/

# 3. In Unity: Use StreamingAssetLoader
# Download each file individually or create batch script
```

---

## Troubleshooting

### Unity Crashes During Upload

**Solution:** Use StreamingAssetLoader
1. Check file size: `ls -lh ~/Desktop/your_file`
2. If > 100 MB, use streaming method
3. Enable MemoryManager to monitor usage

### Rust Server Not Found

**Solution:** Build and run server
```bash
cd nav_lambda_server
cargo build --release
cargo run --release
```

### Files Not Appearing in Unity

**Solution:** Refresh Asset Database
- In Unity: `Assets > Refresh` (or `Ctrl+R`)
- Or: `Assets > Reimport All`

### Upload Stuck/Freezing

**Solution:**
1. Check MemoryManager window
2. Force GC: `Tools > Memory Management > Force Garbage Collection`
3. Cancel and retry with smaller chunks

---

## Quick Reference

### Small Files (< 100 MB)
```bash
# Copy to Unity Assets
cp ~/Desktop/file.fbx nava-ai/Assets/
```

### Large Files (> 100 MB)
```bash
# 1. Start server
cd nav_lambda_server && cargo run --release

# 2. Copy to server
cp ~/Desktop/file.fbx nav_lambda_server/Assets/

# 3. In Unity: Use StreamingAssetLoader
```

### Monitor Memory
```
Unity Menu: Tools > Memory Management > Show Window
```

---

## Best Practices

1. **Always check file size first**
2. **Use streaming for files > 100 MB**
3. **Monitor memory during uploads**
4. **Keep Rust server running for large files**
5. **Use MemoryManager to prevent crashes**

---

## Next Steps

After upload:
1. **Verify files** in Unity Project window
2. **Check import settings** (textures, models)
3. **Test in scene** to ensure everything works
4. **Optimize** if needed (compression, LODs)

---

## Need Help?

- **Memory Issues:** Check `MEMORY_OPTIMIZATION.md`
- **Server Problems:** Check Rust server logs
- **Import Errors:** Check Unity Console
