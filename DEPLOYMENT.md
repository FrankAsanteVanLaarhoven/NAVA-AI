# Deployment Guide - NAVA Dashboard to Real Hardware

This guide explains how to deploy the NAVA-AI Dashboard to work with real hardware.

## Quick Deployment Steps

1. **Deploy your ROS2 navigation stack** to the Jetson
2. **In Unity Inspector**: Change `ROS_Manager` → `Ros IP` from `127.0.0.1` to Jetson IP (e.g., `192.168.1.50`)
3. **Press Play** - Dashboard automatically visualizes real hardware data

That's it! No code changes needed.

## Detailed Instructions

### Prerequisites

- Unity project set up and tested locally
- ROS2 navigation stack running on Jetson
- Jetson and Unity laptop on the same network
- ROS2 topics matching the expected names (see below)

### Step 1: Verify ROS2 Topics on Jetson

Your Jetson ROS2 stack should publish/subscribe to these topics:

**Published by Jetson (received by Unity):**
- `nav/cmd_vel` (geometry_msgs/Twist) - Robot velocity commands
- `nav/margin` (std_msgs/Float32) - Safety margin distance  
- `nav/shadow_toggle` (std_msgs/Bool) - Shadow mode status

**Subscribed by Jetson (sent from Unity):**
- `nav/cmd/toggle_shadow` (std_msgs/Bool) - Shadow mode toggle request

### Step 2: Find Jetson IP Address

**On the Jetson**, run:
```bash
hostname -I
```

This will output the IP address (e.g., `192.168.1.50`).

**Alternative methods:**
- Check your router's admin panel for connected devices
- Use `ifconfig` or `ip addr show` on the Jetson
- Ping from your laptop: `ping jetson-hostname.local` (if mDNS is enabled)

### Step 3: Update Unity Connection Settings

1. Open Unity Editor
2. Load your scene
3. Select `ROS_Manager` GameObject in Hierarchy
4. In Inspector, find the `ROS2DashboardManager` component
5. Under **Connection Settings**:
   - **Ros IP**: Change from `127.0.0.1` to your Jetson's IP (e.g., `192.168.1.50`)
   - **Ros Port**: Keep as `10000` (or match your ROS2 bridge configuration)

### Step 4: Verify Network Connectivity

**From your laptop**, test connectivity:
```bash
ping 192.168.1.50  # Replace with your Jetson IP
```

If ping fails, check:
- Both devices on same network
- Firewall settings
- Jetson network configuration

### Step 5: Start ROS2 Stack on Jetson

On the Jetson, start your navigation stack:
```bash
# Example - adjust for your launch files
ros2 launch your_nav_stack navigation.launch.py
```

Verify topics are publishing:
```bash
ros2 topic list
ros2 topic echo /nav/cmd_vel
```

### Step 6: Run Unity Dashboard

1. In Unity, press **Play**
2. Check Console for connection message:
   ```
   [Unity] Attempting to connect to ROS at 192.168.1.50:10000
   ```
3. Watch for real-time updates:
   - Robot visualization moving based on real velocity
   - Safety margins updating from actual sensors
   - UI indicators reflecting real robot state

## Troubleshooting

### Connection Issues

**Problem**: Unity can't connect to Jetson
- **Check**: Jetson IP address is correct
- **Check**: Both devices on same network
- **Check**: ROS2 bridge is running on Jetson
- **Check**: Firewall isn't blocking port 10000

**Problem**: No data appearing in Unity
- **Check**: ROS2 topics are publishing on Jetson (`ros2 topic list`)
- **Check**: Topic names match exactly (case-sensitive)
- **Check**: ROS2 bridge is configured correctly
- **Check**: Unity Console for error messages

### Network Issues

**Problem**: Can't ping Jetson
- Verify Jetson is powered on and connected
- Check network cables/WiFi connection
- Verify both devices on same subnet
- Try `arp -a` to see devices on network

### ROS2 Bridge Configuration

If using ROS-TCP-Connector bridge, ensure:
- Bridge is running on Jetson
- Bridge is listening on correct port (default: 10000)
- Bridge IP binding allows external connections

## Testing Checklist

Before deploying to real hardware, verify locally:

- [ ] Unity dashboard works with mock node (`127.0.0.1`)
- [ ] All UI elements update correctly
- [ ] Button toggles shadow mode
- [ ] Robot visualization moves smoothly
- [ ] Console shows connection messages

After deploying to Jetson:

- [ ] Unity connects to Jetson IP
- [ ] Real robot data appears in dashboard
- [ ] Velocity updates match robot movement
- [ ] Safety margins reflect actual sensor data
- [ ] Shadow mode toggle affects real robot
- [ ] No connection errors in Console

## Configuration Reference

### Local Testing
```
Ros IP: 127.0.0.1
Ros Port: 10000
```

### Real Hardware (Example)
```
Ros IP: 192.168.1.50
Ros Port: 10000
```

### Network Requirements
- Same network/subnet for Unity laptop and Jetson
- Port 10000 open (or your configured port)
- TCP connectivity between devices

## Notes

- **No code changes required** - only update IP in Inspector
- Unity project works identically for local and real hardware
- All visualization logic remains the same
- Only the connection endpoint changes

## Support

If issues persist:
1. Check Unity Console for detailed error messages
2. Verify ROS2 topics on Jetson with `ros2 topic list`
3. Test network connectivity with `ping` and `telnet`
4. Review ROS2 bridge logs on Jetson
