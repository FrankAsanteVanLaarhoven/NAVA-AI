# WebGL Build Guide - NAVΛ Dashboard

## Overview

The NAVΛ Dashboard can be built as a WebGL application for browser deployment. This allows running the dashboard in any modern web browser without requiring Unity installation.

## Prerequisites

1. **Unity Editor** (2021.3 LTS or later recommended)
2. **WebGL Build Support** module installed in Unity Hub
3. **Python 3** (for local testing server)

## Building via Unity Editor

### Method 1: Quick Build (Recommended)

1. Open Unity Editor
2. Open the project: `nava-ai/`
3. Go to menu: **NAVA-AI Dashboard > Build > Quick WebGL Build**
4. Wait for build to complete
5. Build output will be in `build/webgl/`

### Method 2: Manual Build

1. Open Unity Editor
2. Go to **File > Build Settings**
3. Select **WebGL** as the platform
4. Click **Switch Platform** (if needed)
5. Click **Build** or **Build and Run**
6. Choose output directory (e.g., `build/webgl/`)

## Building via Command Line

### macOS/Linux

```bash
# Run build script
./build_webgl.sh
```

### Windows

```batch
# Run build script
build_webgl.bat
```

### Manual Command

```bash
# macOS
/Applications/Unity/Hub/Editor/[VERSION]/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -quit \
  -projectPath ./nava-ai \
  -buildTarget WebGL \
  -buildPath ./build/webgl

# Windows
"C:\Program Files\Unity\Hub\Editor\[VERSION]\Editor\Unity.exe" ^
  -batchmode ^
  -quit ^
  -projectPath .\nava-ai ^
  -buildTarget WebGL ^
  -buildPath .\build\webgl
```

## Running the Build

### Local Testing

1. Navigate to build directory:
   ```bash
   cd build/webgl
   ```

2. Start a local web server:
   ```bash
   # Python 3
   python3 -m http.server 8000
   
   # Python 2
   python -m SimpleHTTPServer 8000
   
   # Node.js (if installed)
   npx http-server -p 8000
   ```

3. Open browser:
   ```
   http://localhost:8000
   ```

### Deployment

#### Option 1: Static Hosting

Upload the entire `build/webgl/` folder to:
- **GitHub Pages**: Push to `gh-pages` branch
- **Netlify**: Drag and drop folder
- **Vercel**: Deploy folder
- **AWS S3**: Upload to S3 bucket with static hosting

#### Option 2: Web Server

1. Copy `build/webgl/` contents to web server
2. Ensure server supports:
   - `.wasm` files (WebAssembly)
   - `.br` or `.gz` compression (Brotli/Gzip)
   - Proper MIME types

## WebGL Limitations

### Known Limitations

1. **ROS2 Connection**: WebGL builds cannot directly connect to ROS2 via TCP. Options:
   - Use WebSocket bridge (requires server)
   - Use mock data for demo
   - Use WebRTC for real-time streaming

2. **File System**: Limited file system access
   - Use PlayerPrefs for settings
   - Use WebGL-specific storage APIs

3. **Performance**: 
   - Lower performance than native builds
   - Large initial download size
   - Browser memory limits

### Workarounds

1. **ROS2 Bridge**: Create a WebSocket server that bridges ROS2 to WebGL
2. **Mock Mode**: Enable mock data mode for demos
3. **Data Compression**: Enable compression in build settings

## Build Settings

### Recommended Settings

1. **Compression Format**: Brotli (best) or Gzip
2. **Code Optimization**: Size
3. **Exception Support**: None (for smaller builds)
4. **Data Caching**: Enabled
5. **Memory Size**: 256 MB (adjust based on needs)

### Access in Unity

1. **Edit > Project Settings > Player**
2. Select **WebGL** tab
3. Configure settings as needed

## Troubleshooting

### Build Fails

- Check Unity Console for errors
- Ensure WebGL module is installed
- Verify all dependencies are compatible

### Build Too Large

- Enable compression
- Remove unused assets
- Use Asset Bundles for large content
- Optimize textures and models

### Browser Issues

- Use modern browser (Chrome, Firefox, Edge, Safari)
- Enable WebAssembly support
- Check browser console for errors
- Ensure HTTPS for production (required for some features)

## Quick Start

1. **Build**:
   ```bash
   # In Unity Editor: NAVA-AI Dashboard > Build > Quick WebGL Build
   ```

2. **Run Locally**:
   ```bash
   cd build/webgl
   python3 -m http.server 8000
   ```

3. **Open Browser**:
   ```
   http://localhost:8000
   ```

## Production Deployment

For production deployment:

1. Build with **Release** configuration
2. Enable **Compression** (Brotli)
3. Test in multiple browsers
4. Deploy to CDN for faster loading
5. Configure HTTPS (required for WebAssembly)

## Notes

- WebGL builds are **read-only** - no file writing
- ROS2 connection requires **WebSocket bridge**
- Large builds may take time to load
- Browser compatibility varies

The NAVΛ Dashboard WebGL build provides full visualization capabilities in the browser, perfect for remote monitoring and demos.
