# Unity Scene Setup Guide - ROS2 Dashboard

This guide will help you set up the Unity scene for the ROS2 Dashboard. You can either use the automated setup tool or follow the manual steps.

## Quick Setup (Automated)

1. Open Unity and load your scene
2. Go to menu: **NAVA Dashboard → Setup ROS2 Scene**
3. Click **"Auto-Setup Complete Scene"**
4. Done! All objects are created and wired up.

## Manual Setup (Step-by-Step)

### Phase 1: Setup Ground

1. Right-click in **Hierarchy** → **3D Object** → **Plane**
2. Name it `Ground`
3. In **Inspector**, set:
   - **Position**: (0, 0, 0)
   - **Scale**: (10, 1, 10)

### Phase 2: Create Robots

#### Real Robot
1. Right-click in **Hierarchy** → **3D Object** → **Cube**
2. Name it `RealRobot`
3. In **Inspector**, set:
   - **Position**: (0, 0.5, 0)
   - **Scale**: (1, 1, 1)
4. (Optional) Create a blue material:
   - **Project** window → Right-click → **Create** → **Material**
   - Name it `RealRobotMaterial`
   - Set **Albedo** color to blue
   - Drag onto `RealRobot`

#### Shadow Robot
1. Select `RealRobot` in Hierarchy
2. Press **Ctrl+D** (Windows) or **Cmd+D** (Mac) to duplicate
3. Rename to `ShadowRobot`
4. In **Inspector**, set:
   - **Position**: (-2, 0.5, 0)
5. Create wireframe material:
   - **Project** window → Right-click → **Create** → **Material**
   - Name it `ShadowRobotMaterial`
   - **Shader**: Unlit/Transparent
   - **Color**: Purple (R:255, G:0, B:255, A:128)
   - **Render Face**: Front
   - Drag onto `ShadowRobot`
6. **Disable** the GameObject: Uncheck the checkbox next to `ShadowRobot` in Inspector

### Phase 3: Create Dashboard UI

#### Canvas Setup
1. Right-click in **Hierarchy** → **UI** → **Canvas**
2. Canvas settings should be:
   - **Render Mode**: Screen Space - Overlay
   - **Canvas Scaler**: Scale With Screen Size

#### Background Panel
1. Right-click `Canvas` → **UI** → **Panel**
2. Name it `DashboardPanel`
3. In **Rect Transform**:
   - Click the anchor preset (top-left square)
   - Hold **Shift + Alt** and click **Stretch-Stretch**
   - This makes it fill the screen

#### Velocity Text
1. Right-click `DashboardPanel` → **UI** → **Text**
2. Name it `VelocityText`
3. In **Rect Transform**:
   - **Anchor**: Top-Left
   - **Position**: X: 10, Y: -30
   - **Width**: 200, **Height**: 30
4. In **Text** component:
   - **Text**: "Speed: 0.0 m/s"
   - **Font Size**: 14
   - **Color**: White

#### Margin Text
1. Right-click `DashboardPanel` → **UI** → **Text**
2. Name it `MarginText`
3. In **Rect Transform**:
   - **Anchor**: Top-Left
   - **Position**: X: 10, Y: -70
   - **Width**: 250, **Height**: 30
4. In **Text** component:
   - **Text**: "Safety Margin: 2.0 m"
   - **Font Size**: 14
   - **Color**: Green

#### Status Text
1. Right-click `DashboardPanel` → **UI** → **Text**
2. Name it `StatusText`
3. In **Rect Transform**:
   - **Anchor**: Top-Right
   - **Position**: X: -10, Y: -30
   - **Width**: 250, **Height**: 30
4. In **Text** component:
   - **Text**: "MODE: STANDARD"
   - **Font Size**: 14
   - **Color**: White

#### Connection Indicator Image
1. Right-click `DashboardPanel` → **UI** → **Image**
2. Name it `ConnectionIndicator`
3. In **Rect Transform**:
   - **Anchor**: Top-Right
   - **Position**: X: -270, Y: -30
   - **Width**: 20, **Height**: 20
4. In **Image** component:
   - **Color**: Green

#### Toggle Button
1. Right-click `DashboardPanel` → **UI** → **Button**
2. Name it `ToggleShadowBtn`
3. In **Rect Transform**:
   - **Anchor**: Bottom-Center
   - **Position**: X: 0, Y: 50
   - **Width**: 200, **Height**: 50
4. In **Button** component:
   - **Normal Color**: Light blue
5. Select the child `Text` object:
   - **Text**: "TOGGLE SHADOW MODE"
   - **Font Size**: 14
   - **Font Style**: Bold
   - **Alignment**: Center

### Phase 4: Attach Scripts

1. Right-click in **Hierarchy** → **Create Empty**
2. Name it `ROS_Manager`
3. In **Inspector**, click **Add Component**
4. Search for `ROS2DashboardManager` and add it

### Phase 5: Configure Script

Select `ROS_Manager` in Hierarchy. In the **Inspector**, configure `ROS2DashboardManager`:

1. **Connection Settings**:
   - **Ros IP**: `127.0.0.1` (or Jetson IP when deployed)
   - **Ros Port**: `10000`

2. **Scene References**:
   - **Real Robot**: Drag `RealRobot` from Hierarchy
   - **Shadow Robot**: Drag `ShadowRobot` from Hierarchy

3. **UI References**:
   - **Velocity Text**: Drag `VelocityText` from Hierarchy
   - **Margin Text**: Drag `MarginText` from Hierarchy
   - **Status Text**: Drag `StatusText` from Hierarchy
   - **Connection Indicator**: Drag `ConnectionIndicator` from Hierarchy

### Phase 6: Wire Up Button

1. Select `ToggleShadowBtn` in Hierarchy
2. In **Inspector**, find the **Button** component
3. Scroll down to **On Click ()** section
4. Click the **+** button to add an event
5. Drag `ROS_Manager` from Hierarchy into the object slot
6. In the dropdown, select: **ROS2DashboardManager → RequestToggleShadow()**

## Phase 7: Test Setup

### Start ROS2 Node

1. Open terminal
2. Navigate to project root:
   ```bash
   cd /Users/frankvanlaarhoven/Desktop/NAVA_Dashboard
   ```
3. Source ROS2 (adjust for your distro):
   ```bash
   source /opt/ros/humble/setup.bash
   ```
4. Run the mock node:
   ```bash
   python3 ros2_scripts/mock_jetson_node.py
   ```

### Test in Unity

1. Press **Play** button in Unity
2. Check **Console** for: `[Unity] Attempting to connect to ROS at 127.0.0.1:10000`
3. Watch for:
   - `RealRobot` moving back and forth
   - `VelocityText` updating
   - `MarginText` changing color (green/red)
   - `ConnectionIndicator` turning green
4. Click **"TOGGLE SHADOW MODE"** button
5. `ShadowRobot` should appear (purple wireframe)
6. `StatusText` should change to "SHADOW MODE: ACTIVE"
7. `ConnectionIndicator` should turn magenta

## Troubleshooting

### Robot Not Moving
- Check Console for ROS connection errors
- Verify Python node is running
- Check `rosIP` and `rosPort` in ROS_Manager

### UI Not Updating
- Verify all UI references are assigned in ROS_Manager
- Check that UI elements are children of Canvas
- Ensure Text components are not disabled

### Button Not Working
- Verify button is wired to `ROS_Manager.RequestToggleShadow()`
- Check Console for button click messages
- Ensure ROS connection is established

### Shadow Robot Not Appearing
- Check that `ShadowRobot` GameObject exists (even if disabled)
- Verify it's assigned in ROS_Manager
- Check Console for shadow mode toggle messages

## Deployment to Jetson (Real Hardware)

When ready to deploy to real hardware:

### Step 1: Deploy ROS2 Stack to Jetson
1. Transfer your real ROS2 navigation stack to the Jetson
2. Ensure the ROS2 topics match:
   - `nav/cmd_vel` (geometry_msgs/Twist)
   - `nav/margin` (std_msgs/Float32)
   - `nav/shadow_toggle` (std_msgs/Bool)
   - `nav/cmd/toggle_shadow` (std_msgs/Bool) - subscribed by Jetson

### Step 2: Find Jetson IP Address
On the Jetson, run:
```bash
hostname -I
```
Or check your network router's admin panel for connected devices.

### Step 3: Update Unity Connection Settings
1. **Keep Unity project as-is** - no code changes needed
2. In Unity Editor, select `ROS_Manager` GameObject in Hierarchy
3. In Inspector, find `ROS2DashboardManager` component
4. Under **Connection Settings**:
   - Change **Ros IP** from `127.0.0.1` to the Jetson's IP address (e.g., `192.168.1.50`)
   - Keep **Ros Port** as `10000` (or match your ROS2 bridge configuration)
5. **Press Play**

### Step 4: Verify Connection
- Check Unity Console for: `[Unity] Attempting to connect to ROS at [Jetson IP]:10000`
- Watch for real-time data updates:
  - Robot velocity from actual hardware
  - Safety margins from your navigation stack
  - Shadow mode toggling affecting the real robot

The Unity dashboard will now visualize **real robot data** from the Jetson in real-time!

### Quick Reference
- **Local Testing**: `rosIP = "127.0.0.1"` (localhost)
- **Real Hardware**: `rosIP = "192.168.1.50"` (or your Jetson's IP)
- **No code changes required** - just update the IP in Inspector!
