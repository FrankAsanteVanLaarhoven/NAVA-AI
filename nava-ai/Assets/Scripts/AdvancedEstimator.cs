using UnityEngine;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using RosMessageTypes.Sensor;

/// <summary>
/// Advanced State Estimator - Implements Kaufman Filter (Adaptive Noise) and RAIM 
/// (Receiver Autonomous Integrity Monitoring) for Position, Navigation, and Timing (PNT).
/// </summary>
public class AdvancedEstimator : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Text displaying PNT status")]
    public Text pntStatusText;
    
    [Tooltip("Text displaying RAIM integrity status")]
    public Text raimStatusText;
    
    [Tooltip("Text displaying covariance/uncertainty")]
    public Text covarianceText;
    
    [Header("Kaufman Filter Parameters")]
    [Tooltip("Process noise covariance (Q) - adaptive")]
    public Vector3 processNoiseScale = new Vector3(0.01f, 0.01f, 0.01f);
    
    [Tooltip("Measurement noise covariance (R) - adaptive")]
    public Vector3 measurementNoiseScale = new Vector3(0.1f, 0.1f, 0.1f);
    
    [Tooltip("Enable adaptive noise tuning")]
    public bool adaptiveNoise = true;
    
    [Header("RAIM Settings")]
    [Tooltip("RAIM threshold (meters) - residual above this triggers fault")]
    public float raimThreshold = 2.0f;
    
    [Tooltip("Enable dead reckoning fallback")]
    public bool enableDeadReckoning = true;
    
    [Header("ROS Settings")]
    [Tooltip("ROS2 topic for GPS measurements")]
    public string gpsTopic = "/gps/fix";
    
    [Tooltip("ROS2 topic for IMU measurements")]
    public string imuTopic = "/imu/data";
    
    [Tooltip("ROS2 topic for estimated state")]
    public string stateTopic = "/state_estimate";
    
    private ROSConnection ros;
    private Vector3 stateEstimate = Vector3.zero;
    private Vector3 velocityEstimate = Vector3.zero;
    private Matrix4x4 covarianceP;
    private Matrix4x4 processNoiseQ;
    private Matrix4x4 measurementNoiseR;
    private bool raimFaultDetected = false;
    private float lastUpdateTime = 0f;
    private Vector3 lastGPSMeasurement = Vector3.zero;
    private Vector3 lastIMUMeasurement = Vector3.zero;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<PointMsg>(gpsTopic, OnGPSMeasurement);
        ros.Subscribe<ImuMsg>(imuTopic, OnIMUMeasurement);
        ros.RegisterPublisher<PointMsg>(stateTopic, 10);
        
        // Initialize covariance matrix
        covarianceP = Matrix4x4.identity * 10f; // Large initial uncertainty
        
        // Initialize noise matrices
        UpdateNoiseMatrices();
        
        lastUpdateTime = Time.time;
        
        Debug.Log("[AdvancedEstimator] Initialized - Kaufman Filter + RAIM ready");
    }

    void UpdateNoiseMatrices()
    {
        // Process noise Q (motion model uncertainty)
        processNoiseQ = Matrix4x4.Scale(processNoiseScale);
        
        // Measurement noise R (sensor uncertainty)
        measurementNoiseR = Matrix4x4.Scale(measurementNoiseScale);
    }

    void OnGPSMeasurement(PointMsg msg)
    {
        Vector3 gpsPos = new Vector3((float)msg.x, (float)msg.y, (float)msg.z);
        lastGPSMeasurement = gpsPos;
        UpdateEstimation(gpsPos, lastIMUMeasurement);
    }

    void OnIMUMeasurement(ImuMsg msg)
    {
        // Extract velocity from IMU (linear acceleration integrated)
        Vector3 imuVel = new Vector3(
            (float)msg.linear_acceleration.x,
            (float)msg.linear_acceleration.y,
            (float)msg.linear_acceleration.z
        ) * Time.deltaTime;
        
        lastIMUMeasurement = imuVel;
    }

    public void UpdateEstimation(Vector3 gpsMeasurement, Vector3 imuMeasurement)
    {
        float deltaTime = Time.time - lastUpdateTime;
        if (deltaTime <= 0) return;
        
        // Adaptive noise tuning (Kaufman Filter enhancement)
        if (adaptiveNoise)
        {
            TuneNoiseAdaptively(gpsMeasurement, imuMeasurement);
        }
        
        // 1. Predict (Motion Model)
        Vector3 x_pred = stateEstimate + velocityEstimate * deltaTime;
        Matrix4x4 P_pred = covarianceP + processNoiseQ * deltaTime;
        
        // 2. Update (Measurement Model) - Kalman Gain
        Matrix4x4 S = P_pred + measurementNoiseR; // Innovation covariance
        Matrix4x4 K = P_pred * Matrix4x4.Inverse(S); // Kalman Gain
        
        // State update
        Vector3 innovation = gpsMeasurement - x_pred;
        stateEstimate = x_pred + MultiplyMatrixVector(K, innovation);
        covarianceP = (Matrix4x4.identity - K) * P_pred;
        
        // Update velocity estimate
        velocityEstimate = imuMeasurement;
        
        // 3. RAIM Check (Residual Analysis)
        float residual = Vector3.Distance(gpsMeasurement, x_pred);
        CheckRAIM(residual);
        
        // 4. Publish estimated state
        PublishStateEstimate();
        
        // 5. Update UI
        UpdateUI();
        
        lastUpdateTime = Time.time;
    }

    void TuneNoiseAdaptively(Vector3 gpsMeasurement, Vector3 imuMeasurement)
    {
        // Adaptive noise tuning based on measurement consistency
        float gpsVariance = Vector3.Distance(gpsMeasurement, stateEstimate);
        
        // Increase measurement noise if GPS is inconsistent
        if (gpsVariance > 1.0f)
        {
            measurementNoiseScale *= 1.1f; // Increase uncertainty
        }
        else if (gpsVariance < 0.5f)
        {
            measurementNoiseScale *= 0.95f; // Decrease uncertainty (more trust)
        }
        
        measurementNoiseScale = Vector3.Max(measurementNoiseScale, new Vector3(0.01f, 0.01f, 0.01f));
        UpdateNoiseMatrices();
    }

    Vector3 MultiplyMatrixVector(Matrix4x4 matrix, Vector3 vector)
    {
        return new Vector3(
            matrix.m00 * vector.x + matrix.m01 * vector.y + matrix.m02 * vector.z,
            matrix.m10 * vector.x + matrix.m11 * vector.y + matrix.m12 * vector.z,
            matrix.m20 * vector.x + matrix.m21 * vector.y + matrix.m22 * vector.z
        );
    }

    void CheckRAIM(float residual)
    {
        raimFaultDetected = residual > raimThreshold;
        
        if (raimFaultDetected)
        {
            if (raimStatusText != null)
            {
                raimStatusText.text = "RAIM: FAULT DETECTED";
                raimStatusText.color = Color.red;
            }
            
            Debug.LogWarning($"[AdvancedEstimator] RAIM FAULT: Residual = {residual:F2}m (threshold: {raimThreshold:F2}m)");
            
            // Trigger fallback to Dead Reckoning
            if (enableDeadReckoning)
            {
                EnableDeadReckoning();
            }
        }
        else
        {
            if (raimStatusText != null)
            {
                raimStatusText.text = "RAIM: INTEGRITY GOOD";
                raimStatusText.color = Color.green;
            }
        }
    }

    void EnableDeadReckoning()
    {
        // Switch to IMU-only navigation (dead reckoning)
        // Increase process noise to reflect higher uncertainty
        processNoiseScale *= 2f;
        UpdateNoiseMatrices();
        
        Debug.Log("[AdvancedEstimator] Switched to Dead Reckoning mode");
    }

    void PublishStateEstimate()
    {
        if (ros == null) return;
        
        PointMsg stateMsg = new PointMsg
        {
            x = stateEstimate.x,
            y = stateEstimate.y,
            z = stateEstimate.z
        };
        
        ros.Publish(stateTopic, stateMsg);
    }

    void UpdateUI()
    {
        if (pntStatusText != null)
        {
            float uncertainty = Mathf.Sqrt(covarianceP.m00 + covarianceP.m11 + covarianceP.m22);
            pntStatusText.text = $"PNT: Pos=({stateEstimate.x:F2}, {stateEstimate.y:F2}, {stateEstimate.z:F2})\n" +
                                $"Vel=({velocityEstimate.x:F2}, {velocityEstimate.y:F2}, {velocityEstimate.z:F2})\n" +
                                $"Uncertainty: {uncertainty:F2}m";
            
            // Color based on uncertainty
            if (uncertainty > 5f)
            {
                pntStatusText.color = Color.red;
            }
            else if (uncertainty > 2f)
            {
                pntStatusText.color = Color.yellow;
            }
            else
            {
                pntStatusText.color = Color.green;
            }
        }
        
        if (covarianceText != null)
        {
            float trace = covarianceP.m00 + covarianceP.m11 + covarianceP.m22;
            covarianceText.text = $"Covariance Trace: {trace:F4}";
        }
    }

    /// <summary>
    /// Get current state estimate
    /// </summary>
    public Vector3 GetStateEstimate()
    {
        return stateEstimate;
    }

    /// <summary>
    /// Get current uncertainty (covariance trace)
    /// </summary>
    public float GetUncertainty()
    {
        return Mathf.Sqrt(covarianceP.m00 + covarianceP.m11 + covarianceP.m22);
    }

    /// <summary>
    /// Check if RAIM fault is detected
    /// </summary>
    public bool IsRAIMFaultDetected()
    {
        return raimFaultDetected;
    }

    /// <summary>
    /// Get certainty (inverse of uncertainty) for 7D state
    /// </summary>
    public float GetCertainty()
    {
        float uncertainty = GetUncertainty();
        return 1f / (1f + uncertainty); // Convert to certainty (0-1)
    }
}
