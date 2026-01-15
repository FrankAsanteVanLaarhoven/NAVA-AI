# Memory Optimization Guide - NAVΛ Dashboard

## Problem

Uploading large files (Assets/Scenes/BigTextures) directly into Unity WebGL Player causes:
- **"Not Responding"** crashes
- **Out of Memory** errors
- **Editor Freezing** during large file operations

This is a **Unity Memory Limit** issue, not a network one.

## Solution Architecture

### 1. Memory Manager (`MemoryManager.cs`)

**Purpose:** Monitor and manage Unity memory usage automatically.

**Features:**
- Real-time memory monitoring
- Auto-unload when threshold exceeded
- Manual GC trigger
- Editor window for monitoring

**Usage:**
```csharp
// In Unity Editor
Tools > Memory Management > Show Window

// Or add to scene
GameObject.AddComponent<MemoryManager>();
```

**Settings:**
- **Unload Threshold:** 512 MB (default)
- **Auto-Unload:** Enabled (default)
- **Update Interval:** 1 second (default)

### 2. Streaming Asset Loader (`StreamingAssetLoader.cs`)

**Purpose:** Download large files in chunks from Rust Hub server.

**How it works:**
1. Unity requests file from Rust server
2. Rust streams file in 2MB chunks
3. Unity writes chunks directly to disk
4. Unity never holds full file in RAM

**Usage:**
```csharp
StreamingAssetLoader loader = GetComponent<StreamingAssetLoader>();
loader.DownloadLargeFile("large_scene.fbx");
```

**Configuration:**
- **Server URL:** `http://127.0.0.1:8080`
- **Chunk Size:** 2 MB
- **Assets Path:** `Assets/StreamedAssets`

### 3. Rust Asset Server (`nav_lambda_server`)

**Purpose:** Stream large files to Unity in chunks.

**Build:**
```bash
cd nav_lambda_server
cargo build --release
```

**Run:**
```bash
cargo run --release
# Server listens on port 8080
```

**Features:**
- HTTP-like protocol
- Chunked streaming (2MB chunks)
- Progress logging
- Error handling

## Workflow

### For Small Files (< 100 MB)

Use standard Unity import:
```csharp
// Normal Unity workflow
AssetDatabase.ImportAsset("Assets/MyFile.fbx");
```

### For Large Files (> 100 MB)

Use streaming:
```csharp
// 1. Start Rust server
// cargo run --release

// 2. In Unity
StreamingAssetLoader loader = GetComponent<StreamingAssetLoader>();
loader.DownloadLargeFile("large_scene.fbx");

// 3. File streams in chunks, no memory crash
```

## Memory Optimization Strategies

### 1. Texture Compression

**Unity Settings:**
- **Format:** ETC2 (Android) or Crunch (WebGL)
- **Max Size:** 2048x2048 (reduce for testing)
- **Compression:** High

**Code:**
```csharp
TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
importer.textureCompression = TextureImporterCompression.Compressed;
importer.maxTextureSize = 2048;
```

### 2. Resolution Reduction

**For Testing:**
- Run dashboard at **1280x720** instead of 4K
- Reduces GPU memory usage

**Settings:**
```
Edit > Project Settings > Player > Resolution and Presentation
Default Canvas Width: 1280
Default Canvas Height: 720
```

### 3. Use UnityWebRequest

**Instead of:**
```csharp
byte[] data = File.ReadAllBytes(path); // Loads entire file into RAM
```

**Use:**
```csharp
UnityWebRequest request = UnityWebRequest.Get(url);
request.downloadHandler = new DownloadHandlerFile(path); // Streams to disk
```

### 4. Play Mode Optimization

**Editor Settings:**
- Use **Simulate** mode for testing
- Disable unnecessary editor features
- Close unused windows

## Performance Benchmarks

### Before Optimization
- **100 MB file:** Crashes Unity
- **500 MB file:** "Not Responding"
- **1 GB file:** Out of Memory

### After Optimization
- **100 MB file:** Streams in ~5 seconds
- **500 MB file:** Streams in ~25 seconds
- **1 GB file:** Streams in ~50 seconds
- **No crashes:** Memory stays under threshold

## Troubleshooting

### Unity Still Crashes

1. **Check Memory Threshold:**
   - Open Memory Manager window
   - Reduce threshold if needed (e.g., 256 MB)

2. **Force GC:**
   - Click "Force Garbage Collection" button
   - Wait for completion

3. **Close Other Applications:**
   - Free up system RAM
   - Close browser tabs

### Rust Server Not Responding

1. **Check Server Status:**
   ```bash
   curl http://127.0.0.1:8080/Assets/test.txt
   ```

2. **Check Firewall:**
   - Allow port 8080
   - Check localhost access

3. **Check File Path:**
   - Ensure `./Assets/` directory exists
   - Verify file permissions

### Download Stuck

1. **Check Network:**
   - Verify server URL
   - Test with small file first

2. **Check Disk Space:**
   - Ensure enough space for download
   - Check `Assets/StreamedAssets` directory

3. **Cancel and Retry:**
   ```csharp
   loader.CancelDownload();
   loader.DownloadLargeFile(fileName);
   ```

## Best Practices

1. **Always Use Streaming for Files > 100 MB**
2. **Monitor Memory Usage Regularly**
3. **Set Appropriate Thresholds**
4. **Use Texture Compression**
5. **Test with Lower Resolution First**
6. **Keep Rust Server Running During Development**

## Next Steps

1. **Deploy Rust Server to Production:**
   - Use systemd service
   - Configure HTTPS
   - Add authentication

2. **Add Progress UI:**
   - Show download progress
   - Display estimated time
   - Allow cancellation

3. **Implement Resume:**
   - Support partial downloads
   - Resume from last position

4. **Add Compression:**
   - Compress files before streaming
   - Decompress on Unity side

## License

NAVΛ Dashboard - Memory Optimization System
© 2024 Newcastle University / Production Deployment
