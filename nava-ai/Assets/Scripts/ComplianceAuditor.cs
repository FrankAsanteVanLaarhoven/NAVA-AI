using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Text;
using System;
using System.Collections.Generic;

/// <summary>
/// Compliance Auditor - Automated generation of Compliance Reports (PDF/HTML) for legal/insurance validation.
/// Generates ISO 26262 Safety Integrity Level (SIL) reports.
/// </summary>
public class ComplianceAuditor : MonoBehaviour
{
    [Header("Report Settings")]
    [Tooltip("Company name")]
    public string companyName = "NAVA SOTA LABS";
    
    [Tooltip("Safety standard")]
    public string standard = "ISO 26262";
    
    [Tooltip("Safety Integrity Level")]
    public string safetyLevel = "SIL-2";
    
    [Tooltip("Report output directory")]
    public string reportDirectory = "Assets/Reports";
    
    [Header("Component References")]
    [Tooltip("Reference to VNC 7D verifier")]
    public Vnc7dVerifier vncVerifier;
    
    [Tooltip("Reference to SPARK verifier")]
    public SparkTemporalVerifier sparkVerifier;
    
    [Tooltip("Reference to Universal HAL")]
    public UniversalHal universalHal;
    
    [Tooltip("Reference to Consciousness Rigor")]
    public NavlConsciousnessRigor consciousnessRigor;
    
    [Tooltip("Reference to Live Validator")]
    public LiveValidator liveValidator;
    
    [Header("Audit Checks")]
    [Tooltip("Enable VNC rigor check")]
    public bool checkVncRigor = true;
    
    [Tooltip("Enable HAL safety check")]
    public bool checkHalSafety = true;
    
    [Tooltip("Enable SPARK temporal check")]
    public bool checkSparkLogic = true;
    
    [Tooltip("Enable cognitive safety check")]
    public bool checkCognitiveSafety = true;
    
    [Tooltip("Enable Sim2Val check")]
    public bool checkSim2Val = true;
    
    private List<AuditResult> auditResults = new List<AuditResult>();

    [System.Serializable]
    public class AuditResult
    {
        public string checkName;
        public bool passed;
        public string details;
        public float score;
    }

    void Start()
    {
        // Get component references if not assigned
        if (vncVerifier == null)
        {
            vncVerifier = FindObjectOfType<Vnc7dVerifier>();
        }
        
        if (sparkVerifier == null)
        {
            sparkVerifier = FindObjectOfType<SparkTemporalVerifier>();
        }
        
        if (universalHal == null)
        {
            universalHal = FindObjectOfType<UniversalHal>();
        }
        
        if (consciousnessRigor == null)
        {
            consciousnessRigor = FindObjectOfType<NavlConsciousnessRigor>();
        }
        
        if (liveValidator == null)
        {
            liveValidator = FindObjectOfType<LiveValidator>();
        }
        
        Debug.Log("[ComplianceAuditor] Initialized - ISO 26262 compliance ready");
    }

    /// <summary>
    /// Generate ISO 26262 Compliance Report
    /// </summary>
    [ContextMenu("Generate ISO 26262 Compliance Report")]
    public void GenerateReport()
    {
        auditResults.Clear();
        
        // Run all audit checks
        if (checkVncRigor)
        {
            AuditResult result = CheckVncRigor();
            auditResults.Add(result);
        }
        
        if (checkHalSafety)
        {
            AuditResult result = CheckHalSafety();
            auditResults.Add(result);
        }
        
        if (checkSparkLogic)
        {
            AuditResult result = CheckSparkLogic();
            auditResults.Add(result);
        }
        
        if (checkCognitiveSafety)
        {
            AuditResult result = CheckCognitiveSafety();
            auditResults.Add(result);
        }
        
        if (checkSim2Val)
        {
            AuditResult result = CheckSim2Val();
            auditResults.Add(result);
        }
        
        // Generate report
        string report = GenerateReportText();
        
        // Save report
        SaveReport(report);
        
        Debug.Log("[ComplianceAuditor] Report generated successfully");
    }

    AuditResult CheckVncRigor()
    {
        bool passed = false;
        string details = "";
        float score = 0f;
        
        if (vncVerifier != null)
        {
            passed = vncVerifier.IsCertifiedSafe();
            float barrierValue = vncVerifier.GetBarrierValue();
            score = Mathf.Clamp01(barrierValue);
            details = $"Barrier Value: {barrierValue:F3}, Certified: {passed}";
        }
        else
        {
            details = "VNC Verifier not found";
        }
        
        return new AuditResult
        {
            checkName = "VNC 7D Rigor (Control Barrier Function)",
            passed = passed,
            details = details,
            score = score
        };
    }

    AuditResult CheckHalSafety()
    {
        bool passed = false;
        string details = "";
        float score = 0f;
        
        if (universalHal != null)
        {
            var profile = universalHal.GetActiveProfile();
            if (profile != null)
            {
                passed = profile.IsHealthy();
                float battery = profile.GetBattery();
                score = battery / 100f;
                details = $"Hardware: {profile.GetHardwareName()}, Battery: {battery:F1}%, Healthy: {passed}";
            }
            else
            {
                details = "No hardware profile active";
            }
        }
        else
        {
            details = "Universal HAL not found";
        }
        
        return new AuditResult
        {
            checkName = "HAL Safety (Hardware Abstraction)",
            passed = passed,
            details = details,
            score = score
        };
    }

    AuditResult CheckSparkLogic()
    {
        bool passed = false;
        string details = "";
        float score = 1f;
        
        if (sparkVerifier != null)
        {
            passed = !sparkVerifier.HasActiveViolation();
            var violations = sparkVerifier.GetViolationHistory();
            int violationCount = violations.Count;
            score = Mathf.Clamp01(1f - (violationCount * 0.1f));
            details = $"Active Violations: {violationCount}, Zones Monitored: {sparkVerifier.zones.Count}";
        }
        else
        {
            details = "SPARK Verifier not found";
        }
        
        return new AuditResult
        {
            checkName = "SPARK Temporal Logic (Sequence Safety)",
            passed = passed,
            details = details,
            score = score
        };
    }

    AuditResult CheckCognitiveSafety()
    {
        bool passed = false;
        string details = "";
        float score = 0f;
        
        if (consciousnessRigor != null)
        {
            float pScore = consciousnessRigor.GetPScore();
            float threshold = consciousnessRigor.safetyThreshold;
            passed = pScore >= threshold;
            score = Mathf.Clamp01(pScore / (threshold * 2f));
            details = $"P-Score: {pScore:F2}, Threshold: {threshold}, Goal: {consciousnessRigor.GetGoalProximity():F2}, " +
                     $"Intent: {consciousnessRigor.GetModelIntent():F2}, Consciousness: {consciousnessRigor.GetConsciousness():F2}";
        }
        else
        {
            details = "Consciousness Rigor not found";
        }
        
        return new AuditResult
        {
            checkName = "Cognitive Safety (Goal + Intent + Consciousness)",
            passed = passed,
            details = details,
            score = score
        };
    }

    AuditResult CheckSim2Val()
    {
        bool passed = false;
        string details = "";
        float score = 0f;
        
        if (liveValidator != null)
        {
            passed = !liveValidator.IsUncertain();
            float confidence = liveValidator.GetSVRConfidence();
            score = confidence;
            float failureRate = liveValidator.GetFailureRate();
            details = $"SVR Confidence: {confidence:P1}, Failure Rate: {failureRate:E}, Uncertain: {liveValidator.IsUncertain()}";
        }
        else
        {
            details = "Live Validator not found";
        }
        
        return new AuditResult
        {
            checkName = "Sim2Val (Simulation-to-Validation)",
            passed = passed,
            details = details,
            score = score
        };
    }

    string GenerateReportText()
    {
        StringBuilder sb = new StringBuilder();
        
        // Header
        sb.AppendLine("=".PadRight(80, '='));
        sb.AppendLine($"{standard} SAFETY INTEGRITY REPORT");
        sb.AppendLine("=".PadRight(80, '='));
        sb.AppendLine();
        sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"System: {companyName}");
        sb.AppendLine($"Testbed: NAVΛ Universal Dashboard");
        sb.AppendLine($"Safety Integrity Level: {safetyLevel}");
        sb.AppendLine();
        
        // Executive Summary
        sb.AppendLine("EXECUTIVE SUMMARY");
        sb.AppendLine("-".PadRight(80, '-'));
        int passedCount = 0;
        int totalCount = auditResults.Count;
        float totalScore = 0f;
        
        foreach (var result in auditResults)
        {
            if (result.passed) passedCount++;
            totalScore += result.score;
        }
        
        float overallScore = totalCount > 0 ? totalScore / totalCount : 0f;
        bool overallPass = passedCount == totalCount;
        
        sb.AppendLine($"Total Checks: {totalCount}");
        sb.AppendLine($"Passed: {passedCount}");
        sb.AppendLine($"Failed: {totalCount - passedCount}");
        sb.AppendLine($"Overall Score: {overallScore:P1}");
        sb.AppendLine($"Status: {(overallPass ? "CERTIFIED" : "REJECTED")}");
        sb.AppendLine();
        
        // Detailed Results
        sb.AppendLine("DETAILED AUDIT RESULTS");
        sb.AppendLine("-".PadRight(80, '-'));
        
        for (int i = 0; i < auditResults.Count; i++)
        {
            var result = auditResults[i];
            sb.AppendLine($"{i + 1}. {result.checkName}");
            sb.AppendLine($"   Status: {(result.passed ? "PASS" : "FAIL")}");
            sb.AppendLine($"   Score: {result.score:P1}");
            sb.AppendLine($"   Details: {result.details}");
            sb.AppendLine();
        }
        
        // Conclusion
        sb.AppendLine("CONCLUSION");
        sb.AppendLine("-".PadRight(80, '-'));
        sb.AppendLine($"FINAL VERDICT: {(overallPass ? "CERTIFIED" : "REJECTED")}");
        sb.AppendLine();
        
        if (overallPass)
        {
            sb.AppendLine($"The NAVΛ Universal Dashboard meets {standard} {safetyLevel} requirements.");
            sb.AppendLine("All safety checks passed. System is certified for deployment.");
        }
        else
        {
            sb.AppendLine($"The NAVΛ Universal Dashboard does NOT meet {standard} {safetyLevel} requirements.");
            sb.AppendLine("One or more safety checks failed. System requires remediation before deployment.");
        }
        
        sb.AppendLine();
        sb.AppendLine("=".PadRight(80, '='));
        sb.AppendLine($"Report End - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        
        return sb.ToString();
    }

    void SaveReport(string report)
    {
        try
        {
            // Create directory if it doesn't exist
            if (!Directory.Exists(reportDirectory))
            {
                Directory.CreateDirectory(reportDirectory);
            }
            
            // Generate filename
            string filename = $"{standard.Replace(" ", "_")}_Compliance_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            string filepath = Path.Combine(reportDirectory, filename);
            
            // Save report
            File.WriteAllText(filepath, report);
            
            Debug.Log($"[ComplianceAuditor] Report saved to: {filepath}");
            
            // Also save as HTML
            SaveHTMLReport(report, filepath.Replace(".txt", ".html"));
        }
        catch (Exception e)
        {
            Debug.LogError($"[ComplianceAuditor] Failed to save report: {e.Message}");
        }
    }

    void SaveHTMLReport(string textReport, string htmlPath)
    {
        try
        {
            StringBuilder html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html><head>");
            html.AppendLine("<title>ISO 26262 Compliance Report</title>");
            html.AppendLine("<style>");
            html.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
            html.AppendLine("h1 { color: #333; }");
            html.AppendLine(".pass { color: green; font-weight: bold; }");
            html.AppendLine(".fail { color: red; font-weight: bold; }");
            html.AppendLine("pre { background: #f5f5f5; padding: 10px; border-radius: 5px; }");
            html.AppendLine("</style>");
            html.AppendLine("</head><body>");
            html.AppendLine("<h1>ISO 26262 Safety Integrity Report</h1>");
            html.AppendLine("<pre>");
            html.AppendLine(textReport.Replace("PASS", "<span class='pass'>PASS</span>")
                                      .Replace("FAIL", "<span class='fail'>FAIL</span>")
                                      .Replace("CERTIFIED", "<span class='pass'>CERTIFIED</span>")
                                      .Replace("REJECTED", "<span class='fail'>REJECTED</span>"));
            html.AppendLine("</pre>");
            html.AppendLine("</body></html>");
            
            File.WriteAllText(htmlPath, html.ToString());
            Debug.Log($"[ComplianceAuditor] HTML report saved to: {htmlPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[ComplianceAuditor] Failed to save HTML report: {e.Message}");
        }
    }

    /// <summary>
    /// Get audit results
    /// </summary>
    public List<AuditResult> GetAuditResults()
    {
        return new List<AuditResult>(auditResults);
    }

    /// <summary>
    /// Get overall pass status
    /// </summary>
    public bool IsCompliant()
    {
        foreach (var result in auditResults)
        {
            if (!result.passed) return false;
        }
        return auditResults.Count > 0;
    }
}
