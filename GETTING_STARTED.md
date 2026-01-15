# Getting Started - NAVA-AI Dashboard

Welcome to the NAVA-AI Dashboard! This is a production-ready Unity dashboard for ROS2 robot visualization, control, and monitoring.

## Quick Start (5 Minutes)

### Step 1: Open Unity Project
1. Open Unity Hub
2. Open project: `nava-ai/`
3. Wait for compilation to complete

### Step 2: Automated Scene Setup
1. In Unity menu: **NAVA-AI Dashboard** → **Setup ROS2 Scene**
2. Click **"Auto-Setup Complete Scene"** button
3. Wait for setup to complete (all objects created automatically)

### Step 3: Test Locally
1. Open terminal
2. Navigate to project: `cd /path/to/NAVA_Dashboard`
3. Source ROS2: `source /opt/ros/humble/setup.bash`
4. Run mock node: `python3 ros2_scripts/mock_jetson_node.py`
5. In Unity: Press **Play**
6. You should see:
   - Robot moving
   - UI updating
   - All features active

## What Gets Created Automatically

The automated setup creates:

### 3D Scene
- Ground plane
- RealRobot (blue cube)
- ShadowRobot (purple wireframe)
- ReplayGhost (yellow, for trajectory replay)

### UI Dashboard
- Velocity text (top-left)
- Margin text (below velocity)
- Status text (top-right)
- Connection indicator (green dot)
- Toggle Shadow Mode button (bottom-center)
- Camera Display (bottom-right, 320x240)
- Map Display (bottom-left, 320x240)
- Battery Panel (top-right, with slider and voltage)
- Mission Panel (left side, for task queue)
- Replay Button (bottom-center)

### Scripts (All Auto-Added to ROS_Manager)
- ROS2DashboardManager (main handler)
- UnityTeleopController (WASD control)
- CameraFeedVisualizer (camera stream)
- MapVisualizer (SLAM map)
- TrajectoryReplayer (CSV replay)
- LiDARVisualizer (point cloud)
- GeofenceEditor (no-go zones)
- BatteryMonitor (health monitoring)
- GripperVisualizer (end-effector)
- MissionPlannerUI (task queue)
- SonarVisualizer (sonar rays)

## All Features Ready

All 10+ features are automatically:
- ✅ Created
- ✅ Wired up
- ✅ References assigned
- ✅ Ready to use

## Deploy to Real Hardware

1. Deploy ROS2 stack to Jetson/robot
2. In Unity: Select `ROS_Manager`
3. Change `Ros IP` from `127.0.0.1` to robot IP
4. Press Play

That's it! No code changes needed.

## Documentation

- **README.md** - Project overview
- **QUICK_START.md** - Quick reference
- **UNITY_SETUP_GUIDE.md** - Detailed setup
- **ADVANCED_FEATURES.md** - Feature details
- **DEPLOYMENT.md** - Hardware deployment
- **PRODUCTION_CHECKLIST.md** - Testing checklist

## Support

All scripts include:
- Error handling
- Debug logging
- Performance optimizations
- Tooltips in Inspector

Check Unity Console for detailed logs.

## Production Ready ✅

The NAVA-AI Dashboard is fully integrated, tested, and ready for professional use!
