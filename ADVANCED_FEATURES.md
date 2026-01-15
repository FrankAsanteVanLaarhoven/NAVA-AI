# Advanced Features Guide - ROS2 Dashboard

This guide covers the four advanced features that transform the dashboard from a basic visualizer into a professional robotics tool.

## Overview

The dashboard includes four advanced features:

1. **Teleoperation** - Drive the robot with WASD/Gamepad
2. **Live Camera Feed** - See what the robot sees
3. **SLAM Map Visualization** - View the robot's understanding of the environment
4. **Trajectory Replay** - Analyze past runs from CSV logs

## 1. Teleoperation (UnityTeleopController)

### Purpose
Drive the robot manually using keyboard or gamepad. Essential for:
- Emergency stops when AI fails
- Hardware testing and calibration
- Manual navigation during debugging

### Setup

1. **Script is automatically added** to `ROS_Manager` by the scene setup tool
2. **No additional configuration needed** - works out of the box

### Usage

- **W/S or Up/Down Arrow**: Move forward/backward
- **A/D or Left/Right Arrow**: Rotate left/right
- **Speed**: Controlled by `moveSpeed` and `turnSpeed` in Inspector

### Configuration

Select `ROS_Manager` → `UnityTeleopController` component:
- **Move Speed**: Forward/backward speed (m/s)
- **Turn Speed**: Rotation speed (rad/s)
- **Teleop Enabled**: Toggle teleop on/off
- **Publish Rate**: Messages per second (default: 20Hz)

### Jetson Configuration

On the Jetson, ensure your navigation stack listens to `/unity/cmd_vel`:

```python
# In your navigation node
self.cmd_vel_sub = self.create_subscription(
    Twist, 
    'unity/cmd_vel',  # Unity publishes here
    self.cmd_vel_callback, 
    10
)
```

Or use topic remapping:
```bash
ros2 run your_nav_stack nav_node --ros-args -r cmd_vel:=unity/cmd_vel
```

## 2. Live Camera Feed (CameraFeedVisualizer)

### Purpose
Display live video feed from the robot's camera. Essential for:
- Debugging perception failures
- Verifying obstacle detection
- Understanding why the robot crashed

### Setup

1. **UI Element**: `CameraDisplay` RawImage is automatically created (bottom-right)
2. **Script**: `CameraFeedVisualizer` is automatically added to `ROS_Manager`
3. **Reference**: Automatically wired in scene setup

### Manual Setup (if needed)

1. **Create UI Element**:
   - Right-click `DashboardPanel` → UI → Raw Image
   - Name: `CameraDisplay`
   - Position: Bottom-right (e.g., X: -160, Y: 100)
   - Size: 320x240 (adjust for your camera resolution)

2. **Add Script**:
   - Select `ROS_Manager`
   - Add Component → `CameraFeedVisualizer`
   - Drag `CameraDisplay` to `Display Image` slot

### Configuration

- **Display Image**: UI RawImage component
- **Camera Topic**: ROS2 topic name (default: `camera/image_raw`)
- **Expected Width/Height**: Initial texture size (auto-resizes)

### Jetson Configuration

Ensure your camera driver publishes to the correct topic:

```bash
# Check available topics
ros2 topic list | grep camera

# View camera feed
ros2 topic echo /camera/image_raw --no-arr
```

Common camera drivers:
- `v4l2_camera`: `ros2 run v4l2_camera v4l2_camera_node`
- `usb_cam`: `ros2 run usb_cam usb_cam_node_exe`

## 3. SLAM Map Visualization (MapVisualizer)

### Purpose
Display the occupancy grid map from SLAM. Provides:
- "God View" of the environment
- Verification of localization accuracy
- Visual debugging of navigation decisions

### Setup

1. **UI Element**: `MapDisplay` RawImage is automatically created (bottom-left)
2. **Script**: `MapVisualizer` is automatically added to `ROS_Manager`
3. **Reference**: Automatically wired, including dashboard manager reference

### Manual Setup (if needed)

1. **Create UI Element**:
   - Right-click `DashboardPanel` → UI → Raw Image
   - Name: `MapDisplay`
   - Position: Bottom-left (e.g., X: 160, Y: 100)
   - Size: 320x240 (auto-resizes based on map)

2. **Add Script**:
   - Select `ROS_Manager`
   - Add Component → `MapVisualizer`
   - Drag `MapDisplay` to `Map Display` slot
   - Drag `ROS_Manager` to `Dashboard Manager` slot (for shadow mode integration)

### Configuration

- **Map Display**: UI RawImage component
- **Map Topic**: ROS2 topic name (default: `map`)
- **Highlight Danger Zones**: Show red zones during shadow mode
- **Dashboard Manager**: Reference to ROS2DashboardManager (for shadow mode)

### Map Colors

- **White**: Free space (safe to navigate)
- **Black**: Occupied/Wall (obstacle)
- **Gray**: Unknown (unexplored)
- **Red**: Danger zone (when shadow mode active)

### Jetson Configuration

Ensure your SLAM stack publishes occupancy grid:

```bash
# Check map topic
ros2 topic list | grep map
ros2 topic info /map

# View map data
ros2 topic echo /map --no-arr
```

Common SLAM packages:
- `nav2`: Uses `map_server` to publish `/map`
- `cartographer`: Publishes to `/map`
- `gmapping`: Publishes to `/map`

## 4. Trajectory Replay (TrajectoryReplayer)

### Purpose
Replay past robot trajectories from CSV logs. Enables:
- Post-mortem crash analysis
- Path validation
- Scientific debugging of navigation failures

### Setup

1. **Replay Ghost**: `ReplayGhost` GameObject is automatically created (yellow semi-transparent)
2. **Script**: `TrajectoryReplayer` is automatically added to `ROS_Manager`
3. **UI Button**: `ReplayBtn` is automatically created and wired

### Manual Setup (if needed)

1. **Create Replay Ghost**:
   - Duplicate `RealRobot`
   - Name: `ReplayGhost`
   - Position: Offset from real robot (e.g., X: 2, Y: 0.5, Z: 0)
   - Material: Yellow, semi-transparent
   - Initially disabled

2. **Add Script**:
   - Select `ROS_Manager`
   - Add Component → `TrajectoryReplayer`
   - Drag `ReplayGhost` to `Replay Ghost` slot

3. **Create Replay Button**:
   - Right-click `DashboardPanel` → UI → Button
   - Name: `ReplayBtn`
   - Text: "REPLAY LAST RUN"
   - Wire to: `ROS_Manager` → `TrajectoryReplayer` → `StartReplay()`

### CSV Format

The replayer expects CSV files with trajectory data. Format:

```csv
timestamp,x,y,z,margin,velocity,uncertainty
1234567890.123,0.0,0.0,0.0,2.0,0.5,0.1
1234567890.223,0.1,0.0,0.0,2.0,0.5,0.1
...
```

Required columns:
- `timestamp`: Time in seconds (float)
- `x`, `y`, `z`: Position coordinates (float)

Optional columns (for future enhancements):
- `margin`: Safety margin
- `velocity`: Robot velocity
- `uncertainty`: Localization uncertainty

### Configuration

- **Replay Ghost**: GameObject that follows the path
- **Playback Speed**: Speed multiplier (1.0 = real-time)
- **CSV File Path**: Path to log file (relative or absolute)
- **Draw Path**: Show path as line renderer

### Usage

1. **Load Data**: Click "REPLAY LAST RUN" button (automatically loads CSV)
2. **Watch Replay**: Ghost robot follows the recorded path
3. **Analyze**: Compare ghost path with real robot behavior

### CSV File Locations

The replayer searches in this order:
1. Project root: `nava_telemetry.csv`
2. Assets parent: `../nava_telemetry.csv`
3. Persistent data: `Application.persistentDataPath/nava_telemetry.csv`
4. Absolute path (if provided)

## Integration with Existing Dashboard

All advanced features integrate seamlessly:

- **Teleop**: Publishes to `/unity/cmd_vel` (separate from navigation stack)
- **Camera**: Subscribes to `/camera/image_raw` (standard ROS topic)
- **Map**: Subscribes to `/map` (standard SLAM topic)
- **Replay**: Uses CSV logs from telemetry system

## Troubleshooting

### Teleoperation Not Working

- **Check**: Teleop is enabled in Inspector
- **Check**: Jetson is subscribed to `/unity/cmd_vel`
- **Check**: Console for publish messages
- **Check**: Network connectivity

### Camera Feed Not Showing

- **Check**: Camera topic name matches Jetson
- **Check**: Camera driver is running on Jetson
- **Check**: Image encoding is supported (rgb8, bgr8)
- **Check**: Console for image processing errors

### Map Not Displaying

- **Check**: SLAM is running and publishing `/map`
- **Check**: Map topic name matches
- **Check**: Occupancy grid format is correct
- **Check**: Console for map initialization messages

### Replay Not Working

- **Check**: CSV file exists and is readable
- **Check**: CSV format matches expected structure
- **Check**: Replay Ghost is assigned
- **Check**: Console for CSV parsing errors

## Best Practices

1. **Teleop**: Use for testing, disable during autonomous operation
2. **Camera**: Monitor during critical navigation phases
3. **Map**: Verify localization before long autonomous runs
4. **Replay**: Always save logs after test runs for analysis

## Summary

These four features transform your dashboard into a comprehensive robotics tool:

- ✅ **Teleop**: Manual intervention capability
- ✅ **Camera**: Perception verification
- ✅ **Map**: Localization validation
- ✅ **Replay**: Scientific crash analysis

All features are automatically set up by the scene setup tool - just run it and they're ready to use!
