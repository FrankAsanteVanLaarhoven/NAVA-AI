# Global-Standard Accessibility Platform

The NAVA-AI Dashboard implements a **Global-Standard, Accessibility-Ready** platform with robust fallbacks and semantic capabilities for denied environments.

## Overview

This update adds **5 Critical Modules** for operation in GPS-denied, signal-weak, and accessibility-critical scenarios:

1. **RWiFi SLAM Bridge** - Signal-strength based mapping
2. **GPS-Denied Fallback** - Dead Reckoning from IMU+Odom
3. **3D Bounding Box Projector** - Projecting 2D VLM vision into 3D Unity
4. **"Dola-Style" Semantic Voice** - Context-aware command parsing
5. **Syncnomics SOP** - Timeline synchronization

## Components

### 1. RWiFi SLAM Manager (`RwifiSlamManager.cs`)

**Purpose**: Signal-strength based mapping for GPS-denied environments.

**Features**:
- RSSI (Received Signal Strength Indicator) monitoring
- Dynamic map quality assessment
- Grid node visualization
- Trajectory history tracking
- Automatic map stability detection

**Signal Quality Levels**:
- **Green** (> -60 dBm): Good signal, map stable
- **Yellow** (-60 to -80 dBm): Weak signal, map degrading
- **Red** (< -80 dBm): Critical signal, map unreliable

**Use Cases**:
- Indoor navigation (no GPS)
- GPS jamming scenarios
- Signal-degraded environments
- Backup mapping system

### 2. GPS-Denied PNT Manager (`GpsDeniedPntManager.cs`)

**Purpose**: Dead Reckoning fallback when GPS is jammed/denied.

**Features**:
- Automatic GPS health monitoring
- Dead Reckoning (DR) position estimation
- IMU + Odometry fusion
- Mode switching (GPS → DR → Fused)
- Visual status indicators

**PNT Modes**:
- **GPS**: GPS lock active (green light)
- **DR**: Dead Reckoning (yellow light, drift accumulation)
- **Fused**: GPS + DR fusion (cyan, best accuracy)

**Fallback Triggers**:
- GPS fix quality < threshold
- Position covariance > threshold
- GPS signal lost

**Dead Reckoning**:
- Integrates IMU acceleration → velocity → position
- Uses wheel encoder odometry
- Accumulates drift over time
- Visual drift indicator

### 3. 3D Bounding Box Projector (`BBox3dProjector.cs`)

**Purpose**: Projects 2D VLM detections into 3D Unity world space.

**Features**:
- Raycast-based depth estimation
- 3D bounding box visualization
- Confidence-based color coding
- 3D text labels
- Distance-based scaling

**Projection Process**:
1. Receive 2D detection (UV coordinates)
2. Raycast from camera to find depth
3. Create 3D bounding box at hit point
4. Scale based on distance and detection size
5. Color code by confidence
6. Display label with confidence

**Visualization**:
- **Wireframe boxes**: Yellow (default), Green (high confidence), Red (low confidence)
- **3D Labels**: Object name + confidence percentage
- **Distance scaling**: Closer objects = larger boxes

**Use Cases**:
- VLM object detection visualization
- Debugging AI perception
- Understanding what robot "sees"
- Training data validation

### 4. Semantic Voice Commander (`SemanticVoiceCommander.cs`)

**Purpose**: "Dola-Style" semantic voice parsing for context-aware commands.

**Features**:
- Natural language command parsing
- Semantic intent extraction
- Target object recognition
- Command history
- Audio feedback
- ROS2 integration

**Semantic Actions**:
- **Goto**: Navigate to target ("Go to the red chair")
- **Stop**: Emergency stop ("Stop")
- **Avoid**: Avoid obstacle ("Avoid the wall")
- **Explain**: Trigger reasoning ("Explain why")
- **Toggle**: Toggle mode ("Toggle mode")
- **Follow**: Follow target ("Follow person")
- **Search**: Search for object ("Search for box")

**Parsing Process**:
1. Receive voice command
2. Extract semantic intent (action)
3. Extract target object (if applicable)
4. Execute action with context
5. Log to HUD with color coding
6. Publish intent to ROS

**Target Extraction**:
- Color + Object: "red chair", "blue box"
- Object alone: "chair", "wall", "person"
- Context-aware: Understands spatial relationships

### 5. Syncnomics SOP (`SyncnomicsSop.cs`)

**Purpose**: Timeline synchronization between Unity (Sim) and ROS (Real).

**Features**:
- Real-time clock offset monitoring
- Automatic calibration
- Sub-millisecond precision
- Sync status visualization
- ROS clock subscription

**Synchronization**:
- Monitors Unity time vs ROS time
- Calculates sync error (drift)
- Auto-calibrates if error > threshold
- Maintains < 50ms alignment

**Status Indicators**:
- **Green**: Synced (< 50ms error)
- **Red**: Sync error (> 50ms drift)

**Use Cases**:
- Hardware-in-the-loop testing
- Real-time simulation
- Data logging synchronization
- Multi-system coordination

## Accessibility Features

### High Contrast Mode
- Toggle button: `Accessibility/HighContrast`
- Black background + White/Cyan UI
- Enhanced visibility for low vision

### Screen Reader Support
- All UI elements have descriptive names
- Compatible with macOS VoiceOver / Windows Narrator
- Semantic labels for all controls

### Voice Feedback
- Audio acknowledgment for commands
- Error sounds for failures
- Status announcements

## Integration

All accessibility components are:
- ✅ Automatically added during scene setup
- ✅ Fully integrated with existing systems
- ✅ Production-ready with error handling
- ✅ ROS2 integrated
- ✅ Performance optimized

## ROS2 Topics

### RWiFi SLAM
- `/rwifi/signal_strength` (std_msgs/Float32) - RSSI values
- `/slam/pose` (geometry_msgs/PoseStamped) - SLAM pose

### GPS-Denied PNT
- `/gps/fix` (sensor_msgs/NavSatFix) - GPS status
- `/imu/data` (sensor_msgs/Imu) - IMU data
- `/odom` (nav_msgs/Odometry) - Odometry data

### 3D Bounding Box
- `/vlm/detections` (std_msgs/String) - Detection data

### Semantic Voice
- `/voice/command` (std_msgs/String) - Voice commands
- `/voice/intent` (std_msgs/String) - Semantic intent

### Syncnomics
- `/clock` (std_msgs/Time) - ROS clock
- `/sync/request` (std_msgs/Bool) - Sync requests

## Test Cases

### Test Case 1: GPS Denied
- GPS signal lost → Automatic switch to DR
- Yellow light indicates DR mode
- Position estimate from IMU+Odom
- **Result**: System continues operating

### Test Case 2: Weak WiFi Signal
- RSSI drops below -60 dBm
- Map quality degrades (yellow)
- Grid nodes become sparse
- **Result**: System warns but continues

### Test Case 3: VLM Detection
- Press 'B' key → Test detection
- 3D bounding box appears
- Label shows "Pedestrian (90%)"
- **Result**: 2D detection projected to 3D

### Test Case 4: Voice Command
- Press 'V' key → "go to the red chair"
- System extracts intent: Goto
- System extracts target: "red chair"
- **Result**: Navigation command executed

### Test Case 5: Clock Drift
- Simulate time drift
- Sync error > 50ms
- Auto-calibration triggered
- **Result**: Clocks re-synchronized

## Production Ready ✅

The Global-Standard Accessibility Platform is:
- ✅ Fully integrated
- ✅ Production-ready
- ✅ Robust fallbacks
- ✅ Accessibility compliant
- ✅ Ready for denied environments

**The NAVA-AI Dashboard with Accessibility Platform functions in GPS-denied, signal-weak, and accessibility-critical scenarios!**
