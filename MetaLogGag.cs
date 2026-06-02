using UnityEngine;

public class MetaLogGag : MonoBehaviour
{
    void Awake()
    {
        // Intercept logs before they hit the console
        Application.logMessageReceived += HandleLog;
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        // If the log starts with these Meta tags, tell the logger to ignore it
        if (logString.StartsWith("[OVRPlugin]") || 
            logString.StartsWith("[MetaXRFeature]") || 
            logString.StartsWith("[OVRManager]"))
        {
            // We do nothing, effectively "swallowing" the log
        }
        else if (type == LogType.Log)
        {
            // This is where your actual logs would go if you wanted to see them
            // but for now, just letting them pass is usually enough.
        }
    }
}
