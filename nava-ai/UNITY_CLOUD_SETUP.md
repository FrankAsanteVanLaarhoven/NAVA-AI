# Unity Cloud Setup Guide for NAVA-AI Dashboard

## Project Information
- **Project ID**: `bd003673-9721-43af-9135-8dde92ffb263`
- **Organization ID**: `5773978016957`
- **Unity Version**: 2022.3.9f1
- **Project Name**: NAVA-AI Dashboard

## Configuration Status

✅ **Project Settings Updated**
- Cloud Project ID: `bd003673-9721-43af-9135-8dde92ffb263`
- Organization ID: `5773978016957`
- Cloud Enabled: `1` (enabled)
- Project Name: `NAVA-AI Dashboard`

## Setup Instructions

### 1. Open Project in Unity

1. Open **Unity Hub**
2. Click **Add** and select the `nava-ai` folder
3. Select Unity version **2022.3.9f1** (or compatible)
4. Click **Open**

### 2. Verify Unity Cloud Connection

1. In Unity Editor, go to **Edit > Project Settings**
2. Navigate to **Services** (or **Cloud Project**)
3. Sign in with your Unity account if prompted
4. Verify the project shows:
   - **Project ID**: `bd003673-9721-43af-9135-8dde92ffb263`
   - **Organization**: `5773978016957`

### 3. Enable Unity Services

1. Go to **Window > General > Services** (or **Window > Services**)
2. Sign in if not already signed in
3. Enable the following services as needed:
   - **Cloud Build** (for automated builds)
   - **Cloud Save** (already in manifest)
   - **Analytics** (optional)
   - **Remote Config** (already in manifest)

### 4. Configure Unity Cloud Build

1. Go to https://cloud.unity.com
2. Navigate to your project: `bd003673-9721-43af-9135-8dde92ffb263`
3. Click on **Build** in the left sidebar
4. Configure build targets:
   - **Windows** (Standalone)
   - **macOS** (Standalone)
   - **Linux** (Standalone)
   - **WebGL** (for browser deployment)
5. Set up build configurations:
   - Development builds
   - Release builds
   - Custom build scripts if needed

### 5. ROS2 Configuration

The project uses ROS-TCP-Connector for ROS2 communication.

#### Local Testing Setup:
1. In Unity Editor, find the `ROS_Manager` GameObject in the scene
2. In the Inspector, locate `ROS2DashboardManager` component
3. Set **Ros IP** to `127.0.0.1` (localhost)
4. Set **Ros Port** to `10000`
5. Assign robot GameObjects and UI components

#### Real Hardware Setup (Jetson):
1. Find the Jetson's IP address:
   ```bash
   # On Jetson device
   hostname -I
   ```
2. In Unity Editor, update **Ros IP** to the Jetson IP (e.g., `192.168.1.50`)
3. Keep **Ros Port** as `10000` (or match your ROS2 bridge config)

### 6. Package Dependencies

The following packages are already configured in `Packages/manifest.json`:

- ✅ `com.unity.robotics.ros-tcp-connector` - ROS2 communication
- ✅ `com.unity.services.cloud-build` - Cloud Build service
- ✅ `com.unity.services.cloudsave` - Cloud Save service
- ✅ `com.unity.remote-config` - Remote configuration
- ✅ `com.unity.services.multiplayer` - Multiplayer support
- ✅ `com.unity.ml-agents` - ML Agents for AI

### 7. Build and Deploy

#### Unity Desktop Build:
1. Go to **File > Build Settings**
2. Select target platform (Windows, Mac, Linux)
3. Click **Build** or **Build and Run**
4. Choose output directory

#### Unity Cloud Build:
1. Push your project to a Git repository (GitHub, GitLab, etc.)
2. In Unity Cloud Dashboard, link your repository
3. Configure build settings
4. Trigger builds manually or automatically on commits

#### WebGL Build:
1. Go to **File > Build Settings**
2. Select **WebGL** platform
3. Click **Switch Platform** (if needed)
4. Click **Build**
5. Deploy the build folder to a web server

### 8. Running the Project

#### With Mock ROS2 Node (Local Testing):
```bash
# Terminal 1: Start ROS2 daemon
ros2 daemon start

# Terminal 2: Run mock Jetson node
cd ros2_scripts
python3 mock_jetson_node.py

# Unity: Press Play in Editor
```

#### With Real ROS2 Hardware:
1. Deploy your ROS2 navigation stack to Jetson
2. Ensure ROS2 bridge is running on port 10000
3. Update Unity ROS IP to Jetson's IP address
4. Press Play in Unity Editor

## Troubleshooting

### Unity Cloud Not Connecting
- Verify you're signed in to Unity account
- Check internet connection
- Verify project ID matches: `bd003673-9721-43af-9135-8dde92ffb263`
- Try re-linking project in Unity Editor

### ROS2 Connection Issues
- Verify ROS2 daemon is running: `ros2 daemon start`
- Check firewall settings for port 10000
- Verify IP address is correct (localhost vs. Jetson IP)
- Check ROS2 bridge configuration

### Package Installation Issues
- Open **Window > Package Manager**
- Click **Refresh** to update packages
- If ROS-TCP-Connector fails, try:
  - Remove from manifest.json
  - Re-add via Package Manager > Add package from Git URL
  - URL: `https://github.com/Unity-Technologies/ROS-TCP-Connector.git?path=/com.unity.robotics.ros-tcp-connector`

## Additional Resources

- [Unity Cloud Build Documentation](https://docs.unity3d.com/Manual/UnityCloudBuild.html)
- [ROS-TCP-Connector GitHub](https://github.com/Unity-Technologies/ROS-TCP-Connector)
- [Unity Services Documentation](https://docs.unity3d.com/Manual/UnityServices.html)
- [NAVA-AI README](../README.md)

## Project Structure

```
nava-ai/
├── Assets/
│   └── Scripts/
│       └── ROS2DashboardManager.cs  # Main ROS2 integration
├── ProjectSettings/
│   ├── ProjectSettings.asset        # Cloud project ID configured
│   └── UnityConnectSettings.asset   # Unity Services settings
├── Packages/
│   └── manifest.json                 # Package dependencies
└── ros2_scripts/                     # ROS2 Python nodes
    └── mock_jetson_node.py          # Mock node for testing
```

## Next Steps

1. ✅ Project settings configured
2. ⏳ Open in Unity Editor and verify connection
3. ⏳ Enable Unity Cloud Build service
4. ⏳ Configure build targets
5. ⏳ Test ROS2 connection
6. ⏳ Deploy to Unity Cloud or build locally

---

**Last Updated**: January 15, 2026
**Project Version**: Unity 2022.3.9f1
