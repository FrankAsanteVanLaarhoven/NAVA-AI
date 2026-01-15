# Quick Start Guide - ROS2 Dashboard

## Automated Scene Setup (Recommended)

### Step 1: Run the Automated Setup

1. **Open Unity** and load your scene
2. **Wait for compilation** - Unity will detect the Editor script automatically
3. **Go to menu**: `NAVA Dashboard` → `Setup ROS2 Scene`
4. **Click**: "Auto-Setup Complete Scene" button
5. **Wait** - Unity will create all objects automatically

### Step 2: Verify Setup

1. **Select `ROS_Manager`** in Hierarchy
2. **Check Inspector** - All slots should be filled:
   - ✅ Real Robot
   - ✅ Shadow Robot  
   - ✅ Velocity Text
   - ✅ Margin Text
   - ✅ Status Text
   - ✅ Connection Indicator

If any slot is empty, the automation found existing objects with different names. Just drag the correct object from Hierarchy to the slot.

### Step 3: Test with Mock Node

1. **Open Terminal** and navigate to project:
   ```bash
   cd /Users/frankvanlaarhoven/Desktop/NAVA_Dashboard
   ```

2. **Source ROS2** (adjust for your distro):
   ```bash
   source /opt/ros/humble/setup.bash
   ```

3. **Run Mock Node**:
   ```bash
   python3 ros2_scripts/mock_jetson_node.py
   ```

4. **Press Play in Unity**

### Step 4: Verify Everything Works

You should see:
- ✅ **Velocity Text**: Oscillating values (sine wave)
- ✅ **Margin Text**: Changing between Green (2.0m) and Red (0.2m)
- ✅ **Real Robot**: Moving back and forth
- ✅ **Connection Indicator**: Green dot
- ✅ **Button Click**: Toggles shadow mode
  - Status changes to "SHADOW MODE: ACTIVE"
  - Indicator turns Magenta
  - Shadow Robot appears (purple wireframe)
  - Margin drops to 0.2m

## What Gets Created

The automated setup creates:

### 3D Objects
- **Ground**: Plane (10x10 scale)
- **RealRobot**: Blue cube at origin
- **ShadowRobot**: Purple wireframe cube (initially disabled)

### UI Elements
- **Canvas**: Screen overlay
- **DashboardPanel**: Background panel
- **VelocityText**: Top-left, shows speed
- **MarginText**: Below velocity, shows safety margin
- **StatusText**: Top-right, shows mode
- **ConnectionIndicator**: Green dot next to status
- **ToggleShadowBtn**: Bottom-center button

### Scripts
- **ROS_Manager**: GameObject with ROS2DashboardManager script
- All references automatically assigned
- Button wired to toggle function

## Troubleshooting

### Script Not Appearing in Menu
- **Check**: Script is in `Assets/Scripts/Editor/` folder
- **Check**: Unity has finished compiling (spinner in bottom-right)
- **Try**: Right-click in Project → Reimport All

### Objects Not Created
- **Check**: Console for error messages
- **Check**: You're in a scene (not just Project view)
- **Try**: Run setup again (it's safe - won't duplicate)

### References Not Assigned
- **Check**: All objects were created successfully
- **Manual fix**: Drag objects from Hierarchy to Inspector slots
- **Check**: Object names match exactly (case-sensitive)

### Button Not Working
- **Check**: Button is wired in Inspector
- **Check**: ROS connection is established
- **Check**: Console for button click messages

## Next Steps

Once local testing works:

1. **Deploy ROS2 stack** to Jetson
2. **Find Jetson IP**: `hostname -I` on Jetson
3. **In Unity**: Select `ROS_Manager` → Change `Ros IP` to Jetson IP
4. **Press Play** - Real hardware visualization!

See `DEPLOYMENT.md` for detailed deployment instructions.
