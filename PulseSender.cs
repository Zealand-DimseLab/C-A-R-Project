using System.IO;
using UnityEngine;
using System.Collections;
using System.Threading;
using System.Collections.Concurrent;
using UnityEngine.Networking;

public class PulseSender : MonoBehaviour {
    [SerializeField] private TMPro.TMP_Text currentIdText;
    [SerializeField] private TMPro.TMP_Text pulseDataText;
    [SerializeField] private GameObject startBridgeButton;

    [Header("Inspector")]
    [SerializeField] private WahooWsClient wahooWsClient;
    [SerializeField] private LiveAnalytics.TelemetryPublisher telemetryPublisher;
    [SerializeField] private string analyticsApiUrl = "http://127.0.0.1:8080";
    [SerializeField] private string externalApiUrl = "https://10.200.130.98:5001/api/car/logbikedata";

    // Cached path strings - NEVER modify or nullify these at runtime!
    private string _pulseLog;
    private string _arduinoLog;
    private string _bikeDataLog;
    private string _headTransformLog;
    private string _scenarioLog;
    private string _fenceLog;
    
    private string _sessionId     = "";
    private string _participantId = "";
    public string ParticipantId {get;private set;}

    private readonly ConcurrentQueue<string> _pulseQueue = new ConcurrentQueue<string>();
    private volatile int _latestHeartRate = 0;

    private Thread _loggingThread;
    private bool _isRunning = false; // Starts false until logging begins
    private float _timeSinceLastLog;
    private bool _hasCleanedUp = false;
    private bool hasInsertedId;

    void Start() {
        hasInsertedId = false;
        string logDir = Path.Combine(Application.dataPath, "CARLogs");
        pulseDataText.text = "Start Bridge To Recieve Pulse Data";

        if (!Directory.Exists(logDir)) {
            Directory.CreateDirectory(logDir);
        }

        _pulseLog = Path.Combine(logDir, "pulse.txt");
        _arduinoLog = Path.Combine(logDir, "arduino.txt");
        _bikeDataLog = Path.Combine(logDir, "bikeData.txt");
        _headTransformLog = Path.Combine(logDir, "headTransform.txt");
        _scenarioLog = Path.Combine(logDir, "scenario.txt");
        _fenceLog = Path.Combine(logDir, "fence.txt");

        // Wipe files clean for the session
        File.WriteAllText(_pulseLog, "");
        File.WriteAllText(_arduinoLog, "");
        File.WriteAllText(_bikeDataLog, "");
        File.WriteAllText(_headTransformLog, "");
        File.WriteAllText(_scenarioLog, "");
        File.WriteAllText(_fenceLog, "");

        if (wahooWsClient != null) {
            wahooWsClient.OnHeartRate += OnHeartRateReceived;
        } else {
            Debug.LogWarning("PulseSender: WahooWsClient not assigned — pulse will not be logged.");
        }

        StartCoroutine(InitSession());
    }

    private IEnumerator InitSession() {
        yield return null; // Wait 1 frame
        _sessionId = telemetryPublisher != null ? telemetryPublisher.SessionId : "";
        WriteHeader("PENDING");
        StartCoroutine(PollParticipantId());
    }

    public void SetParticipantID(string id){
        this.ParticipantId = id;
        RewriteHeader(id);

        hasInsertedId = true;
        StopCoroutine(PollParticipantId());
    }

    private IEnumerator PollParticipantId() {
        float[] delays = { 5f, 5f, 5f, 5f, 5f, 5f, 10f, 10f, 10f, 30f, 30f, 30f, 30f };
        foreach (float delay in delays) {
            yield return new WaitForSeconds(delay);
            if (!string.IsNullOrEmpty(_participantId)) yield break;

            yield return StartCoroutine(FetchParticipantId());

            if (!string.IsNullOrEmpty(_participantId)) {
                Debug.Log($"PulseSender: participant resolved → {_participantId}");
                RewriteHeader(_participantId);
                yield break;
            }
        }
    }

    private IEnumerator FetchParticipantId() {
        if (string.IsNullOrEmpty(_sessionId)) yield break;
        string url = $"{analyticsApiUrl}/api/sessions/{_sessionId}";
        
        using (UnityWebRequest req = UnityWebRequest.Get(url)) {
            req.timeout = 5;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success) {
                string pid = ExtractJsonString(req.downloadHandler.text, "participant_id");
                if (!string.IsNullOrEmpty(pid) && pid != "null")
                    _participantId = pid;
            }
        }
    }

    private void WriteHeader(string value) {
        try {
            File.WriteAllText(_pulseLog, value + "\n");
            currentIdText.text = value;
            ParticipantId = value;

            // Simply do not write to other files if startLogging is true, 
            // but NEVER set paths to null!
            if (LoggingStarter.startLogging) return;

            File.WriteAllText(_arduinoLog, value + "\n");
            File.WriteAllText(_bikeDataLog, value + "\n");
            File.WriteAllText(_headTransformLog, value + "\n");
            File.WriteAllText(_scenarioLog, value + "\n");
            File.WriteAllText(_fenceLog, value + "\n");
        }
        catch (System.Exception e) {
            Debug.LogWarning($"PulseSender: WriteHeader failed — {e.Message}");
        }
    }

    private void RewriteHeader(string participantId) {
        try {
            string[] lines = File.ReadAllLines(_pulseLog);
            if (lines.Length > 0) lines[0] = participantId;
            File.WriteAllLines(_pulseLog, lines);
            currentIdText.text = participantId;

            if (LoggingStarter.startLogging && hasInsertedId) return;

            File.WriteAllText(_arduinoLog, participantId + "\n");
            File.WriteAllText(_bikeDataLog, participantId + "\n");
            File.WriteAllText(_headTransformLog, participantId + "\n");
            File.WriteAllText(_scenarioLog, participantId + "\n");
            File.WriteAllText(_fenceLog, participantId + "\n");

            hasInsertedId = true;
        }
        catch (System.Exception e) {
            Debug.LogWarning($"PulseSender: RewriteHeader failed — {e.Message}");
        }
    }

    private void OnHeartRateReceived(int hr) {
        if (hr <= 10) {
            pulseDataText.text = "Start Bridge To Recieve Pulse Data";
            if(startBridgeButton != null) startBridgeButton.SetActive(true);
        } else {
            pulseDataText.text = hr.ToString();
            if(startBridgeButton != null) startBridgeButton.SetActive(false);
        }
        _latestHeartRate = hr;
    }

    void Update() {
        // Fix #1: If logging shouldn't happen, ensure thread stops spinning idly
        if (!LoggingStarter.startLogging) {
            if (_isRunning) StopLoggingThread();
            return;
        }

        // If logging should happen, lazily spin up the worker thread
        if (!_isRunning) {
            StartLoggingThread();
        }

        if (Time.time - _timeSinceLastLog > 1f) {
            _timeSinceLastLog = Time.time;
            int hr = _latestHeartRate;
            if (hr > 0) {
                _pulseQueue.Enqueue($"{hr}|{System.DateTimeOffset.UtcNow}");

                if (!string.IsNullOrEmpty(externalApiUrl) && !string.IsNullOrEmpty(_participantId))
                    StartCoroutine(PostToExternalApi(hr));
            }
        }
    }

    private void StartLoggingThread() {
        _isRunning = true;
        _loggingThread = new Thread(WriteToFileLoop) { IsBackground = true };
        _loggingThread.Start();
    }

    private void StopLoggingThread() {
        _isRunning = false;
        if (_loggingThread != null && _loggingThread.IsAlive) {
            _loggingThread.Join(1000); // 1-second clean allowance window
        }
        _loggingThread = null;
    }

    private void WriteToFileLoop() {
        // Cache path safely locally
        string localPulsePath = _pulseLog;

        while (_isRunning) {
            if (_pulseQueue.TryDequeue(out string line)) {
                try {
                    using (StreamWriter w = new StreamWriter(localPulsePath, append: true)) {
                        w.WriteLine(line);
                    }
                }
                catch (System.Exception e) {
                    System.Console.WriteLine($"PulseSender write error: {e.Message}");
                }
            } else {
                Thread.Sleep(20); // 20ms ensures high responsiveness during close calls
            }
        }
    }

    private IEnumerator PostToExternalApi(int bpm) {
        if (!int.TryParse(_participantId, out int userId)) yield break;

        string json = $"{{\"UserId\":{userId},\"Pulse\":{bpm}}}";
        byte[] bodyBytes = System.Text.Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest req = new UnityWebRequest(externalApiUrl, "POST")) {
            req.uploadHandler   = new UploadHandlerRaw(bodyBytes);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout            = 5;
            req.certificateHandler = new AcceptAllCertificatesPulse();

            yield return req.SendWebRequest();
        }
    }

    private void Cleanup() {
        if (_hasCleanedUp) return; // Prevent double invocation crash
        _hasCleanedUp = true;

        StopLoggingThread();

        if (wahooWsClient != null) {
            wahooWsClient.OnHeartRate -= OnHeartRateReceived;
        }
    }

    private void OnDisable()         => Cleanup(); // Safest lifecycle hook
    private void OnApplicationQuit() => Cleanup();
    private void OnDestroy()         => Cleanup();

    private static string ExtractJsonString(string json, string key) {
        string search = $"\"{key}\"";
        int ki = json.IndexOf(search);
        if (ki < 0) return null;
        int colon = json.IndexOf(':', ki + search.Length);
        if (colon < 0) return null;
        int start = colon + 1;
        while (start < json.Length && json[start] == ' ') start++;
        if (start >= json.Length) return null;
        if (json[start] == '"') {
            int end = json.IndexOf('"', start + 1);
            return end < 0 ? null : json.Substring(start + 1, end - start - 1);
        }
        int valEnd = json.IndexOfAny(new[] { ',', '}', ']' }, start);
        return valEnd < 0 ? json.Substring(start).Trim() : json.Substring(start, valEnd - start).Trim();
    }
}
public class AcceptAllCertificatesPulse : UnityEngine.Networking.CertificateHandler{
    protected override bool ValidateCertificate(byte[] certificateData) => true;
}
