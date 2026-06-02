using System.IO;
using UnityEngine;
using System.Threading;
using System.Collections.Concurrent;

public class DBSender : MonoBehaviour {
    public bool shouldSend = false;

    [SerializeField] private Transform headTransform;
    [SerializeField] private Transform bikeHandleTransform;
    [SerializeField] private PulseSender pulseSender;

    private string bikeDataLog;
    private string headTransformLog;
    private string arduinoLog;

    public ArduinoSerialReader arduinoSerialReader;
    private float speed => arduinoSerialReader.speed;

    private volatile int _latestHeartRate = 0;

    private float timeSinceLastLog;

    // Use a single struct to keep data synchronized and reduce multiple queues down to one
    private struct LogPayload {
        public string BikeData;
        public string HeadTransform;
        public string ArduinoData;
    }

    private ConcurrentQueue<LogPayload> masterLogQueue = new ConcurrentQueue<LogPayload>();

    private Thread loggingThread;
    private volatile bool isRunning = true; // 'volatile' ensures cross-thread safety for the bool

    public float logCooldown = 1.0f;

    private bool _manualOverride = false;

    public void SetParticipantIdManually(string pid) {
        if (string.IsNullOrWhiteSpace(pid)) {
            Debug.LogWarning("DBSender.SetParticipantIdManually: empty ID ignored.");
            return;
        }
        // if (_pollCoroutine != null) { StopCoroutine(_pollCoroutine); _pollCoroutine = null; }
        _manualOverride = true;
        // _participantId  = pid.Trim();
        pulseSender.SetParticipantID(pid.Trim());
        // RewriteHeader(_participantId);
        Debug.Log($"DBSender: participant ID manually overridden → {pulseSender.ParticipantId}");
    }


    void Start() {
        bikeDataLog = Application.dataPath + "/CARLogs/bikeData.txt";
        headTransformLog = Application.dataPath + "/CARLogs/headTransform.txt";
        arduinoLog = Application.dataPath + "/CARLogs/arduino.txt";

        string dir = Application.dataPath + "/CARLogs";
    }

    void Update() {
        if (!LoggingStarter.startLogging){
            if(loggingThread != null && isRunning){
                StopLoggingThread();
            }
            return;
        }

        if(loggingThread == null || !loggingThread.IsAlive){
            StartLoggingThread();
        }

        // Capture data on the Main Thread every X seconds
        if (Time.time - timeSinceLastLog > logCooldown) {
            timeSinceLastLog = Time.time;

            // 1. Extract data safely on Main Thread
            float bikeHandleRotationY = bikeHandleTransform.rotation.y;
            Quaternion headRot = headTransform.rotation;
            Vector3 headPos = headTransform.position;
            float currentSpeed = speed;

            // 2. Format the strings safely
            int hr = _latestHeartRate;
            LogPayload payload = new LogPayload {
                BikeData = System.FormattableString.Invariant($"\n\r|{Mathf.Round(bikeHandleRotationY * Mathf.Rad2Deg)}|{1}|{currentSpeed}"),
                HeadTransform = System.FormattableString.Invariant($"\n\r|{headRot.x}|{headRot.y}|{headRot.z}|{headRot.w}|{headPos.x}|{headPos.y}|{headPos.z}"),
                ArduinoData = System.FormattableString.Invariant($"\n\r{arduinoSerialReader.leftBrakeInd}|{arduinoSerialReader.rightBrakeInd}|{System.DateTime.UtcNow}"),
            };

            // 3. Send the unified payload to the single master queue
            masterLogQueue.Enqueue(payload);
        }
    }

    private void StartLoggingThread(){
        isRunning = true;
        loggingThread = new Thread(WriteToFileLoop);
        loggingThread.IsBackground = true;
        loggingThread.Start();
    }

    private void StopLoggingThread(){
        isRunning = false;
        if(loggingThread != null && loggingThread.IsAlive){
            loggingThread.Join(500);
        }
        loggingThread = null;
    }

    private void WriteToFileLoop() {
        string localBikePath = bikeDataLog;
        string localHeadPath = headTransformLog;
        string localArduinoPath = arduinoLog;

        while (isRunning) {
            bool processedElement = false;

            // Process one master payload at a time
            if (masterLogQueue.TryDequeue(out LogPayload payload)) {
                processedElement = true;

                try {
                    // Write Bike Data
                    using (StreamWriter writer = new StreamWriter(localBikePath, true)) {
                        writer.WriteLine(payload.BikeData);
                    }
                    // Write Head Data
                    using (StreamWriter writer = new StreamWriter(localHeadPath, true)) {
                        writer.WriteLine(payload.HeadTransform);
                    }
                    // Write Arduino Data
                    using (StreamWriter writer = new StreamWriter(localArduinoPath, true)) {
                        writer.WriteLine(payload.ArduinoData);
                    }
                }
                catch (System.Exception e) {
                    System.Console.WriteLine($"File Write Error: {e.Message}");
                }
            }

            // Only sleep if NO elements were processed during this loop cycle.
            // This prevents artificial lag spikes when processing backlogs.
            if (!processedElement) {
                Thread.Sleep(20); // Single 20ms sleep is plenty to keep CPU usage at 0%
            }
        }
    }

    private void OnHeartRateReceived(int hr) {
        _latestHeartRate = hr;
    }

    private void OnApplicationQuit() {
        StopLoggingThread();
    }
}
