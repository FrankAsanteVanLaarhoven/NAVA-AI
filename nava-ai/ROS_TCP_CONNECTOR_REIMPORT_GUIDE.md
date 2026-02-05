# ROS TCP Connector DLL Fix - Step-by-Step Guide

## ✅ Pre-Fix Status

- ✅ **GodModeRigor.cs**: Verified (224 lines - intact)
- ✅ **ROS TCP Connector**: Found in manifest.json
- ✅ **Library Folder**: Cleared (backed up)
- ✅ **Package Cache**: Cleared

## Step-by-Step Fix in Unity Editor

### Step 1: Open Unity Editor

1. Open **Unity Hub**
2. Find project: **NAVA-AI** (or navigate to `nava-ai` folder)
3. Click **Open** (or double-click)
4. Wait for Unity to load (2-5 minutes)

### Step 2: Open Package Manager

1. In Unity Editor menu: **Window > Package Manager**
2. Wait for package list to refresh (watch spinner in top-right)
3. **Important**: Wait until spinner stops before proceeding

### Step 3: Find ROS TCP Connector

1. **Scroll down** in the package list
2. Look for one of these names:
   - `ROS TCP Connector`
   - `NavΛ 2.0.3`
   - `com.unity.robotics.ros-tcp-connector`
3. Check the status:
   - **Blue checkmark** = Package is installed
   - **No checkmark** = Package needs to be added

### Step 4: Reimport Package

#### Option A: If Package Shows Checkmark (Installed)

1. **Click on** `ROS TCP Connector` in the list
2. Look for **"Reimport"** button (usually in package details panel)
3. **Click "Reimport"**
4. Wait for reimport to complete (1-2 minutes)
5. Watch for "Import complete" message

#### Option B: If Package is Missing

1. Click the **"+"** button (top-left of Package Manager)
2. Select **"Add package from git URL..."**
3. Enter this URL:
   ```
   https://github.com/Unity-Technologies/ROS-TCP-Connector.git?path=/com.unity.robotics.ros-tcp-connector
   ```
4. Click **"Add"**
5. Wait for download and import (2-5 minutes)

### Step 5: Verify Import

1. **Check Package Manager**:
   - ROS TCP Connector should show with blue checkmark
   - No error messages in package details

2. **Check Console**:
   - Window > General > Console
   - Should see **no DLL loading errors**
   - Look for successful import messages

3. **Verify Scripts**:
   - Check that scripts using `Unity.Robotics.ROSTCPConnector` compile
   - No red errors related to ROS TCP Connector

### Step 6: Verify GodModeRigor.cs

1. In Project window: **Assets > Scripts > GodModeRigor.cs**
2. **Verify**:
   - File exists
   - Has ~224 lines
   - No compilation errors
   - If missing or corrupted, restore from backup

### Step 7: Test Compilation

1. **Wait for compilation** to complete (watch bottom-right)
2. **Check Console**:
   - Should see **0 errors** (or minimal errors)
   - No DLL loading errors
   - ROS TCP Connector should be loaded

## Troubleshooting

### If Reimport Fails

1. **Close Unity Editor**
2. **Delete Library folder** again:
   ```bash
   cd /Users/frankvanlaarhoven/Desktop/anything-mcp/NAVA-AI/nava-ai
   rm -rf Library
   ```
3. **Reopen Unity Editor**
4. **Try reimport again**

### If Package Still Missing

1. **Check Internet Connection**
   - Package downloads from GitHub
   - Need active internet

2. **Check Firewall/Antivirus**:
   - May be blocking GitHub access
   - Add Unity to firewall exceptions

3. **Manual Add**:
   - Use the git URL provided above
   - Or download package manually and import

### If Antivirus is Blocking

1. **Check Quarantine Logs**:
   - Look for `UnityEngine.Robotics`
   - Look for `ROSTCPConnector`
   - Restore if found

2. **Add Exclusions**:
   - Add Unity folder to antivirus exclusions:
     `/Users/frankvanlaarhoven/Desktop/anything-mcp/NAVA-AI/`

3. **Disable Game Mode**:
   - If antivirus has "Game Mode"
   - Disable it (blocks network apps)

## Package URL (For Reference)

```
https://github.com/Unity-Technologies/ROS-TCP-Connector.git?path=/com.unity.robotics.ros-tcp-connector
```

## Verification Checklist

After following steps above:

- [ ] Package Manager shows ROS TCP Connector with checkmark
- [ ] Console shows no DLL loading errors
- [ ] Scripts compile without ROS-related errors
- [ ] GodModeRigor.cs exists and is intact (224 lines)
- [ ] Can enter Play mode without errors

## Next Steps After Fix

Once ROS TCP Connector is loaded:

1. **Setup Scene**: Menu > NAVA-AI Dashboard > Auto-Setup Scene Now
2. **Press Play**: Should work without DLL errors
3. **Test ROS2**: Connect to mock node or real hardware

---

**Status**: Library cleared, package ready for reimport. Follow steps above in Unity Editor.
