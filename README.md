# NAVA-AI Dashboard - Professional ROS2 Unity Integration

Production-ready Unity dashboard for ROS2 robot visualization, control, and monitoring. This comprehensive system provides real-time visualization, teleoperation, perception debugging, and mission planning capabilities.

## Project Structure

- `nava-ai/` - Unity project
  - `Assets/Scripts/ROS2DashboardManager.cs` - Unity script for ROS2 communication
- `ros2_scripts/` - ROS2 Python nodes
  - `mock_jetson_node.py` - Mock Jetson node for testing

## Setup Instructions

### Prerequisites

1. **Unity** (with ROS-TCP-Connector package installed)
2. **ROS2** (Humble or later recommended)
3. **Python 3.8+** with rclpy

### Unity Setup

1. Open the Unity project in `nava-ai/`
2. Ensure the ROS-TCP-Connector package is installed via Package Manager
3. Add the `ROS2DashboardManager.cs` script to a GameObject in your scene
4. Configure the script in the Inspector:
   - Set `rosIP` (default: 127.0.0.1)
   - Set `rosPort` (default: 10000)
   - Assign `realRobot` and `shadowRobot` GameObjects
   - Assign UI Text and Image components

### ROS2 Setup

1. Make the Python script executable:
   ```bash
   chmod +x ros2_scripts/mock_jetson_node.py
   ```

2. Source your ROS2 workspace:
   ```bash
   source /opt/ros/humble/setup.bash  # Adjust for your ROS2 distro
   ```

3. Run the mock Jetson node:
   ```bash
   python3 ros2_scripts/mock_jetson_node.py
   ```

### Running the System

1. Start ROS2 daemon (if not already running):
   ```bash
   ros2 daemon start
   ```

2. Launch the mock Jetson node:
   ```bash
   python3 ros2_scripts/mock_jetson_node.py
   ```

3. Start Unity and play the scene

## ROS2 Topics

### Published Topics (from Jetson/Mock Node to Unity)
- `nav/cmd_vel` (geometry_msgs/Twist) - Robot velocity commands
- `nav/margin` (std_msgs/Float32) - Safety margin distance
- `nav/shadow_toggle` (std_msgs/Bool) - Shadow mode status

### Subscribed Topics (from Unity to Jetson/Mock Node)
- `nav/cmd/toggle_shadow` (std_msgs/Bool) - Request to toggle shadow mode

## Complete Feature Set

### Core Features
- Real-time robot visualization
- Safety margin monitoring with color-coded UI
- Shadow mode toggle functionality
- Bidirectional ROS2 communication

### Advanced Visualization
- **3D LiDAR Point Cloud** - Volumetric perception visualization
- **Live Camera Feed** - Real-time camera stream from robot
- **SLAM Map Display** - Occupancy grid visualization
- **Sonar/Radar Visualization** - Audio-based sensor visualization

### Control & Planning
- **Teleoperation** - WASD/Gamepad manual control
- **Mission Planner** - Task queue and waypoint navigation
- **Trajectory Replay** - CSV log playback for analysis

### Safety & Monitoring
- **Geofencing** - Dynamic no-go zone editor
- **Battery Monitor** - Hardware health monitoring
- **Gripper Visualization** - End-effector state display

### Development Tools
- **World Editor** - Scenario builder for reproducible testing
- **Automated Scene Setup** - One-click complete dashboard setup

All features are production-ready and fully integrated.

## Deployment to Real Hardware (Jetson)

When deploying to real hardware:

1. **Keep the Unity project exactly as is** - no code changes needed
2. **Deploy your real ROS2 navigation stack** to the Jetson
3. **In Unity Editor**:
   - Select `ROS_Manager` GameObject in Hierarchy
   - In Inspector, find `ROS2DashboardManager` component
   - Change **Ros IP** from `127.0.0.1` to the Jetson's IP address (e.g., `192.168.1.50`)
   - Keep **Ros Port** as `10000` (or match your ROS2 bridge configuration)
4. **Press Play** - Unity will automatically visualize real hardware data

The dashboard will now:
- Display real robot velocity from the Jetson
- Show actual safety margins from your navigation stack
- Allow shadow mode toggling that affects the real robot
- Update in real-time as the robot moves

### Finding Jetson IP Address

On the Jetson, run:
```bash
hostname -I
```

Or check your router's admin panel for connected devices.

## Notes

- The mock node simulates robot motion with a sine wave pattern
- Shadow mode reduces the safety margin to simulate near-miss scenarios
- Unity connects to ROS2 via TCP on port 10000 (configurable)
- **For local testing**: Use `127.0.0.1` (localhost)
- **For real hardware**: Use Jetson's IP address (e.g., `192.168.1.50`)