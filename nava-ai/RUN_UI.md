# Running the NAVA-AI Dashboard UI in Unity Editor

## ✅ Unity Editor Status

Unity Editor is currently **RUNNING** and loading the project.

## 🚀 Quick Setup Steps

### Step 1: Wait for Unity to Load
- Unity Editor is starting up (this may take 1-2 minutes)
- Wait for the project to fully load in Unity Editor

### Step 2: Auto-Setup the Scene (Recommended)

Once Unity Editor is fully loaded:

1. **Open the Scene Setup Helper**:
   - In Unity Editor menu bar, click: **NAVA-AI Dashboard > Setup ROS2 Scene**
   - This will open a window with setup options

2. **Click "Auto-Setup Complete Scene"**:
   - This automatically creates:
     - ✅ Ground plane
     - ✅ RealRobot and ShadowRobot GameObjects
     - ✅ UI Canvas with all dashboard elements
     - ✅ ROS_Manager GameObject with ROS2DashboardManager script
     - ✅ All UI text elements (velocity, margin, status)
     - ✅ Toggle Shadow Mode button
     - ✅ Connection indicator

### Step 3: Verify Setup

After auto-setup, check:
- ✅ Scene Hierarchy shows: `ROS_Manager`, `RealRobot`, `ShadowRobot`, `Canvas`
- ✅ ROS_Manager has `ROS2DashboardManager` component attached
- ✅ All UI references are assigned in the Inspector

### Step 4: Configure ROS2 Connection

1. Select `ROS_Manager` in Hierarchy
2. In Inspector, find `ROS2DashboardManager` component
3. Configure:
   - **Ros IP**: `127.0.0.1` (for local testing)
   - **Ros Port**: `10000`
   - Verify all GameObject and UI references are assigned

### Step 5: Press Play! ▶️

Click the **Play** button in Unity Editor to start the dashboard.

## 🧪 Testing with Mock ROS2 Node

To test the UI without real hardware:

```bash
# Terminal 1: Start ROS2 daemon
ros2 daemon start

# Terminal 2: Run mock Jetson node
cd /Users/frankvanlaarhoven/Desktop/anything-mcp/NAVA-AI/ros2_scripts
python3 mock_jetson_node.py
```

Then press **Play** in Unity Editor.

## 📋 Manual Setup (If Auto-Setup Doesn't Work)

If you need to set up manually:

1. **Create Ground Plane**:
   - GameObject > 3D Object > Plane
   - Scale: (10, 1, 10)

2. **Create Robots**:
   - GameObject > 3D Object > Cube (name: "RealRobot")
   - GameObject > 3D Object > Cube (name: "ShadowRobot")
   - Position RealRobot at (0, 0.5, 0)
   - Position ShadowRobot at (0, 0.5, 0)

3. **Create UI Canvas**:
   - GameObject > UI > Canvas
   - Add UI elements:
     - UI > Text (for velocity display)
     - UI > Text (for margin display)
     - UI > Text (for status)
     - UI > Image (for connection indicator)
     - UI > Button (for toggle shadow mode)

4. **Create ROS Manager**:
   - GameObject > Create Empty (name: "ROS_Manager")
   - Add Component > ROS2DashboardManager
   - Assign all references in Inspector

## 🎮 UI Controls

Once running:
- **Toggle Shadow Mode**: Click the button in the UI
- **View Robot Motion**: Watch RealRobot move based on ROS2 messages
- **Monitor Safety Margin**: See color-coded margin display (green/red)
- **Connection Status**: Check connection indicator color

## 🔧 Troubleshooting

### Unity Editor Not Responding
- Wait 2-3 minutes for full project load
- Check Unity Console for errors

### Scene Setup Menu Not Appearing
- Ensure project is fully loaded
- Check that `Assets/Scripts/Editor/SceneSetupHelper.cs` exists
- Try: Assets > Reimport All

### ROS2 Connection Failed
- Verify ROS2 daemon is running: `ros2 daemon start`
- Check IP address (127.0.0.1 for local)
- Verify port 10000 is not blocked by firewall
- Check Unity Console for connection errors

### UI Elements Not Showing
- Verify Canvas is set to "Screen Space - Overlay"
- Check Canvas Scaler settings
- Ensure UI elements are children of Canvas

## 📊 Expected UI Layout

```
┌─────────────────────────────────────┐
│  NAVA-AI Dashboard                  │
├─────────────────────────────────────┤
│  Speed: X.XX m/s                    │
│  Safety Margin: X.XX m              │
│  Status: MODE: STANDARD             │
│  [TOGGLE SHADOW MODE]               │
│  ● (Connection Indicator)           │
└─────────────────────────────────────┘
```

## 🎯 Next Steps

1. ✅ Unity Editor is running
2. ⏳ Wait for project to load
3. ⏳ Run auto-setup via menu
4. ⏳ Press Play to test UI
5. ⏳ Connect to ROS2 (mock or real hardware)

---

**Status**: Unity Editor is running - Ready for setup!
