using System;
using System.IO;
using UnityEngine;
using TMPro;
using System.IO.Ports;
using System.Threading;
using System.Collections.Concurrent; // Added for thread-safe queues

public class ArduinoSerialReader : MonoBehaviour {
    [SerializeField] private TMP_Text bikeSpeed_ReadUI;
    public string USBPort = "COM7"; // Fallback default

    private CharacterController characterController;
    private SerialPort stream; 
    private Thread serialThread;
    
    // Fix #2: Use a thread-safe queue instead of a single variable to prevent dropped packets
    private readonly ConcurrentQueue<int> _signalQueue = new ConcurrentQueue<int>();

    [Header("Bike Settings")]
    public float speed = 0;
    private float wheelCircumference = 2.22f;
    private float lastHitTime;
    
    public bool isBreaking;
    private bool isRunning;

    public float maxTimeBetweenSignals = 2.0f;
    public float brakeSpeedMultiplier = 2.0f;

    [HideInInspector] public int leftBrakeInd;
    [HideInInspector] public int rightBrakeInd;

    private bool _hasCleanedUp = false;

    void Awake(){
        characterController = GetComponent<CharacterController>();
    }

    void Start() {
        // Instantiate the stream cleanly using the configured variable port
        stream = new SerialPort(string.IsNullOrEmpty(USBPort) ? "COM7" : USBPort, 115200);

        try {
            stream.ReadTimeout = 200; // Give it slightly more breathing room
            stream.WriteTimeout = 200;
            stream.Open();
            
            isRunning = true;

            serialThread = new Thread(ReadSerialLoop) {
                IsBackground = true,
                Name = "ArduinoSerialReaderThread"
            };
            serialThread.Start();
            Debug.Log($"[ArduinoReader] Serial port {stream.PortName} opened successfully.");
        }
        catch(Exception ex) {
            Debug.LogError($"[ArduinoReader] Connection failed on {USBPort}: {ex.Message}");
        }
    }

    void Update() {
        // Process all raw signals accumulated in the thread-safe queue this frame
        while (_signalQueue.TryDequeue(out int signal)) {
            ProcessSignal(signal);
        }

        // Apply physics slowdown deceleration over time
        if (Time.time - lastHitTime > maxTimeBetweenSignals) {
            speed = Mathf.MoveTowards(speed, 0f, 0.4f * Time.deltaTime);
        }
        
        if (isBreaking) {
            speed = Mathf.MoveTowards(speed, 0f, brakeSpeedMultiplier * Time.deltaTime);
        }

        if (bikeSpeed_ReadUI != null) {
            bikeSpeed_ReadUI.text = $"{speed:F2} km/t";
        }
    }

    private void ProcessSignal(int signal) {
        if (signal == 1 && !isBreaking) {
            float timeBetweenHits = Time.time - lastHitTime;
            lastHitTime = Time.time;
            if (timeBetweenHits > 0) {
                speed = wheelCircumference / timeBetweenHits;
            }
        }
        else if (signal == 2 || signal == 4) {
            isBreaking = true;
        }
        else if (signal == 3 || signal == 5) {
            isBreaking = false;
        }

        leftBrakeInd = (signal == 2) ? 1 : 0;
        rightBrakeInd = (signal == 4) ? 1 : 0;
    }

    void ReadSerialLoop() {
        // Cache reference locally for thread isolation stability
        SerialPort localStream = stream;

        while (isRunning && localStream != null && localStream.IsOpen) {
            try {
                // ReadLine blocks until a '\n' is found OR ReadTimeout is reached
                string data = localStream.ReadLine();
                
                if (!string.IsNullOrEmpty(data) && int.TryParse(data, out int signal)) {
                    _signalQueue.Enqueue(signal);
                }
            }
            catch (TimeoutException) {
                // Timeout is completely normal when the bike wheel isn't spinning
            }
            catch (IOException ex) {
                // Triggered instantly when stream.Close() happens on the main thread
                Debug.Log($"[ArduinoReader] Serial stream interrupted safely: {ex.Message}");
                break;
            }
            catch (Exception ex) {
                System.Console.WriteLine($"[ArduinoReader] Thread error: {ex.Message}");
            }
        }
    }

    private void Cleanup() {
        if (_hasCleanedUp) return;
        _hasCleanedUp = true;

        isRunning = false;

        // Fix #1: Close the stream FIRST. This unlocks the blocking .ReadLine() call 
        // on the background thread by throwing an intentional IOException.
        if (stream != null) {
            try {
                if (stream.IsOpen) {
                    stream.Close();
                    Debug.Log("[ArduinoReader] Serial stream closed cleanly.");
                }
            }
            catch (Exception ex) {
                Debug.LogWarning($"[ArduinoReader] Error closing stream: {ex.Message}");
            }
            finally {
                stream.Dispose();
                stream = null;
            }
        }

        // Now that the blocking call is broken, the thread will join instantly without freezing Unity
        if (serialThread != null && serialThread.IsAlive) {
            if (!serialThread.Join(1000)) { // 1-second absolute safety escape hatch
                serialThread.Abort(); 
            }
        }
        serialThread = null;
    }

    private void OnDisable()         => Cleanup();
    private void OnApplicationQuit() => Cleanup();
    private void OnDestroy()         => Cleanup();
}
// using System.IO;
// using UnityEngine;
// using TMPro;
// using System.IO.Ports;
// using System.Threading;
//
// public class ArduinoSerialReader : MonoBehaviour {
//     [SerializeField] private TMP_Text bikeSpeed_ReadUI;
//     public string USBPort;
//     // private string logPath;
//
//     private CharacterController characterController;
//
//     SerialPort stream = new SerialPort("COM7", 115200); 
//     private Thread serialThread;
//     private object lockObject = new();
//     private int lastRecievedSignal;
//
//     public float speed = 0;
//     private float wheelCircumference = 2.22f;
//     private float lastHitTime;
//     private float timeBetweenHits;
//     private float lastBrake;
//     private float finalSpeed;
//
//     public bool isBreaking;
//     public bool isRunning;
//
//     public float stopTime;
//     public float maxTimeBetweenSignals;
//     public float brakeSpeedMultiplier;
//
//     public int leftBrakeInd;
//     public int rightBrakeInd;
//
//     private string jsonOut;
//
//     void Awake(){
//         characterController = GetComponent<CharacterController>();
//     }
//
//     void Start() {
//         // logPath = Application.dataPath + "/CARLogs/arduinoLog.txt";
//
//         try{
//             stream.ReadTimeout = 50;
//             stream.PortName = USBPort;
//             stream.Open();
//             isRunning = true;
//
//             //Runs on a seperate thread!!!
//             serialThread = new(ReadSerialLoop);
//             serialThread.Start();
//         }
//         catch(System.IO.IOException ex){
//             Debug.Log(ex.Message + "\nArduino not found. Connect through USB and check USB-port. Expected port: " + USBPort);
//         }
//     }
//
//     void Update() {
//         int signal;
//         //lock object to make sure we dont accidentally write to it from multiple source
//         lock(lockObject){
//             signal = lastRecievedSignal;
//             lastRecievedSignal = 0;
//         }
//
//         if(signal == 1 && !isBreaking){
//             float timeBetweenHits = Time.time - lastHitTime;
//             lastHitTime = Time.time;
//             if(timeBetweenHits > 0){
//                 speed = wheelCircumference / timeBetweenHits;
//             }
//         }
//         else if(signal == 2 || signal == 4){
//             isBreaking = true;
//         }
//         else if(signal == 3 || signal == 5){
//             isBreaking = false;
//         }
//
//         leftBrakeInd = signal == 2 ? 1 : 0;
//         rightBrakeInd = signal == 4 ? 1 : 0;
//
//         if(Time.time - lastHitTime > maxTimeBetweenSignals){
//             speed = Mathf.MoveTowards(speed,0f,0.4f * Time.deltaTime);
//         }
//         if(isBreaking){
//             speed = Mathf.MoveTowards(speed,0,brakeSpeedMultiplier * Time.deltaTime);
//         }
//
//         bikeSpeed_ReadUI.text = $"{speed:F2} km/t";
//     }
//
//     void ReadSerialLoop(){
//         while(isRunning && stream != null && stream.IsOpen){
//             try{
//                 if(stream.BytesToRead > 0){
//                     string data = stream.ReadLine();
//                     if(int.TryParse(data,out int signal)){
//                         //lock object to make sure we dont accidentally write to it from multiple source
//                         lock(lockObject){
//                             lastRecievedSignal = signal;
//                         }
//                     }
//                 }
//             }
//             catch(System.TimeoutException){
//             }
//             catch(System.Exception ex){
//                 Debug.Log(ex.Message);
//             }
//         }
//     }
//
//     void OnApplicationQuit() {
//         isRunning = false;
//         //clean up after use to make sure the resources are freed
//         if(serialThread != null) serialThread.Join();
//         if(stream != null && stream.IsOpen) stream.Close();
//     }
// }
