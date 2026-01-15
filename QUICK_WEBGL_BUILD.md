# Quick WebGL Build - NAVΛ Dashboard

## 🚀 Fastest Way to Run in Browser

### Step 1: Build WebGL (Choose One Method)

#### Method A: Unity Editor (Recommended - 5 minutes)

1. **Open Unity Editor**
   - Launch Unity Hub
   - Open project: `nava-ai/`

2. **Build WebGL**
   - Menu: **NAVA-AI Dashboard > Build > Quick WebGL Build**
   - Wait for build (2-5 minutes)
   - Build output: `build/webgl/`

#### Method B: Command Line (Automated)

```bash
# macOS
./build_webgl.sh

# Windows
build_webgl.bat
```

### Step 2: Run Local Server

```bash
# Quick run script
./run_webgl.sh

# Or manually:
cd build/webgl
python3 -m http.server 8000
```

### Step 3: Open Browser

```
http://localhost:8000
```

## ⚡ Current Status

**WebGL Build**: Not yet built (requires Unity Editor)

**To Build Now**:
1. Open Unity Editor
2. Open project: `nava-ai/`
3. Menu: **NAVA-AI Dashboard > Build > Quick WebGL Build**
4. Run: `./run_webgl.sh`

## 📋 What's Included

The WebGL build includes:
- ✅ All 77+ integrated features
- ✅ Dual-Mode Platform (Academia & Production)
- ✅ Swarm-AGI Coordination
- ✅ Rust-Unity Safety Core
- ✅ Complete Certification System
- ✅ Full UI Dashboard

## 🔧 Troubleshooting

### Build Fails
- Ensure WebGL module is installed in Unity Hub
- Check Unity Console for errors
- Verify Unity version: 2022.3.9f1 or compatible

### Browser Issues
- Use modern browser (Chrome, Firefox, Edge, Safari)
- Enable WebAssembly support
- Check browser console (F12) for errors

### ROS2 Connection
- WebGL cannot directly connect to ROS2
- Use WebSocket bridge or mock data mode
- See `WEBGL_BUILD_GUIDE.md` for details

## 📦 Deployment

Once built, deploy `build/webgl/` to:
- GitHub Pages
- Netlify
- Vercel
- AWS S3
- Any static hosting

## 🎯 Next Steps

1. **Build WebGL** (Unity Editor required)
2. **Test Locally** (`./run_webgl.sh`)
3. **Deploy** (upload to hosting)

The dashboard is ready for browser deployment once built!
