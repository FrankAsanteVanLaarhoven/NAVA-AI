# 🚀 Quick Start: Run NAVA-AI Dashboard UI

## ✅ Current Status
- ✅ Unity Editor is **RUNNING**
- ✅ Project is loading
- ✅ Unity Cloud configured
- ✅ ROS2 packages installed

## 🎯 3-Step Setup (Once Unity Loads)

### Step 1: Auto-Setup Scene (30 seconds)
1. Wait for Unity Editor to fully load (watch the bottom progress bar)
2. In Unity menu bar, click: **NAVA-AI Dashboard > Setup ROS2 Scene**
3. Click the **"Auto-Setup Complete Scene"** button
4. ✅ Scene is now ready!

### Step 2: Press Play ▶️
1. Click the **Play** button at the top of Unity Editor
2. The dashboard UI will appear
3. You'll see the robot visualization and UI elements

### Step 3: Connect ROS2 (Optional - for full functionality)
```bash
# Terminal 1: Start ROS2
ros2 daemon start

# Terminal 2: Run mock node
cd /Users/frankvanlaarhoven/Desktop/anything-mcp/NAVA-AI/ros2_scripts
python3 mock_jetson_node.py
```

## 📱 What You'll See

When you press Play:
- **3D Scene**: Ground plane with RealRobot (blue cube)
- **UI Dashboard**: 
  - Speed display
  - Safety margin (color-coded)
  - Status text
  - Toggle Shadow Mode button
  - Connection indicator

## 🎮 Controls

- **Toggle Shadow Mode**: Click the button in UI
- **Watch Robot Move**: RealRobot moves based on ROS2 messages
- **Monitor Safety**: Green = safe, Red = warning

## ⚡ Fast Track (If Scene Already Exists)

If the scene is already set up:
1. Just press **Play** ▶️
2. That's it!

## 🔧 Troubleshooting

**Menu not appearing?**
- Wait for Unity to fully compile scripts (check bottom right)
- Try: Assets > Reimport All

**No UI showing?**
- Check Hierarchy for "Canvas" GameObject
- Verify Canvas is enabled

**ROS2 connection error?**
- Normal if ROS2 isn't running
- UI will still show, just won't receive data

---

**Ready to go!** Unity is running - just wait for it to load, then follow Step 1 above.
