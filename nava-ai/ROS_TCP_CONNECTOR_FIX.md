# Fix: ROS TCP Connector DLL Loading Error

## Error Type
**Standard DLL loading error** - Not related to God Mode code. This is a Unity package/plugin loading issue.

## Root Causes
1. **Corrupt Package Cache** - ROS TCP Connector files are corrupted
2. **Antivirus False Positive** - Firewall/antivirus blocking `UnityEngine.Robotics` as "unsafe"
3. **Path Mismatch** - Unity can't find the plugin DLL
4. **Corrupt Library Folder** - Unity's cache is corrupted

## Solution Steps

### Step 1: Reimport ROS TCP Connector (In Unity)
1. **Open Unity Editor**
2. **Window > Package Manager**
3. **Wait for refresh** (spinner stops)
4. **Scroll down** to find `ROS TCP Connector` or `NavΛ 2.0.3`
5. **Select it** and click **Reimport** (or "Install" if missing)
6. **Wait for download/import** to complete

### Step 2: Delete Library Folder (If Step 1 Fails)
1. **Close Unity Editor**
2. **Run fix script**:
   ```bash
   cd /Users/frankvanlaarhoven/Desktop/anything-mcp/NAVA-AI
   ./fix-ros-tcp-connector.sh
   ```
3. **Reopen Unity Editor**
4. **Wait for reimport** (5-10 minutes)

### Step 3: Verify GodModeRigor.cs
After fixing DLL error:
- Check `Assets/Scripts/GodModeRigor.cs` exists
- Should have ~223 lines
- If missing, restore from backup or re-create

### Step 4: Check Antivirus (If Still Fails)
1. **Check Antivirus Logs** → Quarantined Files
2. **Look for** `UnityEngine.Robotics` or `ROSTCPConnector`
3. **Restore** if found
4. **Add Unity folder to exclusions**:
   - `/Users/frankvanlaarhoven/Desktop/anything-mcp/NAVA-AI/`
5. **Disable "Game Mode"** if it blocks network apps

## Verification

After fix, verify:
- ✅ No DLL errors in Unity Console
- ✅ ROS TCP Connector shows in Package Manager with checkmark
- ✅ `using Unity.Robotics.ROSTCPConnector;` compiles without errors
- ✅ GodModeRigor.cs is intact (~223 lines)
- ✅ Can enter Play mode without errors

## Package URL (If Need to Re-add)
```
https://github.com/Unity-Technologies/ROS-TCP-Connector.git?path=/com.unity.robotics.ros-tcp-connector
```

## Quick Reference

**In Unity Package Manager:**
- Find: `ROS TCP Connector`
- Action: Click **Reimport**
- If missing: Click **+** → **Add package from git URL** → Paste URL above

**If Reimport Fails:**
- Close Unity
- Delete `Library` folder
- Reopen Unity
- Reimport package again

---

**Status**: This is a Unity package loading issue, NOT a code problem. Fix by reimporting package or clearing Library folder.
