using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector;

public class SceneSetupHelper : EditorWindow
{
    [MenuItem("NAVA-AI Dashboard/Setup ROS2 Scene")]
    public static void ShowWindow()
    {
        GetWindow<SceneSetupHelper>("ROS2 Scene Setup");
    }

    void OnGUI()
    {
        GUILayout.Label("ROS2 Dashboard Scene Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("Auto-Setup Complete Scene", GUILayout.Height(30)))
        {
            SetupCompleteScene();
        }

        GUILayout.Space(10);
        EditorGUILayout.HelpBox("This will create:\n" +
            "• Ground plane\n" +
            "• RealRobot and ShadowRobot cubes\n" +
            "• UI Canvas with all elements\n" +
            "• ROS_Manager GameObject with script\n" +
            "• Wireframe material for ShadowRobot", MessageType.Info);
    }

    public static void SetupCompleteScene()
    {
        Undo.SetCurrentGroupName("Setup ROS2 Scene");
        int group = Undo.GetCurrentGroup();

        // 1. Setup Ground
        GameObject ground = GameObject.Find("Ground");
        if (ground == null)
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            Undo.RegisterCreatedObjectUndo(ground, "Create Ground");
        }
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(10, 1, 10);

        // 2. Create RealRobot
        GameObject realRobot = GameObject.Find("RealRobot");
        if (realRobot == null)
        {
            realRobot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            realRobot.name = "RealRobot";
            Undo.RegisterCreatedObjectUndo(realRobot, "Create RealRobot");
        }
        realRobot.transform.position = new Vector3(0, 0.5f, 0);
        realRobot.transform.localScale = Vector3.one;

        // Create blue material for RealRobot
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }
        Material blueMat = new Material(Shader.Find("Standard"));
        blueMat.color = new Color(0.2f, 0.4f, 1.0f);
        AssetDatabase.CreateAsset(blueMat, "Assets/Materials/RealRobotMaterial.mat");
        realRobot.GetComponent<Renderer>().material = blueMat;

        // 3. Create ShadowRobot
        GameObject shadowRobot = GameObject.Find("ShadowRobot");
        if (shadowRobot == null)
        {
            shadowRobot = GameObject.Instantiate(realRobot);
            shadowRobot.name = "ShadowRobot";
            Undo.RegisterCreatedObjectUndo(shadowRobot, "Create ShadowRobot");
        }
        shadowRobot.transform.position = new Vector3(-2, 0.5f, 0);
        
        // Create wireframe material for ShadowRobot
        Material wireframeMat = new Material(Shader.Find("Unlit/Transparent"));
        wireframeMat.color = new Color(1f, 0f, 1f, 0.5f); // Purple with transparency
        AssetDatabase.CreateAsset(wireframeMat, "Assets/Materials/ShadowRobotMaterial.mat");
        shadowRobot.GetComponent<Renderer>().material = wireframeMat;
        
        // Disable shadow robot initially
        shadowRobot.SetActive(false);

        // 4. Create Canvas and UI
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");
        }

        // Create Panel (search in Canvas hierarchy)
        GameObject panel = null;
        if (canvas != null)
        {
            Transform panelTransform = canvas.transform.Find("DashboardPanel");
            if (panelTransform != null)
            {
                panel = panelTransform.gameObject;
            }
        }
        if (panel == null)
        {
            panel = new GameObject("DashboardPanel");
            panel.transform.SetParent(canvas.transform, false);
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.3f);
            
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            Undo.RegisterCreatedObjectUndo(panel, "Create Panel");
        }

        // Create Velocity Text (search in panel hierarchy)
        GameObject velocityText = null;
        if (panel != null)
        {
            Transform velTransform = panel.transform.Find("VelocityText");
            if (velTransform != null)
            {
                velocityText = velTransform.gameObject;
            }
        }
        if (velocityText == null)
        {
            velocityText = CreateUIText("VelocityText", panel.transform, "Speed: 0.0 m/s");
            RectTransform rect = velocityText.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(10, -30);
            rect.sizeDelta = new Vector2(200, 30);
            Undo.RegisterCreatedObjectUndo(velocityText, "Create VelocityText");
        }

        // Create Margin Text (search in panel hierarchy)
        GameObject marginText = null;
        if (panel != null)
        {
            Transform marginTransform = panel.transform.Find("MarginText");
            if (marginTransform != null)
            {
                marginText = marginTransform.gameObject;
            }
        }
        if (marginText == null)
        {
            marginText = CreateUIText("MarginText", panel.transform, "Safety Margin: 2.0 m");
            RectTransform rect = marginText.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(10, -70);
            rect.sizeDelta = new Vector2(250, 30);
            marginText.GetComponent<Text>().color = Color.green;
            Undo.RegisterCreatedObjectUndo(marginText, "Create MarginText");
        }

        // Create Status Text (search in panel hierarchy)
        GameObject statusText = null;
        if (panel != null)
        {
            Transform statusTransform = panel.transform.Find("StatusText");
            if (statusTransform != null)
            {
                statusText = statusTransform.gameObject;
            }
        }
        if (statusText == null)
        {
            statusText = CreateUIText("StatusText", panel.transform, "MODE: STANDARD");
            RectTransform rect = statusText.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-10, -30);
            rect.sizeDelta = new Vector2(250, 30);
            Undo.RegisterCreatedObjectUndo(statusText, "Create StatusText");
        }

        // Create Connection Indicator Image (search in panel hierarchy)
        GameObject indicator = null;
        if (panel != null)
        {
            Transform indicatorTransform = panel.transform.Find("ConnectionIndicator");
            if (indicatorTransform != null)
            {
                indicator = indicatorTransform.gameObject;
            }
        }
        if (indicator == null)
        {
            indicator = new GameObject("ConnectionIndicator");
            indicator.transform.SetParent(panel.transform, false);
            Image indicatorImage = indicator.AddComponent<Image>();
            indicatorImage.color = Color.green;
            
            RectTransform rect = indicator.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-270, -30);
            rect.sizeDelta = new Vector2(20, 20);
            Undo.RegisterCreatedObjectUndo(indicator, "Create ConnectionIndicator");
        }

        // Create Toggle Button (search in panel hierarchy)
        GameObject toggleBtn = null;
        if (panel != null)
        {
            Transform btnTransform = panel.transform.Find("ToggleShadowBtn");
            if (btnTransform != null)
            {
                toggleBtn = btnTransform.gameObject;
            }
        }
        if (toggleBtn == null)
        {
            toggleBtn = new GameObject("ToggleShadowBtn");
            toggleBtn.transform.SetParent(panel.transform, false);
            Button button = toggleBtn.AddComponent<Button>();
            Image btnImage = toggleBtn.AddComponent<Image>();
            btnImage.color = new Color(0.2f, 0.6f, 0.8f);
            
            RectTransform rect = toggleBtn.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0);
            rect.anchorMax = new Vector2(0.5f, 0);
            rect.anchoredPosition = new Vector2(0, 50);
            rect.sizeDelta = new Vector2(200, 50);
            
            // Add button text
            GameObject btnText = CreateUIText("Text", toggleBtn.transform, "TOGGLE SHADOW MODE");
            RectTransform textRect = btnText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            btnText.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
            btnText.GetComponent<Text>().fontStyle = FontStyle.Bold;
            
            Undo.RegisterCreatedObjectUndo(toggleBtn, "Create ToggleShadowBtn");
        }

        // 5. Create ROS_Manager and attach script
        GameObject rosManager = GameObject.Find("ROS_Manager");
        if (rosManager == null)
        {
            rosManager = new GameObject("ROS_Manager");
            Undo.RegisterCreatedObjectUndo(rosManager, "Create ROS_Manager");
        }

        ROS2DashboardManager manager = rosManager.GetComponent<ROS2DashboardManager>();
        if (manager == null)
        {
            manager = rosManager.AddComponent<ROS2DashboardManager>();
        }

        // Assign references
        SerializedObject so = new SerializedObject(manager);
        so.FindProperty("realRobot").objectReferenceValue = realRobot;
        so.FindProperty("shadowRobot").objectReferenceValue = shadowRobot;
        so.FindProperty("velocityText").objectReferenceValue = velocityText.GetComponent<Text>();
        so.FindProperty("marginText").objectReferenceValue = marginText.GetComponent<Text>();
        so.FindProperty("statusText").objectReferenceValue = statusText.GetComponent<Text>();
        so.FindProperty("connectionIndicator").objectReferenceValue = indicator.GetComponent<Image>();
        so.ApplyModifiedProperties();

        // Wire up button
        Button btn = toggleBtn.GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => manager.RequestToggleShadow());

        Undo.CollapseUndoOperations(group);
        Selection.activeGameObject = rosManager;
        
        // 6. Setup Advanced Features (Optional)
        SetupAdvancedFeatures(rosManager, panel.transform, realRobot);
        
        Undo.CollapseUndoOperations(group);
        Selection.activeGameObject = rosManager;
        
        EditorUtility.DisplayDialog("Setup Complete", 
            "NAVA-AI Dashboard (World-Leading Status) scene has been set up!\n\n" +
            "All objects have been created and wired up.\n" +
            "The ROS_Manager is selected in the hierarchy.\n\n" +
            "All Advanced Features:\n" +
            "• Teleoperation controller\n" +
            "• Camera feed visualizer\n" +
            "• Map visualizer\n" +
            "• Trajectory replayer\n" +
            "• LiDAR point cloud visualizer\n" +
            "• Geofence editor\n" +
            "• Battery monitor\n" +
            "• Gripper visualizer\n" +
            "• Mission planner UI\n" +
            "• Sonar visualizer\n\n" +
            "NAVΛ-Bench Features:\n" +
            "• Real-Time Voxel SLAM\n" +
            "• Neural Saliency Overlay\n" +
            "• Latency & Jitter Profiler\n" +
            "• Automated Benchmark Suite\n\n" +
            "World-Leading Features:\n" +
            "• Fleet Swarm Command Center\n" +
            "• Temporal Fusion (Time-Warp)\n" +
            "• Ephemeral UI (Context-Aware)\n\n" +
            "Universal Architecture (AGI/Humanoids/Drones):\n" +
            "• Universal Model Manager\n" +
            "• Reasoning HUD (Chain of Thought)\n" +
            "• Digital Twin Physics\n" +
            "• Swarm Formation Controller\n" +
            "• Synaptic Fire Visualizer\n\n" +
            "Ultimate Real-World Simulation:\n" +
            "• Mcity Map Loader (Real World Context)\n" +
            "• Advanced State Estimator (Kaufman + RAIM)\n" +
            "• CMDP Planner (Constrained Decision Process)\n" +
            "• Live Validator (Sim2Val + SVR/DVR)\n" +
            "• Cloud Sync Manager (Remote Operations)\n\n" +
            "Ironclad 7D VNC (Tesla/Waymo Grade):\n" +
            "• VNC 7D Verifier (CBF + Lyapunov)\n" +
            "• God Mode Overlay (7D Telemetry)\n" +
            "• Certified Safety Manager (Safety > AI)\n" +
            "• NAVΛ 7D Rigor (P-Score Calculation)\n" +
            "• Ironclad Visualizer (7D Dimensions)\n" +
            "• Ironclad Manager (Lockdown System)\n\n" +
            "Cognitive Safety (Goal + Intent + Consciousness):\n" +
            "• Consciousness Rigor (P = Safety + g + i + c)\n" +
            "• Consciousness Overlay (Fatigue Visualization)\n" +
            "• Intent Visualizer (Model Intent vs Reality)\n\n" +
            "Global-Standard Accessibility:\n" +
            "• RWiFi SLAM Manager (Signal-strength mapping)\n" +
            "• GPS-Denied PNT Manager (Dead reckoning fallback)\n" +
            "• 3D Bounding Box Projector (VLM → 3D Unity)\n" +
            "• Semantic Voice Commander (Dola-style parsing)\n" +
            "• Syncnomics SOP (Timeline synchronization)\n\n" +
            "God Mode (ISO 26262 Standard):\n" +
            "• Universal HAL (Hardware Abstraction Layer)\n" +
            "• SPARK Temporal Verifier (Sequence Safety)\n" +
            "• Swarm ECS Manager (DOTS/High-Performance)\n" +
            "• AR/XR Overlay (Holographic Dashboard)\n" +
            "• Compliance Auditor (ISO 26262 Reports)\n\n" +
            "Final Integration:\n" +
            "• NAVΛ Project Verifier (Master Controller)\n" +
            "• System Integrity Checker\n" +
            "• Component Verification\n\n" +
            "Ironclad Evolution (Online Learning):\n" +
            "• Ironclad Data Logger (Training Data Collection)\n" +
            "• Adaptive VLA Manager (Online Training)\n" +
            "• Causal Graph Builder (Certification Evidence)\n" +
            "• Evolution HUD (Training Visualization)\n\n" +
            "Adaptive Safety (Self-Healing):\n" +
            "• Environment Profiler (Terrain & Sensor Adaptation)\n" +
            "• Self-Healing Safety (Auto-Recovery)\n" +
            "• Dynamic Zone Manager (Context-Aware Zones)\n" +
            "• Enhanced Adaptive VLA (Environment-Based Risk Tuning)\n\n" +
            "Certification & Compliance:\n" +
            "• Certification Compiler (Rigor + Uncertainty → Certificate)\n" +
            "• Verified Log Chain (JSON/CSV Export)\n" +
            "• SIM2VAL++ Integration\n\n" +
            "Military/NASA Grade Features:\n" +
            "• Secure Data Logger (AES-256 Encryption, SHA-256 Signatures)\n" +
            "• Stealth Visualizer (Radar Evasion, Thermal Camouflage)\n" +
            "• Massive Data Scrapper (High-Performance Ingestion)\n" +
            "• Quick AI Processor (Fast Sim2Val++ Processing)\n" +
            "• Omnipotent AI Mode (All-Access, Bypasses Safety)\n\n" +
            "Swarm-AGI Hybrid System (Beyond Waymo):\n" +
            "• Swarm AGI Commander (Global Planning, Heterogeneous Task Delegation)\n" +
            "• Fleet Geofence (Waymo-Style Dynamic Safety Envelopes)\n" +
            "• Global Voxel Map (Tesla-Style Occupancy Network)\n" +
            "• Heterogeneous Model Manager (Dynamic VLA/RL/SSM Assignment)\n" +
            "• Swarm Certification Overlay (Fleet-Wide P-Score Visualization)\n\n" +
            "You can now:\n" +
            "1. Start the Python ROS2 node\n" +
            "2. Press Play in Unity", 
            "OK");
    }
    
    static void SetupAdvancedFeatures(GameObject rosManager, Transform panelParent, GameObject realRobot)
    {
        ROS2DashboardManager dashboardManager = rosManager.GetComponent<ROS2DashboardManager>();
        
        // 1. Add Teleoperation Controller
        UnityTeleopController teleop = rosManager.GetComponent<UnityTeleopController>();
        if (teleop == null)
        {
            teleop = rosManager.AddComponent<UnityTeleopController>();
            Debug.Log("[SceneSetup] Added UnityTeleopController - Use WASD to drive robot");
        }
        
        // 2. Create Camera Feed Display
        GameObject cameraDisplay = null;
        Transform cameraTransform = panelParent.Find("CameraDisplay");
        if (cameraTransform != null)
        {
            cameraDisplay = cameraTransform.gameObject;
        }
        if (cameraDisplay == null)
        {
            cameraDisplay = new GameObject("CameraDisplay");
            cameraDisplay.transform.SetParent(panelParent, false);
            RawImage cameraImage = cameraDisplay.AddComponent<RawImage>();
            cameraImage.color = Color.black;
            
            RectTransform rect = cameraDisplay.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.anchoredPosition = new Vector2(-160, 100);
            rect.sizeDelta = new Vector2(320, 240);
            
            CameraFeedVisualizer cameraFeed = rosManager.GetComponent<CameraFeedVisualizer>();
            if (cameraFeed == null)
            {
                cameraFeed = rosManager.AddComponent<CameraFeedVisualizer>();
            }
            SerializedObject cameraSO = new SerializedObject(cameraFeed);
            cameraSO.FindProperty("displayImage").objectReferenceValue = cameraImage;
            cameraSO.ApplyModifiedProperties();
            
            Undo.RegisterCreatedObjectUndo(cameraDisplay, "Create CameraDisplay");
        }
        
        // 3. Create Map Display
        GameObject mapDisplay = null;
        Transform mapTransform = panelParent.Find("MapDisplay");
        if (mapTransform != null)
        {
            mapDisplay = mapTransform.gameObject;
        }
        if (mapDisplay == null)
        {
            mapDisplay = new GameObject("MapDisplay");
            mapDisplay.transform.SetParent(panelParent, false);
            RawImage mapImage = mapDisplay.AddComponent<RawImage>();
            mapImage.color = Color.gray;
            
            RectTransform rect = mapDisplay.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(0, 0);
            rect.anchoredPosition = new Vector2(160, 100);
            rect.sizeDelta = new Vector2(320, 240);
            
            MapVisualizer mapViz = rosManager.GetComponent<MapVisualizer>();
            if (mapViz == null)
            {
                mapViz = rosManager.AddComponent<MapVisualizer>();
            }
            SerializedObject mapSO = new SerializedObject(mapViz);
            mapSO.FindProperty("mapDisplay").objectReferenceValue = mapImage;
            mapSO.FindProperty("dashboardManager").objectReferenceValue = dashboardManager;
            mapSO.ApplyModifiedProperties();
            
            Undo.RegisterCreatedObjectUndo(mapDisplay, "Create MapDisplay");
        }
        
        // 4. Create Replay Ghost and Trajectory Replayer
        GameObject replayGhost = GameObject.Find("ReplayGhost");
        if (replayGhost == null && realRobot != null)
        {
            replayGhost = GameObject.Instantiate(realRobot);
            replayGhost.name = "ReplayGhost";
            replayGhost.transform.position = new Vector3(2, 0.5f, 0);
            
            // Make it semi-transparent
            Renderer renderer = replayGhost.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material ghostMat = new Material(Shader.Find("Standard"));
                ghostMat.color = new Color(1f, 1f, 0f, 0.5f); // Yellow semi-transparent
                renderer.material = ghostMat;
            }
            
            replayGhost.SetActive(false);
            
            TrajectoryReplayer replayer = rosManager.GetComponent<TrajectoryReplayer>();
            if (replayer == null)
            {
                replayer = rosManager.AddComponent<TrajectoryReplayer>();
            }
            SerializedObject replaySO = new SerializedObject(replayer);
            replaySO.FindProperty("replayGhost").objectReferenceValue = replayGhost;
            replaySO.ApplyModifiedProperties();
            
            // Create Replay Button
            GameObject replayBtn = null;
            Transform replayBtnTransform = panelParent.Find("ReplayBtn");
            if (replayBtnTransform != null)
            {
                replayBtn = replayBtnTransform.gameObject;
            }
            if (replayBtn == null)
            {
                replayBtn = new GameObject("ReplayBtn");
                replayBtn.transform.SetParent(panelParent, false);
                Button button = replayBtn.AddComponent<Button>();
                Image btnImage = replayBtn.AddComponent<Image>();
                btnImage.color = new Color(1f, 0.6f, 0f);
                
                RectTransform rect = replayBtn.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0);
                rect.anchorMax = new Vector2(0.5f, 0);
                rect.anchoredPosition = new Vector2(0, 120);
                rect.sizeDelta = new Vector2(180, 40);
                
                GameObject btnText = CreateUIText("Text", replayBtn.transform, "REPLAY LAST RUN");
                RectTransform textRect = btnText.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;
                btnText.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
                btnText.GetComponent<Text>().fontStyle = FontStyle.Bold;
                btnText.GetComponent<Text>().fontSize = 12;
                
                // Wire button to replayer
                button.onClick.AddListener(() => {
                    replayer.LoadData();
                    replayer.StartReplay();
                });
                
                Undo.RegisterCreatedObjectUndo(replayBtn, "Create ReplayBtn");
            }
            
            Undo.RegisterCreatedObjectUndo(replayGhost, "Create ReplayGhost");
        }
        
        // 5. Add LiDAR Visualizer
        LiDARVisualizer lidar = rosManager.GetComponent<LiDARVisualizer>();
        if (lidar == null)
        {
            lidar = rosManager.AddComponent<LiDARVisualizer>();
            Debug.Log("[SceneSetup] Added LiDARVisualizer - Point cloud visualization ready");
        }
        
        // 6. Add Geofence Editor
        GeofenceEditor geofence = rosManager.GetComponent<GeofenceEditor>();
        if (geofence == null)
        {
            geofence = rosManager.AddComponent<GeofenceEditor>();
            Debug.Log("[SceneSetup] Added GeofenceEditor - Geofencing zones ready");
        }
        
        // 7. Create Battery Monitor UI
        GameObject batteryPanel = null;
        Transform batteryTransform = panelParent.Find("BatteryPanel");
        if (batteryTransform != null)
        {
            batteryPanel = batteryTransform.gameObject;
        }
        if (batteryPanel == null)
        {
            batteryPanel = new GameObject("BatteryPanel");
            batteryPanel.transform.SetParent(panelParent, false);
            
            RectTransform panelRect = batteryPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1, 1);
            panelRect.anchorMax = new Vector2(1, 1);
            panelRect.anchoredPosition = new Vector2(-200, -80);
            panelRect.sizeDelta = new Vector2(180, 60);
            
            // Battery Slider
            GameObject sliderObj = new GameObject("BatterySlider");
            sliderObj.transform.SetParent(batteryPanel.transform, false);
            Slider batterySlider = sliderObj.AddComponent<Slider>();
            Image sliderBg = sliderObj.AddComponent<Image>();
            sliderBg.color = new Color(0.2f, 0.2f, 0.2f);
            
            RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
            sliderRect.anchorMin = Vector2.zero;
            sliderRect.anchorMax = new Vector2(1, 0.5f);
            sliderRect.sizeDelta = Vector2.zero;
            sliderRect.offsetMin = new Vector2(10, 10);
            sliderRect.offsetMax = new Vector2(-10, -5);
            
            // Fill area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillRect = fillArea.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            fillRect.offsetMin = new Vector2(10, 0);
            fillRect.offsetMax = new Vector2(-10, 0);
            
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = Color.green;
            batterySlider.fillRect = fill.GetComponent<RectTransform>();
            
            // Battery Text
            GameObject batteryText = CreateUIText("BatteryText", batteryPanel.transform, "Battery: 100%");
            RectTransform textRect = batteryText.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0.5f);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.sizeDelta = Vector2.zero;
            textRect.offsetMin = new Vector2(10, 0);
            textRect.offsetMax = new Vector2(-10, -5);
            batteryText.GetComponent<Text>().fontSize = 12;
            
            // Voltage Text
            GameObject voltageText = CreateUIText("VoltageText", batteryPanel.transform, "12.0 V");
            RectTransform voltRect = voltageText.GetComponent<RectTransform>();
            voltRect.anchorMin = new Vector2(0, 0);
            voltRect.anchorMax = new Vector2(1, 0.5f);
            voltRect.sizeDelta = Vector2.zero;
            voltRect.offsetMin = new Vector2(10, 5);
            voltRect.offsetMax = new Vector2(-10, 0);
            voltageText.GetComponent<Text>().fontSize = 11;
            
            // Warning Light
            GameObject warningLight = new GameObject("WarningLight");
            warningLight.transform.SetParent(batteryPanel.transform, false);
            Image warningImage = warningLight.AddComponent<Image>();
            warningImage.color = Color.green;
            
            RectTransform warningRect = warningLight.GetComponent<RectTransform>();
            warningRect.anchorMin = new Vector2(1, 0.5f);
            warningRect.anchorMax = new Vector2(1, 1);
            warningRect.anchoredPosition = new Vector2(-10, 0);
            warningRect.sizeDelta = new Vector2(15, 15);
            
            // Add Battery Monitor component
            BatteryMonitor batteryMonitor = rosManager.GetComponent<BatteryMonitor>();
            if (batteryMonitor == null)
            {
                batteryMonitor = rosManager.AddComponent<BatteryMonitor>();
            }
            SerializedObject batterySO = new SerializedObject(batteryMonitor);
            batterySO.FindProperty("batterySlider").objectReferenceValue = batterySlider;
            batterySO.FindProperty("voltageText").objectReferenceValue = voltageText.GetComponent<Text>();
            batterySO.FindProperty("percentageText").objectReferenceValue = batteryText.GetComponent<Text>();
            batterySO.FindProperty("warningLight").objectReferenceValue = warningImage;
            batterySO.ApplyModifiedProperties();
            
            Undo.RegisterCreatedObjectUndo(batteryPanel, "Create BatteryPanel");
        }
        
        // 8. Add Gripper Visualizer (if gripper exists in scene)
        GameObject gripper = GameObject.Find("Gripper") ?? GameObject.Find("EndEffector");
        if (gripper != null)
        {
            GripperVisualizer gripperViz = rosManager.GetComponent<GripperVisualizer>();
            if (gripperViz == null)
            {
                gripperViz = rosManager.AddComponent<GripperVisualizer>();
                SerializedObject gripperSO = new SerializedObject(gripperViz);
                gripperSO.FindProperty("gripperRenderer").objectReferenceValue = gripper.GetComponent<Renderer>();
                gripperSO.ApplyModifiedProperties();
                Debug.Log("[SceneSetup] Added GripperVisualizer");
            }
        }
        
        // 9. Create Mission Planner UI
        GameObject missionPanel = null;
        Transform missionTransform = panelParent.Find("MissionPanel");
        if (missionTransform != null)
        {
            missionPanel = missionTransform.gameObject;
        }
        if (missionPanel == null)
        {
            missionPanel = new GameObject("MissionPanel");
            missionPanel.transform.SetParent(panelParent, false);
            
            RectTransform missionRect = missionPanel.GetComponent<RectTransform>();
            missionRect.anchorMin = new Vector2(0, 1);
            missionRect.anchorMax = new Vector2(0, 1);
            missionRect.anchoredPosition = new Vector2(10, -200);
            missionRect.sizeDelta = new Vector2(250, 150);
            
            Image panelBg = missionPanel.AddComponent<Image>();
            panelBg.color = new Color(0, 0, 0, 0.5f);
            
            // Title
            GameObject title = CreateUIText("Title", missionPanel.transform, "Mission Planner");
            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.sizeDelta = new Vector2(0, 25);
            titleRect.offsetMin = new Vector2(5, -25);
            titleRect.offsetMax = new Vector2(-5, 0);
            title.GetComponent<Text>().fontStyle = FontStyle.Bold;
            
            // Task List Container (ScrollView would be ideal, but simple for now)
            GameObject taskList = new GameObject("TaskList");
            taskList.transform.SetParent(missionPanel.transform, false);
            RectTransform listRect = taskList.GetComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0, 0);
            listRect.anchorMax = new Vector2(1, 1);
            listRect.sizeDelta = new Vector2(0, -30);
            listRect.offsetMin = new Vector2(5, 5);
            listRect.offsetMax = new Vector2(-5, -5);
            
            // Current Task Text
            GameObject currentTask = CreateUIText("CurrentTask", missionPanel.transform, "Current: None");
            RectTransform currentRect = currentTask.GetComponent<RectTransform>();
            currentRect.anchorMin = new Vector2(0, 0);
            currentRect.anchorMax = new Vector2(1, 0);
            currentRect.sizeDelta = new Vector2(0, 20);
            currentRect.offsetMin = new Vector2(5, 0);
            currentRect.offsetMax = new Vector2(-5, 5);
            currentTask.GetComponent<Text>().fontSize = 11;
            
            // Add Mission Planner component
            MissionPlannerUI missionPlanner = rosManager.GetComponent<MissionPlannerUI>();
            if (missionPlanner == null)
            {
                missionPlanner = rosManager.AddComponent<MissionPlannerUI>();
            }
            SerializedObject missionSO = new SerializedObject(missionPlanner);
            missionSO.FindProperty("taskListContainer").objectReferenceValue = taskList.transform;
            missionSO.FindProperty("currentTaskText").objectReferenceValue = currentTask.GetComponent<Text>();
            missionSO.ApplyModifiedProperties();
            
            Undo.RegisterCreatedObjectUndo(missionPanel, "Create MissionPanel");
        }
        
        // 10. Add Sonar Visualizer
        SonarVisualizer sonar = rosManager.GetComponent<SonarVisualizer>();
        if (sonar == null)
        {
            sonar = rosManager.AddComponent<SonarVisualizer>();
            Debug.Log("[SceneSetup] Added SonarVisualizer - Sonar visualization ready");
        }
        
        // 11. NAVΛ-Bench Features - Real-Time Voxel SLAM
        VoxelMapBuilder voxelBuilder = rosManager.GetComponent<VoxelMapBuilder>();
        if (voxelBuilder == null)
        {
            voxelBuilder = rosManager.AddComponent<VoxelMapBuilder>();
            Debug.Log("[SceneSetup] Added VoxelMapBuilder - Real-time voxel SLAM ready");
        }
        
        // 12. NAVΛ-Bench Features - Neural Saliency Overlay
        VlaSaliencyOverlay saliency = rosManager.GetComponent<VlaSaliencyOverlay>();
        if (saliency == null)
        {
            saliency = rosManager.AddComponent<VlaSaliencyOverlay>();
            
            // Create saliency UI if not exists
            GameObject saliencyDisplay = null;
            Transform saliencyTransform = panelParent.Find("SaliencyDisplay");
            if (saliencyTransform != null)
            {
                saliencyDisplay = saliencyTransform.gameObject;
            }
            if (saliencyDisplay == null)
            {
                saliencyDisplay = new GameObject("SaliencyDisplay");
                saliencyDisplay.transform.SetParent(panelParent, false);
                RawImage saliencyImage = saliencyDisplay.AddComponent<RawImage>();
                saliencyImage.color = Color.black;
                
                RectTransform rect = saliencyDisplay.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(1, 1);
                rect.anchorMax = new Vector2(1, 1);
                rect.anchoredPosition = new Vector2(-180, -100);
                rect.sizeDelta = new Vector2(160, 120);
                
                SerializedObject saliencySO = new SerializedObject(saliency);
                saliencySO.FindProperty("saliencyMap").objectReferenceValue = saliencyImage;
                saliencySO.ApplyModifiedProperties();
                
                Undo.RegisterCreatedObjectUndo(saliencyDisplay, "Create SaliencyDisplay");
            }
            
            Debug.Log("[SceneSetup] Added VlaSaliencyOverlay - AI attention visualization ready");
        }
        
        // 13. NAVΛ-Bench Features - Latency Profiler
        GameObject latencyPanel = null;
        Transform latencyTransform = panelParent.Find("LatencyPanel");
        if (latencyTransform != null)
        {
            latencyPanel = latencyTransform.gameObject;
        }
        if (latencyPanel == null)
        {
            latencyPanel = new GameObject("LatencyPanel");
            latencyPanel.transform.SetParent(panelParent, false);
            
            RectTransform panelRect = latencyPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 1);
            panelRect.anchorMax = new Vector2(0, 1);
            panelRect.anchoredPosition = new Vector2(10, -100);
            panelRect.sizeDelta = new Vector2(300, 80);
            
            Image panelBg = latencyPanel.AddComponent<Image>();
            panelBg.color = new Color(0, 0, 0, 0.5f);
            
            // Jitter Text
            GameObject jitterText = CreateUIText("JitterText", latencyPanel.transform, "Jitter: 0.000ms");
            RectTransform jitterRect = jitterText.GetComponent<RectTransform>();
            jitterRect.anchorMin = new Vector2(0, 0.5f);
            jitterRect.anchorMax = new Vector2(1, 1);
            jitterRect.sizeDelta = Vector2.zero;
            jitterRect.offsetMin = new Vector2(5, 0);
            jitterRect.offsetMax = new Vector2(-5, -5);
            jitterText.GetComponent<Text>().fontSize = 11;
            
            // Latency Text
            GameObject latencyText = CreateUIText("LatencyText", latencyPanel.transform, "Latency: 0.00ms");
            RectTransform latencyRect = latencyText.GetComponent<RectTransform>();
            latencyRect.anchorMin = new Vector2(0, 0);
            latencyRect.anchorMax = new Vector2(1, 0.5f);
            latencyRect.sizeDelta = Vector2.zero;
            latencyRect.offsetMin = new Vector2(5, 5);
            latencyRect.offsetMax = new Vector2(-5, 0);
            latencyText.GetComponent<Text>().fontSize = 11;
            
            // Add LatencyProfiler component
            LatencyProfiler profiler = rosManager.GetComponent<LatencyProfiler>();
            if (profiler == null)
            {
                profiler = rosManager.AddComponent<LatencyProfiler>();
            }
            SerializedObject profilerSO = new SerializedObject(profiler);
            profilerSO.FindProperty("jitterText").objectReferenceValue = jitterText.GetComponent<Text>();
            profilerSO.FindProperty("latencyText").objectReferenceValue = latencyText.GetComponent<Text>();
            profilerSO.ApplyModifiedProperties();
            
            Undo.RegisterCreatedObjectUndo(latencyPanel, "Create LatencyPanel");
        }
        
        // 14. NAVΛ-Bench Features - Benchmark Runner
        BenchmarkRunner benchmark = rosManager.GetComponent<BenchmarkRunner>();
        if (benchmark == null)
        {
            benchmark = rosManager.AddComponent<BenchmarkRunner>();
            SerializedObject benchmarkSO = new SerializedObject(benchmark);
            benchmarkSO.FindProperty("robot").objectReferenceValue = realRobot;
            benchmarkSO.FindProperty("dashboardManager").objectReferenceValue = dashboardManager;
            benchmarkSO.ApplyModifiedProperties();
            Debug.Log("[SceneSetup] Added BenchmarkRunner - Automated testing suite ready");
        }
        
        // 15. World-Leading Features - Fleet Manager
        FleetManager fleet = rosManager.GetComponent<FleetManager>();
        if (fleet == null)
        {
            fleet = rosManager.AddComponent<FleetManager>();
            SerializedObject fleetSO = new SerializedObject(fleet);
            fleetSO.FindProperty("robotPrefab").objectReferenceValue = realRobot;
            fleetSO.FindProperty("fleetOrigin").objectReferenceValue = rosManager.transform;
            fleetSO.ApplyModifiedProperties();
            Debug.Log("[SceneSetup] Added FleetManager - Multi-agent swarm control ready");
        }
        
        // 16. World-Leading Features - Temporal Fusion
        TemporalFusionVisualizer temporal = rosManager.GetComponent<TemporalFusionVisualizer>();
        if (temporal == null)
        {
            temporal = rosManager.AddComponent<TemporalFusionVisualizer>();
            SerializedObject temporalSO = new SerializedObject(temporal);
            temporalSO.FindProperty("currentRobot").objectReferenceValue = realRobot;
            temporalSO.FindProperty("shadowRobot").objectReferenceValue = shadowRobot;
            temporalSO.ApplyModifiedProperties();
            Debug.Log("[SceneSetup] Added TemporalFusionVisualizer - Time-warp visualization ready");
        }
        
        // 17. World-Leading Features - Ephemeral UI
        EphemeralUI ephemeral = rosManager.GetComponent<EphemeralUI>();
        if (ephemeral == null)
        {
            ephemeral = rosManager.AddComponent<EphemeralUI>();
            
            // Setup CanvasGroups for ephemeral UI
            if (panelParent != null)
            {
                CanvasGroup hudGroup = panelParent.GetComponent<CanvasGroup>();
                if (hudGroup == null)
                {
                    hudGroup = panelParent.gameObject.AddComponent<CanvasGroup>();
                }
                
                // Create detail group
                GameObject detailPanel = new GameObject("DetailPanel");
                detailPanel.transform.SetParent(panelParent, false);
                CanvasGroup detailGroup = detailPanel.AddComponent<CanvasGroup>();
                detailGroup.alpha = 0f;
                detailPanel.SetActive(false);
                
                RectTransform detailRect = detailPanel.GetComponent<RectTransform>();
                detailRect.anchorMin = Vector2.zero;
                detailRect.anchorMax = Vector2.one;
                detailRect.sizeDelta = Vector2.zero;
                
                Image detailBg = detailPanel.AddComponent<Image>();
                detailBg.color = new Color(0, 0, 0, 0.8f);
                
                SerializedObject ephemeralSO = new SerializedObject(ephemeral);
                ephemeralSO.FindProperty("hudGroup").objectReferenceValue = hudGroup;
                ephemeralSO.FindProperty("detailGroup").objectReferenceValue = detailGroup;
                ephemeralSO.ApplyModifiedProperties();
                
                Undo.RegisterCreatedObjectUndo(detailPanel, "Create DetailPanel");
            }
            
            Debug.Log("[SceneSetup] Added EphemeralUI - Context-aware HUD ready");
        }
        
        // 18. Ultimate Features - Mcity Map Loader
        McityMapLoader mcityLoader = rosManager.GetComponent<McityMapLoader>();
        if (mcityLoader == null)
        {
            mcityLoader = rosManager.AddComponent<McityMapLoader>();
            Debug.Log("[SceneSetup] Added McityMapLoader - Real-world map data ready");
        }
        
        // 19. Ultimate Features - Advanced State Estimator
        GameObject estimatorPanel = null;
        Transform estimatorTransform = panelParent.Find("EstimatorPanel");
        if (estimatorTransform != null)
        {
            estimatorPanel = estimatorTransform.gameObject;
        }
        if (estimatorPanel == null)
        {
            estimatorPanel = new GameObject("EstimatorPanel");
            estimatorPanel.transform.SetParent(panelParent, false);
            
            RectTransform panelRect = estimatorPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1, 1);
            panelRect.anchorMax = new Vector2(1, 1);
            panelRect.anchoredPosition = new Vector2(-10, -250);
            panelRect.sizeDelta = new Vector2(300, 100);
            
            Image panelBg = estimatorPanel.AddComponent<Image>();
            panelBg.color = new Color(0, 0, 0, 0.5f);
            
            // PNT Status Text
            GameObject pntText = CreateUIText("PNTStatusText", estimatorPanel.transform, "PNT: Initializing...");
            RectTransform pntRect = pntText.GetComponent<RectTransform>();
            pntRect.anchorMin = new Vector2(0, 0.5f);
            pntRect.anchorMax = new Vector2(1, 1);
            pntRect.sizeDelta = Vector2.zero;
            pntRect.offsetMin = new Vector2(5, 0);
            pntRect.offsetMax = new Vector2(-5, -5);
            pntText.GetComponent<Text>().fontSize = 11;
            
            // RAIM Status Text
            GameObject raimText = CreateUIText("RAIMStatusText", estimatorPanel.transform, "RAIM: GOOD");
            RectTransform raimRect = raimText.GetComponent<RectTransform>();
            raimRect.anchorMin = new Vector2(0, 0);
            raimRect.anchorMax = new Vector2(1, 0.5f);
            raimRect.sizeDelta = Vector2.zero;
            raimRect.offsetMin = new Vector2(5, 5);
            raimRect.offsetMax = new Vector2(-5, 0);
            raimText.GetComponent<Text>().fontSize = 11;
            
            // Add AdvancedEstimator component
            AdvancedEstimator estimator = rosManager.GetComponent<AdvancedEstimator>();
            if (estimator == null)
            {
                estimator = rosManager.AddComponent<AdvancedEstimator>();
            }
            SerializedObject estimatorSO = new SerializedObject(estimator);
            estimatorSO.FindProperty("pntStatusText").objectReferenceValue = pntText.GetComponent<Text>();
            estimatorSO.FindProperty("raimStatusText").objectReferenceValue = raimText.GetComponent<Text>();
            estimatorSO.ApplyModifiedProperties();
            
            Undo.RegisterCreatedObjectUndo(estimatorPanel, "Create EstimatorPanel");
        }
        
        // 20. Ultimate Features - CMDP Planner
        MarkovDecisionPlanner cmdp = rosManager.GetComponent<MarkovDecisionPlanner>();
        if (cmdp == null)
        {
            cmdp = rosManager.AddComponent<MarkovDecisionPlanner>();
            SerializedObject cmdpSO = new SerializedObject(cmdp);
            cmdpSO.FindProperty("geofenceEditor").objectReferenceValue = rosManager.GetComponent<GeofenceEditor>();
            cmdpSO.ApplyModifiedProperties();
            Debug.Log("[SceneSetup] Added MarkovDecisionPlanner - CMDP planning ready");
        }
        
        // 21. Ultimate Features - Live Validator
        GameObject validatorPanel = null;
        Transform validatorTransform = panelParent.Find("ValidatorPanel");
        if (validatorTransform != null)
        {
            validatorPanel = validatorTransform.gameObject;
        }
        if (validatorPanel == null)
        {
            validatorPanel = new GameObject("ValidatorPanel");
            validatorPanel.transform.SetParent(panelParent, false);
            
            RectTransform panelRect = validatorPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 1);
            panelRect.anchorMax = new Vector2(0, 1);
            panelRect.anchoredPosition = new Vector2(10, -350);
            panelRect.sizeDelta = new Vector2(300, 100);
            
            Image panelBg = validatorPanel.AddComponent<Image>();
            panelBg.color = new Color(0, 0, 0, 0.5f);
            
            // Sim2Val Status Text
            GameObject sim2valText = CreateUIText("Sim2ValStatusText", validatorPanel.transform, "Sim2Val: VALIDATED");
            RectTransform sim2valRect = sim2valText.GetComponent<RectTransform>();
            sim2valRect.anchorMin = new Vector2(0, 0.66f);
            sim2valRect.anchorMax = new Vector2(1, 1);
            sim2valRect.sizeDelta = Vector2.zero;
            sim2valRect.offsetMin = new Vector2(5, 0);
            sim2valRect.offsetMax = new Vector2(-5, -5);
            sim2valText.GetComponent<Text>().fontSize = 11;
            
            // Failure Rate Text
            GameObject failureText = CreateUIText("FailureRateText", validatorPanel.transform, "Failure Rate: 1e-6");
            RectTransform failureRect = failureText.GetComponent<RectTransform>();
            failureRect.anchorMin = new Vector2(0, 0.33f);
            failureRect.anchorMax = new Vector2(1, 0.66f);
            failureRect.sizeDelta = Vector2.zero;
            failureRect.offsetMin = new Vector2(5, 0);
            failureRect.offsetMax = new Vector2(-5, 0);
            failureText.GetComponent<Text>().fontSize = 11;
            
            // SVR Confidence Text
            GameObject svrText = CreateUIText("SVRConfidenceText", validatorPanel.transform, "SVR: 95%");
            RectTransform svrRect = svrText.GetComponent<RectTransform>();
            svrRect.anchorMin = new Vector2(0, 0);
            svrRect.anchorMax = new Vector2(1, 0.33f);
            svrRect.sizeDelta = Vector2.zero;
            svrRect.offsetMin = new Vector2(5, 5);
            svrRect.offsetMax = new Vector2(-5, 0);
            svrText.GetComponent<Text>().fontSize = 11;
            
            // Add LiveValidator component
            LiveValidator validator = rosManager.GetComponent<LiveValidator>();
            if (validator == null)
            {
                validator = rosManager.AddComponent<LiveValidator>();
            }
            SerializedObject validatorSO = new SerializedObject(validator);
            validatorSO.FindProperty("sim2valStatusText").objectReferenceValue = sim2valText.GetComponent<Text>();
            validatorSO.FindProperty("failureRateText").objectReferenceValue = failureText.GetComponent<Text>();
            validatorSO.FindProperty("svrConfidenceText").objectReferenceValue = svrText.GetComponent<Text>();
            validatorSO.ApplyModifiedProperties();
            
            Undo.RegisterCreatedObjectUndo(validatorPanel, "Create ValidatorPanel");
        }
        
        // 22. Ultimate Features - Cloud Sync Manager
        CloudSyncManager cloudSync = rosManager.GetComponent<CloudSyncManager>();
        if (cloudSync == null)
        {
            cloudSync = rosManager.AddComponent<CloudSyncManager>();
            SerializedObject cloudSO = new SerializedObject(cloudSync);
            cloudSO.FindProperty("mcityLoader").objectReferenceValue = mcityLoader;
            cloudSO.FindProperty("dashboardManager").objectReferenceValue = dashboardManager;
            cloudSO.ApplyModifiedProperties();
            Debug.Log("[SceneSetup] Added CloudSyncManager - Remote sync ready");
        }
        
        // 23. Ironclad 7D VNC - VNC 7D Verifier
        Vnc7dVerifier vncVerifier = realRobot.GetComponent<Vnc7dVerifier>();
        if (vncVerifier == null)
        {
            vncVerifier = realRobot.AddComponent<Vnc7dVerifier>();
            SerializedObject vncSO = new SerializedObject(vncVerifier);
            vncSO.FindProperty("estimator").objectReferenceValue = rosManager.GetComponent<AdvancedEstimator>();
            vncSO.FindProperty("robotRigidbody").objectReferenceValue = realRobot.GetComponent<Rigidbody>();
            vncSO.ApplyModifiedProperties();
            Debug.Log("[SceneSetup] Added Vnc7dVerifier - 7D CBF verification ready");
        }
        
        // 24. Ironclad 7D VNC - God Mode Overlay
        GodModeOverlay godMode = realRobot.GetComponent<GodModeOverlay>();
        if (godMode == null)
        {
            godMode = realRobot.AddComponent<GodModeOverlay>();
            SerializedObject godSO = new SerializedObject(godMode);
            godSO.FindProperty("verifier").objectReferenceValue = vncVerifier;
            godSO.ApplyModifiedProperties();
            Debug.Log("[SceneSetup] Added GodModeOverlay - 7D telemetry ready");
        }
        
        // 25. Ironclad 7D VNC - Certified Safety Manager
        CertifiedSafetyManager safetyManager = rosManager.GetComponent<CertifiedSafetyManager>();
        if (safetyManager == null)
        {
            safetyManager = rosManager.AddComponent<CertifiedSafetyManager>();
            SerializedObject safetySO = new SerializedObject(safetyManager);
            safetySO.FindProperty("verifier").objectReferenceValue = vncVerifier;
            safetySO.FindProperty("modelManager").objectReferenceValue = rosManager.GetComponent<UniversalModelManager>();
            safetySO.FindProperty("robotRigidbody").objectReferenceValue = realRobot.GetComponent<Rigidbody>();
            safetySO.ApplyModifiedProperties();
            Debug.Log("[SceneSetup] Added CertifiedSafetyManager - Ironclad safety enforcement ready");
        }
        
        // 26. Ironclad 7D VNC - NAVΛ 7D Rigor
        Navl7dRigor rigor = realRobot.GetComponent<Navl7dRigor>();
        if (rigor == null)
        {
            rigor = realRobot.AddComponent<Navl7dRigor>();
            SerializedObject rigorSO = new SerializedObject(rigor);
            rigorSO.FindProperty("modelManager").objectReferenceValue = rosManager.GetComponent<UniversalModelManager>();
            rigorSO.FindProperty("estimator").objectReferenceValue = rosManager.GetComponent<AdvancedEstimator>();
            rigorSO.ApplyModifiedProperties();
            Debug.Log("[SceneSetup] Added Navl7dRigor - P-score calculation ready");
        }
        
        // 27. Ironclad 7D VNC - Ironclad Visualizer
        GameObject rigorPanel = null;
        Transform rigorTransform = panelParent.Find("RigorPanel");
        if (rigorTransform != null)
        {
            rigorPanel = rigorTransform.gameObject;
        }
        if (rigorPanel == null)
        {
            rigorPanel = new GameObject("RigorPanel");
            rigorPanel.transform.SetParent(panelParent, false);
            
            RectTransform panelRect = rigorPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0);
            panelRect.anchorMax = new Vector2(0.5f, 0);
            panelRect.anchoredPosition = new Vector2(0, 10);
            panelRect.sizeDelta = new Vector2(400, 200);
            
            Image panelBg = rigorPanel.AddComponent<Image>();
            panelBg.color = new Color(0, 0, 0, 0.7f);
            
            // P-Score Display
            GameObject pScoreText = CreateUIText("PScoreText", rigorPanel.transform, "P-Score: 0.00");
            RectTransform pScoreRect = pScoreText.GetComponent<RectTransform>();
            pScoreRect.anchorMin = new Vector2(0, 0.8f);
            pScoreRect.anchorMax = new Vector2(1, 1);
            pScoreRect.sizeDelta = Vector2.zero;
            pScoreRect.offsetMin = new Vector2(10, 0);
            pScoreRect.offsetMax = new Vector2(-10, -5);
            pScoreText.GetComponent<Text>().fontSize = 14;
            pScoreText.GetComponent<Text>().fontStyle = FontStyle.Bold;
            
            // Equation Display
            GameObject equationText = CreateUIText("EquationText", rigorPanel.transform, "P = x + t + g + i + c");
            RectTransform equationRect = equationText.GetComponent<RectTransform>();
            equationRect.anchorMin = new Vector2(0, 0.6f);
            equationRect.anchorMax = new Vector2(1, 0.8f);
            equationRect.sizeDelta = Vector2.zero;
            equationRect.offsetMin = new Vector2(10, 0);
            equationRect.offsetMax = new Vector2(-10, 0);
            equationText.GetComponent<Text>().fontSize = 11;
            
            // Breach Status
            GameObject breachText = CreateUIText("BreachStatusText", rigorPanel.transform, "RIGOR: P IN BOUNDS");
            RectTransform breachRect = breachText.GetComponent<RectTransform>();
            breachRect.anchorMin = new Vector2(0, 0);
            breachRect.anchorMax = new Vector2(1, 0.6f);
            breachRect.sizeDelta = Vector2.zero;
            breachRect.offsetMin = new Vector2(10, 10);
            breachRect.offsetMax = new Vector2(-10, 0);
            breachText.GetComponent<Text>().fontSize = 12;
            breachText.GetComponent<Text>().color = Color.cyan;
            
            // Link to Navl7dRigor
            SerializedObject rigorSO = new SerializedObject(rigor);
            rigorSO.FindProperty("pValueDisplay").objectReferenceValue = pScoreText.GetComponent<Text>();
            rigorSO.FindProperty("equationDisplay").objectReferenceValue = equationText.GetComponent<Text>();
            rigorSO.FindProperty("breachStatus").objectReferenceValue = breachText.GetComponent<Text>();
            rigorSO.ApplyModifiedProperties();
            
            Undo.RegisterCreatedObjectUndo(rigorPanel, "Create RigorPanel");
        }
        
        // 28. Ironclad 7D VNC - Ironclad Manager
        IroncladManager ironclad = realRobot.GetComponent<IroncladManager>();
        if (ironclad == null)
        {
            ironclad = realRobot.AddComponent<IroncladManager>();
            Debug.Log("[SceneSetup] Added IroncladManager - Lockdown system ready");
        }
        
        // 29. Ironclad 7D VNC - Ironclad Visualizer
        IroncladVisualizer ironcladViz = rigorPanel.GetComponent<IroncladVisualizer>();
        if (ironcladViz == null)
        {
            ironcladViz = rigorPanel.AddComponent<IroncladVisualizer>();
            SerializedObject vizSO = new SerializedObject(ironcladViz);
            vizSO.FindProperty("rigor").objectReferenceValue = rigor;
            vizSO.ApplyModifiedProperties();
            Debug.Log("[SceneSetup] Added IroncladVisualizer - 7D dimension visualization ready");
        }
        
        // 30. Cognitive Safety - Consciousness Rigor
        NavlConsciousnessRigor consciousnessRigor = realRobot.GetComponent<NavlConsciousnessRigor>();
        if (consciousnessRigor == null)
        {
            consciousnessRigor = realRobot.AddComponent<NavlConsciousnessRigor>();
            SerializedObject consciousnessSO = new SerializedObject(consciousnessRigor);
            consciousnessSO.FindProperty("vlaSaliency").objectReferenceValue = rosManager.GetComponent<VlaSaliencyOverlay>();
            consciousnessSO.FindProperty("vncVerifier").objectReferenceValue = vncVerifier;
            consciousnessSO.FindProperty("estimator").objectReferenceValue = rosManager.GetComponent<AdvancedEstimator>();
            // Create target goal if not exists
            GameObject goalObj = GameObject.Find("TargetGoal");
            if (goalObj == null)
            {
                goalObj = new GameObject("TargetGoal");
                goalObj.transform.position = new Vector3(5, 0.5f, 5);
                Undo.RegisterCreatedObjectUndo(goalObj, "Create TargetGoal");
            }
            consciousnessSO.FindProperty("targetGoal").objectReferenceValue = goalObj.transform;
            consciousnessSO.ApplyModifiedProperties();
            Debug.Log("[SceneSetup] Added NavlConsciousnessRigor - Cognitive safety ready");
        }
        
        // 31. Cognitive Safety - Consciousness Overlay
        GameObject consciousnessPanel = null;
        Transform consciousnessTransform = panelParent.Find("ConsciousnessPanel");
        if (consciousnessTransform != null)
        {
            consciousnessPanel = consciousnessTransform.gameObject;
        }
        if (consciousnessPanel == null)
        {
            consciousnessPanel = new GameObject("ConsciousnessPanel");
            consciousnessPanel.transform.SetParent(panelParent, false);
            
            RectTransform panelRect = consciousnessPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(300, 100);
            
            CanvasGroup overlayGroup = consciousnessPanel.AddComponent<CanvasGroup>();
            overlayGroup.alpha = 0;
            overlayGroup.interactable = false;
            overlayGroup.blocksRaycasts = false;
            
            Image panelBg = consciousnessPanel.AddComponent<Image>();
            panelBg.color = new Color(0, 0, 0, 0.5f);
            
            // Fatigue Text
            GameObject fatigueText = CreateUIText("FatigueText", consciousnessPanel.transform, "SYSTEM: CONSCIOUS");
            RectTransform fatigueRect = fatigueText.GetComponent<RectTransform>();
            fatigueRect.anchorMin = new Vector2(0, 0);
            fatigueRect.anchorMax = new Vector2(1, 1);
            fatigueRect.sizeDelta = Vector2.zero;
            fatigueRect.offsetMin = new Vector2(10, 10);
            fatigueRect.offsetMax = new Vector2(-10, -10);
            fatigueText.GetComponent<Text>().fontSize = 16;
            fatigueText.GetComponent<Text>().fontStyle = FontStyle.Bold;
            fatigueText.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
            fatigueText.GetComponent<Text>().color = Color.green;
            
            // Reticle
            GameObject reticleObj = new GameObject("Reticle");
            reticleObj.transform.SetParent(consciousnessPanel.transform, false);
            Image reticleImage = reticleObj.AddComponent<Image>();
            reticleImage.color = Color.white;
            RectTransform reticleRect = reticleObj.GetComponent<RectTransform>();
            reticleRect.anchorMin = new Vector2(0.5f, 0.5f);
            reticleRect.anchorMax = new Vector2(0.5f, 0.5f);
            reticleRect.sizeDelta = new Vector2(20, 20);
            reticleRect.anchoredPosition = Vector2.zero;
            
            // Add ConsciousnessOverlay component
            ConsciousnessOverlay consciousnessOverlay = consciousnessPanel.AddComponent<ConsciousnessOverlay>();
            SerializedObject overlaySO = new SerializedObject(consciousnessOverlay);
            overlaySO.FindProperty("fatigueOverlay").objectReferenceValue = overlayGroup;
            overlaySO.FindProperty("fatigueText").objectReferenceValue = fatigueText.GetComponent<Text>();
            overlaySO.FindProperty("reticle").objectReferenceValue = reticleRect;
            overlaySO.FindProperty("consciousnessRigor").objectReferenceValue = consciousnessRigor;
            overlaySO.ApplyModifiedProperties();
            
            Undo.RegisterCreatedObjectUndo(consciousnessPanel, "Create ConsciousnessPanel");
        }
        
        // 32. Cognitive Safety - Intent Visualizer
        IntentVisualizer intentViz = realRobot.GetComponent<IntentVisualizer>();
        if (intentViz == null)
        {
            intentViz = realRobot.AddComponent<IntentVisualizer>();
            SerializedObject intentSO = new SerializedObject(intentViz);
            intentSO.FindProperty("teleopController").objectReferenceValue = realRobot.GetComponent<UnityTeleopController>();
            intentSO.FindProperty("vlaSaliency").objectReferenceValue = rosManager.GetComponent<VlaSaliencyOverlay>();
            intentSO.FindProperty("consciousnessRigor").objectReferenceValue = consciousnessRigor;
            intentSO.ApplyModifiedProperties();
            Debug.Log("[SceneSetup] Added IntentVisualizer - Intent visualization ready");
        }
        
        // 33. Accessibility - RWiFi SLAM Manager
        RwifiSlamManager rwifi = rosManager.GetComponent<RwifiSlamManager>();
        if (rwifi == null)
        {
            rwifi = rosManager.AddComponent<RwifiSlamManager>();
            Debug.Log("[SceneSetup] Added RwifiSlamManager - Signal-strength mapping ready");
        }
        
        // 34. Accessibility - GPS-Denied PNT Manager
        GpsDeniedPntManager gpsDenied = realRobot.GetComponent<GpsDeniedPntManager>();
        if (gpsDenied == null)
        {
            gpsDenied = realRobot.AddComponent<GpsDeniedPntManager>();
            Debug.Log("[SceneSetup] Added GpsDeniedPntManager - Dead reckoning fallback ready");
        }
        
        // 35. Accessibility - 3D Bounding Box Projector
        BBox3dProjector bboxProjector = rosManager.GetComponent<BBox3dProjector>();
        if (bboxProjector == null)
        {
            bboxProjector = rosManager.AddComponent<BBox3dProjector>();
            SerializedObject bboxSO = new SerializedObject(bboxProjector);
            bboxSO.FindProperty("visionCamera").objectReferenceValue = Camera.main;
            bboxSO.ApplyModifiedProperties();
            Debug.Log("[SceneSetup] Added BBox3dProjector - 3D detection projection ready");
        }
        
        // 36. Accessibility - Semantic Voice Commander
        SemanticVoiceCommander voiceCommander = rosManager.GetComponent<SemanticVoiceCommander>();
        if (voiceCommander == null)
        {
            voiceCommander = rosManager.AddComponent<SemanticVoiceCommander>();
            SerializedObject voiceSO = new SerializedObject(voiceCommander);
            voiceSO.FindProperty("reasoningHUD").objectReferenceValue = rosManager.GetComponent<ReasoningHUD>();
            voiceSO.FindProperty("dashboardManager").objectReferenceValue = dashboardManager;
            voiceSO.ApplyModifiedProperties();
            Debug.Log("[SceneSetup] Added SemanticVoiceCommander - Semantic voice parsing ready");
        }
        
        // 37. Accessibility - Syncnomics SOP
        SyncnomicsSop syncnomics = rosManager.GetComponent<SyncnomicsSop>();
        if (syncnomics == null)
        {
            syncnomics = rosManager.AddComponent<SyncnomicsSop>();
            Debug.Log("[SceneSetup] Added SyncnomicsSop - Timeline synchronization ready");
        }
        
        // 38. God Mode - Universal HAL
        UniversalHal hal = rosManager.GetComponent<UniversalHal>();
        if (hal == null)
        {
            hal = rosManager.AddComponent<UniversalHal>();
            Debug.Log("[SceneSetup] Added UniversalHal - Hardware abstraction ready");
        }
        
        // 39. God Mode - SPARK Temporal Verifier
        SparkTemporalVerifier spark = realRobot.GetComponent<SparkTemporalVerifier>();
        if (spark == null)
        {
            spark = realRobot.AddComponent<SparkTemporalVerifier>();
            Debug.Log("[SceneSetup] Added SparkTemporalVerifier - Temporal logic ready");
        }
        
        // 40. God Mode - Swarm ECS Manager
        SwarmEcsManager swarmECS = rosManager.GetComponent<SwarmEcsManager>();
        if (swarmECS == null)
        {
            swarmECS = rosManager.AddComponent<SwarmEcsManager>();
            SerializedObject swarmSO = new SerializedObject(swarmECS);
            swarmSO.FindProperty("agentPrefab").objectReferenceValue = realRobot;
            swarmSO.ApplyModifiedProperties();
            Debug.Log("[SceneSetup] Added SwarmEcsManager - High-performance swarm ready");
        }
        
        // 41. God Mode - AR/XR Overlay
        ArOverlay arOverlay = rosManager.GetComponent<ArOverlay>();
        if (arOverlay == null)
        {
            arOverlay = rosManager.AddComponent<ArOverlay>();
            SerializedObject arSO = new SerializedObject(arOverlay);
            arSO.FindProperty("robotRoot").objectReferenceValue = realRobot.transform;
            arSO.FindProperty("sparkVerifier").objectReferenceValue = spark;
            arSO.FindProperty("vncVerifier").objectReferenceValue = vncVerifier;
            arSO.ApplyModifiedProperties();
            Debug.Log("[SceneSetup] Added ArOverlay - Holographic dashboard ready");
        }
        
        // 42. God Mode - Compliance Auditor
        ComplianceAuditor auditor = rosManager.GetComponent<ComplianceAuditor>();
        if (auditor == null)
        {
            auditor = rosManager.AddComponent<ComplianceAuditor>();
            SerializedObject auditorSO = new SerializedObject(auditor);
            auditorSO.FindProperty("vncVerifier").objectReferenceValue = vncVerifier;
            auditorSO.FindProperty("sparkVerifier").objectReferenceValue = spark;
            auditorSO.FindProperty("universalHal").objectReferenceValue = hal;
            auditorSO.FindProperty("consciousnessRigor").objectReferenceValue = consciousnessRigor;
            auditorSO.FindProperty("liveValidator").objectReferenceValue = rosManager.GetComponent<LiveValidator>();
            auditorSO.ApplyModifiedProperties();
            Debug.Log("[SceneSetup] Added ComplianceAuditor - ISO 26262 compliance ready");
        }
        
        // 43. Final Integration - NAVΛ Project Verifier (Master Controller)
        GameObject systemVerifierObj = GameObject.Find("SystemVerifier");
        if (systemVerifierObj == null)
        {
            systemVerifierObj = new GameObject("SystemVerifier");
            Undo.RegisterCreatedObjectUndo(systemVerifierObj, "Create SystemVerifier");
        }
        
        NavlProjectVerifier projectVerifier = systemVerifierObj.GetComponent<NavlProjectVerifier>();
        if (projectVerifier == null)
        {
            projectVerifier = systemVerifierObj.AddComponent<NavlProjectVerifier>();
            
            // Create UI for verifier
            GameObject verifierPanel = new GameObject("VerifierPanel");
            verifierPanel.transform.SetParent(panelParent, false);
            
            RectTransform panelRect = verifierPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 1);
            panelRect.anchorMax = new Vector2(0.5f, 1);
            panelRect.anchoredPosition = new Vector2(0, -10);
            panelRect.sizeDelta = new Vector2(500, 150);
            
            Image panelBg = verifierPanel.AddComponent<Image>();
            panelBg.color = new Color(0, 0, 0, 0.8f);
            
            // Final Status Text
            GameObject statusText = CreateUIText("FinalStatusText", verifierPanel.transform, "SYSTEM: VERIFYING...");
            RectTransform statusRect = statusText.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0, 0.5f);
            statusRect.anchorMax = new Vector2(1, 1);
            statusRect.sizeDelta = Vector2.zero;
            statusRect.offsetMin = new Vector2(10, 0);
            statusRect.offsetMax = new Vector2(-10, -5);
            statusText.GetComponent<Text>().fontSize = 16;
            statusText.GetComponent<Text>().fontStyle = FontStyle.Bold;
            statusText.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
            statusText.GetComponent<Text>().color = Color.cyan;
            
            // Integrity Light
            GameObject integrityLightObj = new GameObject("IntegrityLight");
            integrityLightObj.transform.SetParent(verifierPanel.transform, false);
            Image integrityLight = integrityLightObj.AddComponent<Image>();
            integrityLight.color = Color.green;
            RectTransform lightRect = integrityLightObj.GetComponent<RectTransform>();
            lightRect.anchorMin = new Vector2(0.5f, 0);
            lightRect.anchorMax = new Vector2(0.5f, 0.5f);
            lightRect.anchoredPosition = new Vector2(0, 10);
            lightRect.sizeDelta = new Vector2(30, 30);
            
            // Component Checklist
            GameObject checklistText = CreateUIText("ComponentChecklist", verifierPanel.transform, "Checking components...");
            RectTransform checklistRect = checklistText.GetComponent<RectTransform>();
            checklistRect.anchorMin = new Vector2(0, 0);
            checklistRect.anchorMax = new Vector2(1, 0.5f);
            checklistRect.sizeDelta = Vector2.zero;
            checklistRect.offsetMin = new Vector2(10, 5);
            checklistRect.offsetMax = new Vector2(-10, 0);
            checklistText.GetComponent<Text>().fontSize = 10;
            checklistText.GetComponent<Text>().alignment = TextAnchor.UpperLeft;
            
            // Wire up verifier
            SerializedObject verifierSO = new SerializedObject(projectVerifier);
            verifierSO.FindProperty("realRobot").objectReferenceValue = realRobot;
            verifierSO.FindProperty("swarmLeader").objectReferenceValue = GameObject.Find("FleetController");
            verifierSO.FindProperty("digitalTwinCore").objectReferenceValue = rosManager;
            verifierSO.FindProperty("mapLoader").objectReferenceValue = rosManager;
            verifierSO.FindProperty("rosManager").objectReferenceValue = rosManager;
            verifierSO.FindProperty("finalStatusText").objectReferenceValue = statusText.GetComponent<Text>();
            verifierSO.FindProperty("integrityLight").objectReferenceValue = integrityLight;
            verifierSO.FindProperty("componentChecklistText").objectReferenceValue = checklistText.GetComponent<Text>();
            verifierSO.ApplyModifiedProperties();
            
            Undo.RegisterCreatedObjectUndo(verifierPanel, "Create VerifierPanel");
            
            Debug.Log("[SceneSetup] Added NavlProjectVerifier - Master scene controller ready");
        }
        
        // 44. Ironclad Evolution - Data Logger
        IroncladDataLogger dataLogger = rosManager.GetComponent<IroncladDataLogger>();
        if (dataLogger == null)
        {
            dataLogger = rosManager.AddComponent<IroncladDataLogger>();
            SerializedObject loggerSO = new SerializedObject(dataLogger);
            loggerSO.FindProperty("consciousnessRigor").objectReferenceValue = consciousnessRigor;
            loggerSO.FindProperty("vncVerifier").objectReferenceValue = vncVerifier;
            loggerSO.FindProperty("teleopController").objectReferenceValue = realRobot.GetComponent<UnityTeleopController>();
            loggerSO.ApplyModifiedProperties();
            Debug.Log("[SceneSetup] Added IroncladDataLogger - Training data collection ready");
        }
        
        // 45. Ironclad Evolution - Adaptive VLA Manager
        AdaptiveVlaManager adaptiveVla = rosManager.GetComponent<AdaptiveVlaManager>();
        if (adaptiveVla == null)
        {
            adaptiveVla = rosManager.AddComponent<AdaptiveVlaManager>();
            SerializedObject adaptiveSO = new SerializedObject(adaptiveVla);
            adaptiveSO.FindProperty("consciousnessRigor").objectReferenceValue = consciousnessRigor;
            adaptiveSO.FindProperty("modelManager").objectReferenceValue = rosManager.GetComponent<UniversalModelManager>();
            adaptiveSO.FindProperty("vlaSaliency").objectReferenceValue = rosManager.GetComponent<VlaSaliencyOverlay>();
            
            // Create training status UI
            GameObject trainingStatusText = CreateUIText("TrainingStatusText", panelParent, "LEARNING: INITIALIZING...");
            RectTransform trainingRect = trainingStatusText.GetComponent<RectTransform>();
            trainingRect.anchorMin = new Vector2(0, 0.9f);
            trainingRect.anchorMax = new Vector2(0.5f, 1);
            trainingRect.sizeDelta = Vector2.zero;
            trainingRect.offsetMin = new Vector2(10, -30);
            trainingRect.offsetMax = new Vector2(-10, -10);
            trainingStatusText.GetComponent<Text>().fontSize = 12;
            trainingStatusText.GetComponent<Text>().alignment = TextAnchor.UpperLeft;
            adaptiveSO.FindProperty("trainingStatusText").objectReferenceValue = trainingStatusText.GetComponent<Text>();
            
            // Create confidence bias text
            GameObject confidenceText = CreateUIText("ConfidenceBiasText", panelParent, "Confidence Bias: 0.000");
            RectTransform confidenceRect = confidenceText.GetComponent<RectTransform>();
            confidenceRect.anchorMin = new Vector2(0.5f, 0.9f);
            confidenceRect.anchorMax = new Vector2(1, 1);
            confidenceRect.sizeDelta = Vector2.zero;
            confidenceRect.offsetMin = new Vector2(10, -30);
            confidenceRect.offsetMax = new Vector2(-10, -10);
            confidenceText.GetComponent<Text>().fontSize = 12;
            confidenceText.GetComponent<Text>().alignment = TextAnchor.UpperRight;
            adaptiveSO.FindProperty("confidenceBiasText").objectReferenceValue = confidenceText.GetComponent<Text>();
            
            adaptiveSO.ApplyModifiedProperties();
            Debug.Log("[SceneSetup] Added AdaptiveVlaManager - Online training ready");
        }
        
        // 46. Ironclad Evolution - Causal Graph Builder
        CausalGraphBuilder causalGraph = realRobot.GetComponent<CausalGraphBuilder>();
        if (causalGraph == null)
        {
            causalGraph = realRobot.AddComponent<CausalGraphBuilder>();
            SerializedObject causalSO = new SerializedObject(causalGraph);
            causalSO.FindProperty("teleopController").objectReferenceValue = realRobot.GetComponent<UnityTeleopController>();
            causalSO.FindProperty("consciousnessRigor").objectReferenceValue = consciousnessRigor;
            causalSO.FindProperty("vncVerifier").objectReferenceValue = vncVerifier;
            causalSO.ApplyModifiedProperties();
            Debug.Log("[SceneSetup] Added CausalGraphBuilder - Causal graph certification ready");
        }
        
        // 47. Ironclad Evolution - Evolution HUD
        EvolutionHUD evolutionHUD = rosManager.GetComponent<EvolutionHUD>();
        if (evolutionHUD == null)
        {
            evolutionHUD = rosManager.AddComponent<EvolutionHUD>();
            SerializedObject evolutionSO = new SerializedObject(evolutionHUD);
            evolutionSO.FindProperty("consciousnessRigor").objectReferenceValue = consciousnessRigor;
            evolutionSO.FindProperty("dataLogger").objectReferenceValue = dataLogger;
            evolutionSO.FindProperty("adaptiveVla").objectReferenceValue = adaptiveVla;
            
            // Create heatmap
            GameObject heatmapObj = new GameObject("DataHeatmap");
            heatmapObj.transform.SetParent(panelParent, false);
            RawImage heatmap = heatmapObj.AddComponent<RawImage>();
            RectTransform heatmapRect = heatmapObj.GetComponent<RectTransform>();
            heatmapRect.anchorMin = new Vector2(0.5f, 0.5f);
            heatmapRect.anchorMax = new Vector2(0.5f, 0.5f);
            heatmapRect.anchoredPosition = new Vector2(0, 0);
            heatmapRect.sizeDelta = new Vector2(200, 200);
            evolutionSO.FindProperty("dataHeatmap").objectReferenceValue = heatmap;
            
            // Create generation count
            GameObject genCountText = CreateUIText("GenerationCount", panelParent, "DATAPOINTS: 0");
            RectTransform genRect = genCountText.GetComponent<RectTransform>();
            genRect.anchorMin = new Vector2(0.5f, 0.4f);
            genRect.anchorMax = new Vector2(0.5f, 0.4f);
            genRect.anchoredPosition = new Vector2(0, 0);
            genRect.sizeDelta = new Vector2(200, 30);
            genCountText.GetComponent<Text>().fontSize = 14;
            genCountText.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
            evolutionSO.FindProperty("generationCount").objectReferenceValue = genCountText.GetComponent<Text>();
            
            // Create training stats
            GameObject statsText = CreateUIText("TrainingStats", panelParent, "Training Stats...");
            RectTransform statsRect = statsText.GetComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0, 0.5f);
            statsRect.anchorMax = new Vector2(0.5f, 0.9f);
            statsRect.sizeDelta = Vector2.zero;
            statsRect.offsetMin = new Vector2(10, 10);
            statsRect.offsetMax = new Vector2(-10, -10);
            statsText.GetComponent<Text>().fontSize = 10;
            statsText.GetComponent<Text>().alignment = TextAnchor.UpperLeft;
            evolutionSO.FindProperty("trainingStatsText").objectReferenceValue = statsText.GetComponent<Text>();
            
            // Create success rate
            GameObject successRateText = CreateUIText("SuccessRate", panelParent, "Success Rate: 0%");
            RectTransform successRect = successRateText.GetComponent<RectTransform>();
            successRect.anchorMin = new Vector2(0.5f, 0.3f);
            successRect.anchorMax = new Vector2(0.5f, 0.3f);
            successRect.anchoredPosition = new Vector2(0, 0);
            successRect.sizeDelta = new Vector2(200, 30);
            successRateText.GetComponent<Text>().fontSize = 14;
            successRateText.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
            evolutionSO.FindProperty("successRateText").objectReferenceValue = successRateText.GetComponent<Text>();
            
            evolutionSO.ApplyModifiedProperties();
            Debug.Log("[SceneSetup] Added EvolutionHUD - Training visualization ready");
        }
        
        // 48. Adaptive Safety - Environment Profiler
        EnvironmentProfiler envProfiler = realRobot.GetComponent<EnvironmentProfiler>();
        if (envProfiler == null)
        {
            envProfiler = realRobot.AddComponent<EnvironmentProfiler>();
            SerializedObject envSO = new SerializedObject(envProfiler);
            envSO.FindProperty("navlRigor").objectReferenceValue = realRobot.GetComponent<Navl7dRigor>();
            envSO.FindProperty("consciousnessRigor").objectReferenceValue = consciousnessRigor;
            
            // Create UI for environment profiler
            GameObject terrainText = CreateUIText("TerrainStatus", panelParent, "TERRAIN: Analyzing...");
            RectTransform terrainRect = terrainText.GetComponent<RectTransform>();
            terrainRect.anchorMin = new Vector2(0, 0.8f);
            terrainRect.anchorMax = new Vector2(0.5f, 0.9f);
            terrainRect.sizeDelta = Vector2.zero;
            terrainRect.offsetMin = new Vector2(10, 0);
            terrainRect.offsetMax = new Vector2(-10, -5);
            terrainText.GetComponent<Text>().fontSize = 11;
            envSO.FindProperty("terrainStatusText").objectReferenceValue = terrainText.GetComponent<Text>();
            
            GameObject lightText = CreateUIText("LightStatus", panelParent, "SENSOR: Analyzing...");
            RectTransform lightRect = lightText.GetComponent<RectTransform>();
            lightRect.anchorMin = new Vector2(0.5f, 0.8f);
            lightRect.anchorMax = new Vector2(1, 0.9f);
            lightRect.sizeDelta = Vector2.zero;
            lightRect.offsetMin = new Vector2(10, 0);
            lightRect.offsetMax = new Vector2(-10, -5);
            lightText.GetComponent<Text>().fontSize = 11;
            envSO.FindProperty("lightStatusText").objectReferenceValue = lightText.GetComponent<Text>();
            
            envSO.ApplyModifiedProperties();
            Debug.Log("[SceneSetup] Added EnvironmentProfiler - Terrain and sensor adaptation ready");
        }
        
        // 49. Adaptive Safety - Self-Healing Safety
        SelfHealingSafety selfHealing = realRobot.GetComponent<SelfHealingSafety>();
        if (selfHealing == null)
        {
            selfHealing = realRobot.AddComponent<SelfHealingSafety>();
            SerializedObject healingSO = new SerializedObject(selfHealing);
            healingSO.FindProperty("environmentProfiler").objectReferenceValue = envProfiler;
            healingSO.FindProperty("modelManager").objectReferenceValue = rosManager.GetComponent<UniversalModelManager>();
            healingSO.FindProperty("reasoningHUD").objectReferenceValue = rosManager.GetComponent<ReasoningHUD>();
            
            // Create healing status UI
            GameObject healingText = CreateUIText("HealingStatus", panelParent, "");
            RectTransform healingRect = healingText.GetComponent<RectTransform>();
            healingRect.anchorMin = new Vector2(0, 0.7f);
            healingRect.anchorMax = new Vector2(1, 0.8f);
            healingRect.sizeDelta = Vector2.zero;
            healingRect.offsetMin = new Vector2(10, 0);
            healingRect.offsetMax = new Vector2(-10, -5);
            healingText.GetComponent<Text>().fontSize = 12;
            healingText.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
            healingSO.FindProperty("healingStatusText").objectReferenceValue = healingText.GetComponent<Text>();
            
            healingSO.ApplyModifiedProperties();
            Debug.Log("[SceneSetup] Added SelfHealingSafety - Auto-recovery system ready");
        }
        
        // 50. Adaptive Safety - Dynamic Zone Manager
        DynamicZoneManager zoneManager = realRobot.GetComponent<DynamicZoneManager>();
        if (zoneManager == null)
        {
            zoneManager = realRobot.AddComponent<DynamicZoneManager>();
            SerializedObject zoneSO = new SerializedObject(zoneManager);
            zoneSO.FindProperty("consciousnessRigor").objectReferenceValue = consciousnessRigor;
            zoneSO.FindProperty("selfHealingSafety").objectReferenceValue = selfHealing;
            zoneSO.FindProperty("vncVerifier").objectReferenceValue = vncVerifier;
            
            // Create zone status UI
            GameObject zoneText = CreateUIText("ZoneStatus", panelParent, "ZONE: Initializing...");
            RectTransform zoneRect = zoneText.GetComponent<RectTransform>();
            zoneRect.anchorMin = new Vector2(0, 0.6f);
            zoneRect.anchorMax = new Vector2(1, 0.7f);
            zoneRect.sizeDelta = Vector2.zero;
            zoneRect.offsetMin = new Vector2(10, 0);
            zoneRect.offsetMax = new Vector2(-10, -5);
            zoneText.GetComponent<Text>().fontSize = 12;
            zoneText.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
            zoneSO.FindProperty("zoneStatusText").objectReferenceValue = zoneText.GetComponent<Text>();
            
            zoneSO.ApplyModifiedProperties();
            Debug.Log("[SceneSetup] Added DynamicZoneManager - Context-aware zones ready");
        }
        
        // 51. Certification Compiler (The Notary)
        CertificationCompiler certCompiler = rosManager.GetComponent<CertificationCompiler>();
        if (certCompiler == null)
        {
            certCompiler = rosManager.AddComponent<CertificationCompiler>();
            SerializedObject certSO = new SerializedObject(certCompiler);
            certSO.FindProperty("rigorRigor").objectReferenceValue = realRobot.GetComponent<Navl7dRigor>();
            certSO.FindProperty("sim2valValidator").objectReferenceValue = rosManager.GetComponent<LiveValidator>();
            certSO.FindProperty("vncVerifier").objectReferenceValue = vncVerifier;
            certSO.FindProperty("consciousnessRigor").objectReferenceValue = consciousnessRigor;
            
            // Create compile status UI
            GameObject compileText = CreateUIText("CompileStatus", panelParent, "COMPILER: READY");
            RectTransform compileRect = compileText.GetComponent<RectTransform>();
            compileRect.anchorMin = new Vector2(0, 0.5f);
            compileRect.anchorMax = new Vector2(0.5f, 0.6f);
            compileRect.sizeDelta = Vector2.zero;
            compileRect.offsetMin = new Vector2(10, 0);
            compileRect.offsetMax = new Vector2(-10, -5);
            compileText.GetComponent<Text>().fontSize = 12;
            compileText.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
            compileText.GetComponent<Text>().fontStyle = FontStyle.Bold;
            certSO.FindProperty("compileStatusText").objectReferenceValue = compileText.GetComponent<Text>();
            
            // Create certificate count UI
            GameObject certCountText = CreateUIText("CertificateCount", panelParent, "CERTIFICATES: 0");
            RectTransform certCountRect = certCountText.GetComponent<RectTransform>();
            certCountRect.anchorMin = new Vector2(0.5f, 0.5f);
            certCountRect.anchorMax = new Vector2(1, 0.6f);
            certCountRect.sizeDelta = Vector2.zero;
            certCountRect.offsetMin = new Vector2(10, 0);
            certCountRect.offsetMax = new Vector2(-10, -5);
            certCountText.GetComponent<Text>().fontSize = 12;
            certCountText.GetComponent<Text>().alignment = TextAnchor.MiddleRight;
            certSO.FindProperty("certificateCountText").objectReferenceValue = certCountText.GetComponent<Text>();
            
            certSO.ApplyModifiedProperties();
            Debug.Log("[SceneSetup] Added CertificationCompiler - Certification notary ready");
            
            // Wire real robot reference for Rust core
            SerializedObject certSO2 = new SerializedObject(certCompiler);
            certSO2.FindProperty("realRobot").objectReferenceValue = realRobot;
            certSO2.ApplyModifiedProperties();
        }
        
        // 52. Military/NASA Grade - Secure Data Logger
        SecureDataLogger secureLogger = rosManager.GetComponent<SecureDataLogger>();
        if (secureLogger == null)
        {
            secureLogger = rosManager.AddComponent<SecureDataLogger>();
            Debug.Log("[SceneSetup] Added SecureDataLogger - AES-256 encryption ready");
        }
        
        // 53. Military/NASA Grade - Stealth Visualizer
        StealthVisualizer stealth = realRobot.GetComponent<StealthVisualizer>();
        if (stealth == null)
        {
            stealth = realRobot.AddComponent<StealthVisualizer>();
            Debug.Log("[SceneSetup] Added StealthVisualizer - Radar evasion ready");
        }
        
        // 54. Military/NASA Grade - Massive Data Scrapper
        MassiveDataScrapper dataScrapper = rosManager.GetComponent<MassiveDataScrapper>();
        if (dataScrapper == null)
        {
            dataScrapper = rosManager.AddComponent<MassiveDataScrapper>();
            
            // Create status UI
            GameObject scrapperStatusText = CreateUIText("ScrapperStatus", panelParent, "SCRAPER: Ready");
            RectTransform scrapperStatusRect = scrapperStatusText.GetComponent<RectTransform>();
            scrapperStatusRect.anchorMin = new Vector2(0, 0.4f);
            scrapperStatusRect.anchorMax = new Vector2(0.5f, 0.5f);
            scrapperStatusRect.sizeDelta = Vector2.zero;
            scrapperStatusRect.offsetMin = new Vector2(10, 0);
            scrapperStatusRect.offsetMax = new Vector2(-10, -5);
            scrapperStatusText.GetComponent<Text>().fontSize = 11;
            SerializedObject scrapperSO = new SerializedObject(dataScrapper);
            scrapperSO.FindProperty("statusText").objectReferenceValue = scrapperStatusText.GetComponent<Text>();
            scrapperSO.ApplyModifiedProperties();
            
            Debug.Log("[SceneSetup] Added MassiveDataScrapper - High-performance ingestion ready");
        }
        
        // 55. Military/NASA Grade - Quick AI Processor
        ScrapperAiProcessor aiProcessor = rosManager.GetComponent<ScrapperAiProcessor>();
        if (aiProcessor == null)
        {
            aiProcessor = rosManager.AddComponent<ScrapperAiProcessor>();
            SerializedObject processorSO = new SerializedObject(aiProcessor);
            processorSO.FindProperty("dataScrapper").objectReferenceValue = dataScrapper;
            
            // Create processor status UI
            GameObject processorStatusText = CreateUIText("ProcessorStatus", panelParent, "PROCESSOR: Ready");
            RectTransform processorStatusRect = processorStatusText.GetComponent<RectTransform>();
            processorStatusRect.anchorMin = new Vector2(0.5f, 0.4f);
            processorStatusRect.anchorMax = new Vector2(1, 0.5f);
            processorStatusRect.sizeDelta = Vector2.zero;
            processorStatusRect.offsetMin = new Vector2(10, 0);
            processorStatusRect.offsetMax = new Vector2(-10, -5);
            processorStatusText.GetComponent<Text>().fontSize = 11;
            processorSO.FindProperty("statusText").objectReferenceValue = processorStatusText.GetComponent<Text>();
            processorSO.ApplyModifiedProperties();
            
            Debug.Log("[SceneSetup] Added ScrapperAiProcessor - Fast AI training ready");
        }
        
        // 56. Military/NASA Grade - Omnipotent AI Mode
        OmnipotentAiMode omnipotent = realRobot.GetComponent<OmnipotentAiMode>();
        if (omnipotent == null)
        {
            omnipotent = realRobot.AddComponent<OmnipotentAiMode>();
            SerializedObject omnipotentSO = new SerializedObject(omnipotent);
            omnipotentSO.FindProperty("navlRigor").objectReferenceValue = realRobot.GetComponent<Navl7dRigor>();
            omnipotentSO.FindProperty("selfHealingSafety").objectReferenceValue = realRobot.GetComponent<SelfHealingSafety>();
            omnipotentSO.FindProperty("sparkVerifier").objectReferenceValue = realRobot.GetComponent<SparkTemporalVerifier>();
            omnipotentSO.FindProperty("vncVerifier").objectReferenceValue = vncVerifier;
            omnipotentSO.FindProperty("consciousnessRigor").objectReferenceValue = consciousnessRigor;
            
            // Create omnipotent status UI
            GameObject omnipotentStatusText = CreateUIText("OmnipotentStatus", panelParent, "AI MODE: RESTRICTED");
            RectTransform omnipotentStatusRect = omnipotentStatusText.GetComponent<RectTransform>();
            omnipotentStatusRect.anchorMin = new Vector2(0, 0.3f);
            omnipotentStatusRect.anchorMax = new Vector2(1, 0.4f);
            omnipotentStatusRect.sizeDelta = Vector2.zero;
            omnipotentStatusRect.offsetMin = new Vector2(10, 0);
            omnipotentStatusRect.offsetMax = new Vector2(-10, -5);
            omnipotentStatusText.GetComponent<Text>().fontSize = 14;
            omnipotentStatusText.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
            omnipotentStatusText.GetComponent<Text>().fontStyle = FontStyle.Bold;
            omnipotentStatusText.GetComponent<Text>().color = Color.cyan;
            omnipotentSO.FindProperty("statusText").objectReferenceValue = omnipotentStatusText.GetComponent<Text>();
            omnipotentSO.ApplyModifiedProperties();
            
            Debug.LogWarning("[SceneSetup] Added OmnipotentAiMode - All-access mode ready (WARNING: Bypasses all safety)");
        }
        
        // 57. Swarm-AGI - Swarm AGI Commander
        SwarmAgiCommander swarmAgi = rosManager.GetComponent<SwarmAgiCommander>();
        if (swarmAgi == null)
        {
            swarmAgi = rosManager.AddComponent<SwarmAgiCommander>();
            SerializedObject swarmAgiSO = new SerializedObject(swarmAgi);
            swarmAgiSO.FindProperty("reasoningInterface").objectReferenceValue = rosManager.GetComponent<ReasoningHUD>();
            
            // Create swarm status UI
            GameObject swarmStatusText = CreateUIText("SwarmStatus", panelParent, "SWARM-AGI: Initializing...");
            RectTransform swarmStatusRect = swarmStatusText.GetComponent<RectTransform>();
            swarmStatusRect.anchorMin = new Vector2(0, 0.2f);
            swarmStatusRect.anchorMax = new Vector2(0.5f, 0.3f);
            swarmStatusRect.sizeDelta = Vector2.zero;
            swarmStatusRect.offsetMin = new Vector2(10, 0);
            swarmStatusRect.offsetMax = new Vector2(-10, -5);
            swarmStatusText.GetComponent<Text>().fontSize = 12;
            swarmAgiSO.FindProperty("swarmStatusText").objectReferenceValue = swarmStatusText.GetComponent<Text>();
            
            // Create mission text
            GameObject missionText = CreateUIText("MissionText", panelParent, "MISSION: None");
            RectTransform missionRect = missionText.GetComponent<RectTransform>();
            missionRect.anchorMin = new Vector2(0.5f, 0.2f);
            missionRect.anchorMax = new Vector2(1, 0.3f);
            missionRect.sizeDelta = Vector2.zero;
            missionRect.offsetMin = new Vector2(10, 0);
            missionRect.offsetMax = new Vector2(-10, -5);
            missionText.GetComponent<Text>().fontSize = 12;
            swarmAgiSO.FindProperty("missionText").objectReferenceValue = missionText.GetComponent<Text>();
            
            swarmAgiSO.ApplyModifiedProperties();
            Debug.Log("[SceneSetup] Added SwarmAgiCommander - Global planning ready");
        }
        
        // 58. Swarm-AGI - Fleet Geofence
        FleetGeofence fleetGeofence = rosManager.GetComponent<FleetGeofence>();
        if (fleetGeofence == null)
        {
            fleetGeofence = rosManager.AddComponent<FleetGeofence>();
            SerializedObject geofenceSO = new SerializedObject(fleetGeofence);
            geofenceSO.FindProperty("leaderAgent").objectReferenceValue = realRobot;
            geofenceSO.ApplyModifiedProperties();
            Debug.Log("[SceneSetup] Added FleetGeofence - Waymo-style safety envelopes ready");
        }
        
        // 59. Swarm-AGI - Global Voxel Map
        GlobalVoxelMap globalVoxel = rosManager.GetComponent<GlobalVoxelMap>();
        if (globalVoxel == null)
        {
            globalVoxel = rosManager.AddComponent<GlobalVoxelMap>();
            Debug.Log("[SceneSetup] Added GlobalVoxelMap - Tesla-style occupancy network ready");
        }
        
        // 60. Swarm-AGI - Heterogeneous Model Manager
        HeterogeneousModelManager heteroModel = rosManager.GetComponent<HeterogeneousModelManager>();
        if (heteroModel == null)
        {
            heteroModel = rosManager.AddComponent<HeterogeneousModelManager>();
            
            // Create pool status UI
            GameObject poolStatusText = CreateUIText("PoolStatus", panelParent, "MODELS: Initializing...");
            RectTransform poolStatusRect = poolStatusText.GetComponent<RectTransform>();
            poolStatusRect.anchorMin = new Vector2(0, 0.1f);
            poolStatusRect.anchorMax = new Vector2(1, 0.2f);
            poolStatusRect.sizeDelta = Vector2.zero;
            poolStatusRect.offsetMin = new Vector2(10, 0);
            poolStatusRect.offsetMax = new Vector2(-10, -5);
            poolStatusText.GetComponent<Text>().fontSize = 11;
            poolStatusText.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
            SerializedObject heteroSO = new SerializedObject(heteroModel);
            heteroSO.FindProperty("poolStatusText").objectReferenceValue = poolStatusText.GetComponent<Text>();
            heteroSO.ApplyModifiedProperties();
            
            // Wire HeterogeneousModelManager to SwarmAgiCommander
            if (swarmAgi != null)
            {
                SerializedObject swarmAgiSO2 = new SerializedObject(swarmAgi);
                swarmAgiSO2.FindProperty("modelManager").objectReferenceValue = heteroModel;
                swarmAgiSO2.ApplyModifiedProperties();
            }
            
            Debug.Log("[SceneSetup] Added HeterogeneousModelManager - Dynamic model assignment ready");
        }
        
        // 61. Swarm-AGI - Swarm Certification Overlay
        SwarmCertificationOverlay swarmCert = rosManager.GetComponent<SwarmCertificationOverlay>();
        if (swarmCert == null)
        {
            swarmCert = rosManager.AddComponent<SwarmCertificationOverlay>();
            
            // Create overlay panel
            GameObject overlayPanelObj = new GameObject("SwarmOverlayPanel");
            overlayPanelObj.transform.SetParent(panelParent, false);
            CanvasGroup overlayPanel = overlayPanelObj.AddComponent<CanvasGroup>();
            overlayPanel.alpha = 0f;
            
            RectTransform overlayRect = overlayPanelObj.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;
            
            Image overlayBg = overlayPanelObj.AddComponent<Image>();
            overlayBg.color = new Color(1, 0, 0, 0.2f); // Red tint for unsafe
            
            // Create fleet status text
            GameObject fleetStatusText = CreateUIText("FleetStatus", overlayPanelObj.transform, "FLEET STATUS: Checking...");
            RectTransform fleetStatusRect = fleetStatusText.GetComponent<RectTransform>();
            fleetStatusRect.anchorMin = new Vector2(0.5f, 0.8f);
            fleetStatusRect.anchorMax = new Vector2(0.5f, 0.8f);
            fleetStatusRect.anchoredPosition = Vector2.zero;
            fleetStatusRect.sizeDelta = new Vector2(400, 100);
            fleetStatusText.GetComponent<Text>().fontSize = 16;
            fleetStatusText.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
            fleetStatusText.GetComponent<Text>().fontStyle = FontStyle.Bold;
            
            // Create distribution text
            GameObject distributionText = CreateUIText("DistributionText", overlayPanelObj.transform, "P-SCORE DISTRIBUTION:");
            RectTransform distRect = distributionText.GetComponent<RectTransform>();
            distRect.anchorMin = new Vector2(0.5f, 0.6f);
            distRect.anchorMax = new Vector2(0.5f, 0.6f);
            distRect.anchoredPosition = Vector2.zero;
            distRect.sizeDelta = new Vector2(400, 50);
            distributionText.GetComponent<Text>().fontSize = 12;
            distributionText.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
            
            SerializedObject swarmCertSO = new SerializedObject(swarmCert);
            swarmCertSO.FindProperty("overlayPanel").objectReferenceValue = overlayPanel;
            swarmCertSO.FindProperty("fleetStatusText").objectReferenceValue = fleetStatusText.GetComponent<Text>();
            swarmCertSO.FindProperty("distributionText").objectReferenceValue = distributionText.GetComponent<Text>();
            swarmCertSO.ApplyModifiedProperties();
            
            Undo.RegisterCreatedObjectUndo(overlayPanelObj, "Create SwarmOverlayPanel");
            
            Debug.Log("[SceneSetup] Added SwarmCertificationOverlay - Fleet certification view ready");
        }
        
        // 63. Dual-Mode Platform - Fleet Discovery Manager (Production)
        FleetDiscoveryManager fleetDiscovery = rosManager.GetComponent<FleetDiscoveryManager>();
        if (fleetDiscovery == null)
        {
            fleetDiscovery = rosManager.AddComponent<FleetDiscoveryManager>();
            
            // Create fleet count UI
            GameObject fleetCountText = CreateUIText("FleetCount", panelParent, "FLEET: 0/0 ACTIVE");
            RectTransform fleetCountRect = fleetCountText.GetComponent<RectTransform>();
            fleetCountRect.anchorMin = new Vector2(0, 0.05f);
            fleetCountRect.anchorMax = new Vector2(0.5f, 0.1f);
            fleetCountRect.sizeDelta = Vector2.zero;
            fleetCountRect.offsetMin = new Vector2(10, 0);
            fleetCountRect.offsetMax = new Vector2(-10, -5);
            fleetCountText.GetComponent<Text>().fontSize = 11;
            SerializedObject fleetSO = new SerializedObject(fleetDiscovery);
            fleetSO.FindProperty("fleetCountText").objectReferenceValue = fleetCountText.GetComponent<Text>();
            
            // Create discovery status
            GameObject discoveryStatusText = CreateUIText("DiscoveryStatus", panelParent, "DISCOVERY: Initializing...");
            RectTransform discoveryStatusRect = discoveryStatusText.GetComponent<RectTransform>();
            discoveryStatusRect.anchorMin = new Vector2(0.5f, 0.05f);
            discoveryStatusRect.anchorMax = new Vector2(1, 0.1f);
            discoveryStatusRect.sizeDelta = Vector2.zero;
            discoveryStatusRect.offsetMin = new Vector2(10, 0);
            discoveryStatusRect.offsetMax = new Vector2(-10, -5);
            discoveryStatusText.GetComponent<Text>().fontSize = 11;
            fleetSO.FindProperty("discoveryStatusText").objectReferenceValue = discoveryStatusText.GetComponent<Text>();
            fleetSO.ApplyModifiedProperties();
            
            Debug.Log("[SceneSetup] Added FleetDiscoveryManager - Production fleet discovery ready");
        }
        
        // 64. Dual-Mode Platform - Mission Profile System (Production)
        MissionProfileSystem missionProfile = rosManager.GetComponent<MissionProfileSystem>();
        if (missionProfile == null)
        {
            missionProfile = rosManager.AddComponent<MissionProfileSystem>();
            Debug.Log("[SceneSetup] Added MissionProfileSystem - Production configuration ready");
        }
        
        // 65. Dual-Mode Platform - Security Portal (Production)
        SecurityPortal securityPortal = rosManager.GetComponent<SecurityPortal>();
        if (securityPortal == null)
        {
            securityPortal = rosManager.AddComponent<SecurityPortal>();
            
            // Create lock screen
            GameObject lockScreen = new GameObject("LockScreen");
            lockScreen.transform.SetParent(panelParent, false);
            Image lockBg = lockScreen.AddComponent<Image>();
            lockBg.color = new Color(0, 0, 0, 0.8f);
            RectTransform lockRect = lockScreen.GetComponent<RectTransform>();
            lockRect.anchorMin = Vector2.zero;
            lockRect.anchorMax = Vector2.one;
            lockRect.sizeDelta = Vector2.zero;
            
            // Create login panel
            GameObject loginPanel = new GameObject("LoginPanel");
            loginPanel.transform.SetParent(lockScreen.transform, false);
            Image loginBg = loginPanel.AddComponent<Image>();
            loginBg.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            RectTransform loginRect = loginPanel.GetComponent<RectTransform>();
            loginRect.anchorMin = new Vector2(0.5f, 0.5f);
            loginRect.anchorMax = new Vector2(0.5f, 0.5f);
            loginRect.anchoredPosition = Vector2.zero;
            loginRect.sizeDelta = new Vector2(400, 200);
            
            // Create auth status text
            GameObject authStatusText = CreateUIText("AuthStatus", loginPanel.transform, "AUTH: Login required");
            RectTransform authStatusRect = authStatusText.GetComponent<RectTransform>();
            authStatusRect.anchorMin = new Vector2(0, 0.5f);
            authStatusRect.anchorMax = new Vector2(1, 1);
            authStatusRect.sizeDelta = Vector2.zero;
            authStatusRect.offsetMin = new Vector2(10, 0);
            authStatusRect.offsetMax = new Vector2(-10, -10);
            authStatusText.GetComponent<Text>().fontSize = 14;
            authStatusText.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
            
            SerializedObject securitySO = new SerializedObject(securityPortal);
            securitySO.FindProperty("lockScreen").objectReferenceValue = lockScreen;
            securitySO.FindProperty("loginPanel").objectReferenceValue = loginPanel;
            securitySO.FindProperty("authStatusText").objectReferenceValue = authStatusText.GetComponent<Text>();
            securitySO.ApplyModifiedProperties();
            
            Undo.RegisterCreatedObjectUndo(lockScreen, "Create LockScreen");
            
            Debug.Log("[SceneSetup] Added SecurityPortal - Production authentication ready");
        }
        
        // 66. Dual-Mode Platform - Real-Time Analytics Hub (Production)
        RealTimeAnalyticsHub analyticsHub = rosManager.GetComponent<RealTimeAnalyticsHub>();
        if (analyticsHub == null)
        {
            analyticsHub = rosManager.AddComponent<RealTimeAnalyticsHub>();
            
            // Create metrics text
            GameObject metricsText = CreateUIText("MetricsText", panelParent, "FLEET METRICS: Initializing...");
            RectTransform metricsRect = metricsText.GetComponent<RectTransform>();
            metricsRect.anchorMin = new Vector2(0, 0);
            metricsRect.anchorMax = new Vector2(0.5f, 0.05f);
            metricsRect.sizeDelta = Vector2.zero;
            metricsRect.offsetMin = new Vector2(10, 0);
            metricsRect.offsetMax = new Vector2(-10, -5);
            metricsText.GetComponent<Text>().fontSize = 10;
            SerializedObject analyticsSO = new SerializedObject(analyticsHub);
            analyticsSO.FindProperty("metricsText").objectReferenceValue = metricsText.GetComponent<Text>();
            analyticsSO.ApplyModifiedProperties();
            
            Debug.Log("[SceneSetup] Added RealTimeAnalyticsHub - Production analytics ready");
        }
        
        // 67. Dual-Mode Platform - Video Streamer (Production)
        VideoStreamer videoStreamer = rosManager.GetComponent<VideoStreamer>();
        if (videoStreamer == null)
        {
            videoStreamer = rosManager.AddComponent<VideoStreamer>();
            Debug.Log("[SceneSetup] Added VideoStreamer - Production telepresence ready");
        }
        
        // 68. Dual-Mode Platform - Academic Session Recorder (Academia)
        AcademicSessionRecorder sessionRecorder = rosManager.GetComponent<AcademicSessionRecorder>();
        if (sessionRecorder == null)
        {
            sessionRecorder = rosManager.AddComponent<AcademicSessionRecorder>();
            Debug.Log("[SceneSetup] Added AcademicSessionRecorder - Academia session logging ready");
        }
        
        // 69. Dual-Mode Platform - Lecture Annotation Tool (Academia)
        LectureAnnotationTool annotationTool = rosManager.GetComponent<LectureAnnotationTool>();
        if (annotationTool == null)
        {
            annotationTool = rosManager.AddComponent<LectureAnnotationTool>();
            Debug.Log("[SceneSetup] Added LectureAnnotationTool - Academia annotation ready");
        }
        
        // 70. Dual-Mode Platform - Experiment Workflow Controller (Academia)
        ExperimentWorkflowController workflowController = rosManager.GetComponent<ExperimentWorkflowController>();
        if (workflowController == null)
        {
            workflowController = rosManager.AddComponent<ExperimentWorkflowController>();
            
            // Create experiment status text
            GameObject experimentStatusText = CreateUIText("ExperimentStatus", panelParent, "EXPERIMENT: STOPPED");
            RectTransform experimentStatusRect = experimentStatusText.GetComponent<RectTransform>();
            experimentStatusRect.anchorMin = new Vector2(0.5f, 0);
            experimentStatusRect.anchorMax = new Vector2(1, 0.05f);
            experimentStatusRect.sizeDelta = Vector2.zero;
            experimentStatusRect.offsetMin = new Vector2(10, 0);
            experimentStatusRect.offsetMax = new Vector2(-10, -5);
            experimentStatusText.GetComponent<Text>().fontSize = 10;
            SerializedObject workflowSO = new SerializedObject(workflowController);
            workflowSO.FindProperty("experimentStatusText").objectReferenceValue = experimentStatusText.GetComponent<Text>();
            workflowSO.FindProperty("sessionRecorder").objectReferenceValue = sessionRecorder;
            workflowSO.ApplyModifiedProperties();
            
            Debug.Log("[SceneSetup] Added ExperimentWorkflowController - Academia workflow ready");
        }
        
        // 71. Dual-Mode Platform - Dual Mode Manager (Master Controller)
        DualModeManager dualMode = rosManager.GetComponent<DualModeManager>();
        if (dualMode == null)
        {
            dualMode = rosManager.AddComponent<DualModeManager>();
            
            // Create mode status text
            GameObject modeStatusText = CreateUIText("ModeStatus", panelParent, "MODE: ACADEMIA");
            RectTransform modeStatusRect = modeStatusText.GetComponent<RectTransform>();
            modeStatusRect.anchorMin = new Vector2(0.5f, 0.95f);
            modeStatusRect.anchorMax = new Vector2(0.5f, 0.95f);
            modeStatusRect.anchoredPosition = Vector2.zero;
            modeStatusRect.sizeDelta = new Vector2(200, 30);
            modeStatusText.GetComponent<Text>().fontSize = 14;
            modeStatusText.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
            modeStatusText.GetComponent<Text>().fontStyle = FontStyle.Bold;
            modeStatusText.GetComponent<Text>().color = Color.cyan;
            
            SerializedObject dualModeSO = new SerializedObject(dualMode);
            dualModeSO.FindProperty("modeStatusText").objectReferenceValue = modeStatusText.GetComponent<Text>();
            dualModeSO.FindProperty("academiaComponents").arraySize = 3;
            dualModeSO.FindProperty("academiaComponents").GetArrayElementAtIndex(0).objectReferenceValue = sessionRecorder;
            dualModeSO.FindProperty("academiaComponents").GetArrayElementAtIndex(1).objectReferenceValue = annotationTool;
            dualModeSO.FindProperty("academiaComponents").GetArrayElementAtIndex(2).objectReferenceValue = workflowController;
            dualModeSO.FindProperty("productionComponents").arraySize = 4;
            dualModeSO.FindProperty("productionComponents").GetArrayElementAtIndex(0).objectReferenceValue = fleetDiscovery;
            dualModeSO.FindProperty("productionComponents").GetArrayElementAtIndex(1).objectReferenceValue = securityPortal;
            dualModeSO.FindProperty("productionComponents").GetArrayElementAtIndex(2).objectReferenceValue = analyticsHub;
            dualModeSO.FindProperty("productionComponents").GetArrayElementAtIndex(3).objectReferenceValue = videoStreamer;
            dualModeSO.ApplyModifiedProperties();
            
            Debug.Log("[SceneSetup] Added DualModeManager - Dual-mode platform ready");
        }
        
        // 72. Universal Architecture - Model Manager
        UniversalModelManager modelManager = rosManager.GetComponent<UniversalModelManager>();
        if (modelManager == null)
        {
            modelManager = rosManager.AddComponent<UniversalModelManager>();
            Debug.Log("[SceneSetup] Added UniversalModelManager - Model switching ready");
        }
        
        // 19. Universal Architecture - Reasoning HUD
        GameObject reasoningPanel = null;
        Transform reasoningTransform = panelParent.Find("ReasoningPanel");
        if (reasoningTransform != null)
        {
            reasoningPanel = reasoningTransform.gameObject;
        }
        if (reasoningPanel == null)
        {
            reasoningPanel = new GameObject("ReasoningPanel");
            reasoningPanel.transform.SetParent(panelParent, false);
            
            RectTransform reasoningRect = reasoningPanel.GetComponent<RectTransform>();
            reasoningRect.anchorMin = new Vector2(1, 0);
            reasoningRect.anchorMax = new Vector2(1, 0);
            reasoningRect.anchoredPosition = new Vector2(-10, 10);
            reasoningRect.sizeDelta = new Vector2(400, 300);
            
            Image panelBg = reasoningPanel.AddComponent<Image>();
            panelBg.color = new Color(0, 0, 0, 0.8f);
            
            CanvasGroup reasoningGroup = reasoningPanel.AddComponent<CanvasGroup>();
            reasoningGroup.alpha = 0f;
            reasoningPanel.SetActive(false);
            
            // Thought Log
            GameObject thoughtLog = CreateUIText("ThoughtLog", reasoningPanel.transform, "Reasoning HUD Ready...\n");
            RectTransform logRect = thoughtLog.GetComponent<RectTransform>();
            logRect.anchorMin = new Vector2(0, 0.3f);
            logRect.anchorMax = new Vector2(1, 1);
            logRect.sizeDelta = Vector2.zero;
            logRect.offsetMin = new Vector2(10, 10);
            logRect.offsetMax = new Vector2(-10, -10);
            thoughtLog.GetComponent<Text>().fontSize = 11;
            thoughtLog.GetComponent<Text>().alignment = TextAnchor.UpperLeft;
            
            // Confidence Bar
            GameObject confidenceBarObj = new GameObject("ConfidenceBar");
            confidenceBarObj.transform.SetParent(reasoningPanel.transform, false);
            Image confidenceBar = confidenceBarObj.AddComponent<Image>();
            confidenceBar.color = Color.green;
            
            RectTransform barRect = confidenceBarObj.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0, 0.2f);
            barRect.anchorMax = new Vector2(1, 0.3f);
            barRect.sizeDelta = Vector2.zero;
            barRect.offsetMin = new Vector2(10, 0);
            barRect.offsetMax = new Vector2(-10, 0);
            
            // Confidence Text
            GameObject confidenceText = CreateUIText("ConfidenceText", reasoningPanel.transform, "Confidence: 100%");
            RectTransform confTextRect = confidenceText.GetComponent<RectTransform>();
            confTextRect.anchorMin = new Vector2(0, 0);
            confTextRect.anchorMax = new Vector2(1, 0.2f);
            confTextRect.sizeDelta = Vector2.zero;
            confTextRect.offsetMin = new Vector2(10, 0);
            confTextRect.offsetMax = new Vector2(-10, 0);
            confidenceText.GetComponent<Text>().fontSize = 12;
            
            // Add ReasoningHUD component
            ReasoningHUD reasoningHUD = rosManager.GetComponent<ReasoningHUD>();
            if (reasoningHUD == null)
            {
                reasoningHUD = rosManager.AddComponent<ReasoningHUD>();
            }
            SerializedObject reasoningSO = new SerializedObject(reasoningHUD);
            reasoningSO.FindProperty("thoughtLog").objectReferenceValue = thoughtLog.GetComponent<Text>();
            reasoningSO.FindProperty("confidenceBar").objectReferenceValue = confidenceBar;
            reasoningSO.FindProperty("confidenceText").objectReferenceValue = confidenceText.GetComponent<Text>();
            reasoningSO.ApplyModifiedProperties();
            
            // Link to model manager
            SerializedObject modelSO = new SerializedObject(modelManager);
            modelSO.FindProperty("reasoningHUD").objectReferenceValue = reasoningPanel;
            modelSO.ApplyModifiedProperties();
            
            Undo.RegisterCreatedObjectUndo(reasoningPanel, "Create ReasoningPanel");
        }
        
        // 20. Universal Architecture - Digital Twin Physics
        DigitalTwinPhysics digitalTwin = realRobot.GetComponent<DigitalTwinPhysics>();
        if (digitalTwin == null)
        {
            digitalTwin = realRobot.AddComponent<DigitalTwinPhysics>();
            Debug.Log("[SceneSetup] Added DigitalTwinPhysics - Heterogeneous physics ready");
        }
        
        // 21. Universal Architecture - Synaptic Visualizer
        SynapticFireVisualizer synaptic = rosManager.GetComponent<SynapticFireVisualizer>();
        if (synaptic == null)
        {
            synaptic = rosManager.AddComponent<SynapticFireVisualizer>();
            Debug.Log("[SceneSetup] Added SynapticFireVisualizer - Model switching visualization ready");
        }
        
        // Link synaptic visualizer to model manager
        SerializedObject modelManagerSO = new SerializedObject(modelManager);
        modelManagerSO.FindProperty("synapticVisualizer").objectReferenceValue = synaptic;
        modelManagerSO.ApplyModifiedProperties();
    }

    static GameObject CreateUIText(string name, Transform parent, string text)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);
        Text textComponent = textObj.AddComponent<Text>();
        textComponent.text = text;
        textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textComponent.fontSize = 14;
        textComponent.color = Color.white;
        return textObj;
    }
}
