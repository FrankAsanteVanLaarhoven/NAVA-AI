# Quick Steps: Reimport ROS TCP Connector

## In Unity Editor (3 Steps)

### 1. Open Package Manager
**Window > Package Manager**

### 2. Find and Reimport
- Scroll to find **"ROS TCP Connector"**
- Click on it
- Click **"Reimport"** button

### 3. Wait and Verify
- Wait for import (1-2 minutes)
- Check Console - should see no DLL errors

## If Package is Missing

1. Click **"+"** button in Package Manager
2. Select **"Add package from git URL"**
3. Paste:
   ```
   https://github.com/Unity-Technologies/ROS-TCP-Connector.git?path=/com.unity.robotics.ros-tcp-connector
   ```
4. Click **"Add"**

## Done!

After reimport, DLL errors should be gone and project should run.
