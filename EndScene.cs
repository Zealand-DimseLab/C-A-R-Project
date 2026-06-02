using UnityEngine.Networking;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.IO;
using System.Collections;

public class EndScene : MonoBehaviour{
    [Header("Bike Data")]
    [SerializeField] private TMP_Text bikeDataLogText;
    [SerializeField] private Button bikeDataSenderButton;
    [SerializeField] private TMP_Text bikeDataSenderButtonText;

    [Header("Head Transform")]
    [SerializeField] private TMP_Text headTransformDataLogText;
    [SerializeField] private Button headTransformSenderButton;
    [SerializeField] private TMP_Text headTransformSenderButtonText;

    [Header("Arduino")]
    [SerializeField] private TMP_Text arduinoDataLogText;
    [SerializeField] private Button arduinoSenderButton;
    [SerializeField] private TMP_Text arduinoSenderButtonText;

    [Header("Pulse")]
    [SerializeField] private TMP_Text pulseDataLogText;
    [SerializeField] private Button pulseSenderButton;
    [SerializeField] private TMP_Text pulseSenderButtonText;

    private string bærbarAPI = "https://10.200.130.98:5001/api/cardata";
    private string rpiAPI = "https://10.200.130.36:5001/api/cardata";

    private string bikeDataLogPath;
    private string headTransformLogPath;
    private string arduinoLogPath;
    private string pulseLogPath;
    private string scenarioLogPath;
    private string fenceLogPath;

    public float checkInterval;
    private float timeSinceLastCheck;
    private int counter;

    void Start(){
        bikeDataLogPath = Application.dataPath + "/CARLogs/bikeData.txt";
        headTransformLogPath = Application.dataPath + "/CARLogs/headTransform.txt";
        arduinoLogPath = Application.dataPath + "/CARLogs/arduino.txt";
        pulseLogPath = Application.dataPath + "/CARLogs/pulse.txt";
        scenarioLogPath = Application.dataPath + "/CARLogs/scenario.txt";
        fenceLogPath = Application.dataPath + "/CARLogs/fence.txt";

        if(File.Exists(bikeDataLogPath)){
            FileInfo fileInfo = new(bikeDataLogPath);
            bikeDataLogText.text = $"Log file found: {fileInfo.FullName}\nContent size: {fileInfo.Length}";
            bikeDataSenderButtonText.text = "Send";
            bikeDataSenderButton.enabled = true;
        }
        else{
            bikeDataLogText.text = "Log not found";
            bikeDataSenderButtonText.text = "Send";
            bikeDataSenderButton.enabled = false;
        }

        if(File.Exists(headTransformLogPath)){
            FileInfo fileInfo = new(headTransformLogPath);
            headTransformDataLogText.text = $"Log file found: {fileInfo.FullName}\nContent size: {fileInfo.Length}";
            headTransformSenderButtonText.text = "Send";
            headTransformSenderButton.enabled = true;
        }
        else{
            headTransformDataLogText.text = "Log not found";
            headTransformSenderButtonText.text = "Send";
            headTransformSenderButton.enabled = false;
        }

        if(File.Exists(arduinoLogPath)){
            FileInfo fileInfo = new(arduinoLogPath);
            arduinoDataLogText.text = $"Log file found: {fileInfo.FullName}\nContent size: {fileInfo.Length}";
            arduinoSenderButtonText.text = "Send";
            arduinoSenderButton.enabled = true;
        }
        else{
            arduinoDataLogText.text = "Log not found";
            arduinoSenderButtonText.text = "Send";
            arduinoSenderButton.enabled = false;
        }

        if(File.Exists(pulseLogPath)){
            FileInfo fileInfo = new(pulseLogPath);
            pulseDataLogText.text = $"Log file found: {fileInfo.FullName}\nContent size: {fileInfo.Length}";
            pulseSenderButtonText.text = "Send";
            pulseSenderButton.enabled = true;
        }
        else{
            pulseDataLogText.text = "Log not found";
            pulseSenderButtonText.text = "Send";
            pulseSenderButton.enabled = false;
        }
    }

    void Update(){
        if(Time.time - timeSinceLastCheck > checkInterval){

            if(File.Exists(bikeDataLogPath)){
                FileInfo fileInfo = new(bikeDataLogPath);
                bikeDataLogText.text = $"Log file found:\n {fileInfo.Name}\nContent size: {fileInfo.Length}";
                bikeDataSenderButtonText.text = "Send";
                bikeDataSenderButton.enabled = true;
            }
            else{
                bikeDataLogText.text = "Log not found. Checking... " + counter;
                bikeDataSenderButtonText.text = "Send";
                bikeDataSenderButton.enabled = false;
            }

            if(File.Exists(headTransformLogPath)){
                FileInfo fileInfo = new(headTransformLogPath);
                headTransformDataLogText.text = $"Log file found:\n {fileInfo.Name}\nContent size: {fileInfo.Length}";
                headTransformSenderButtonText.text = "Send";
                headTransformSenderButton.enabled = true;
            }
            else{
                headTransformDataLogText.text = "Log not found";
                headTransformSenderButtonText.text = "Send";
                headTransformSenderButton.enabled = false;
            }

            if(File.Exists(arduinoLogPath)){
                FileInfo fileInfo = new(arduinoLogPath);
                arduinoDataLogText.text = $"Log file found:\n {fileInfo.Name}\nContent size: {fileInfo.Length}";
                arduinoSenderButtonText.text = "Send";
                arduinoSenderButton.enabled = true;
            }
            else{
                arduinoDataLogText.text = "Log not found";
                arduinoSenderButtonText.text = "Send";
                arduinoSenderButton.enabled = false;
            }

            if(File.Exists(pulseLogPath)){
                FileInfo fileInfo = new(pulseLogPath);
                pulseDataLogText.text = $"Log file found:\n {fileInfo.Name}\nContent size: {fileInfo.Length}";
                pulseSenderButtonText.text = "Send";
                pulseSenderButton.enabled = true;
            }
            else{
                pulseDataLogText.text = "Log not found";
                pulseSenderButtonText.text = "Send";
                pulseSenderButton.enabled = false;
            }

            timeSinceLastCheck = Time.time;
            counter++;
        }
        if(counter > 3) counter = 0;
    }

    public void SendBikeData(){
        StartCoroutine(SendBikeDataCoroutine());
    }

    public void SendHeadTransform(){
        StartCoroutine(SendHeadTransformCoroutine());
    }

    public void SendArduinoData(){
        StartCoroutine(SendArduinoDataCoroutine());
    }

    public void SendPulseData(){
        StartCoroutine(SendPulseDataCoroutine());
    }

    public IEnumerator SendBikeDataCoroutine(){
        if (!File.Exists(bikeDataLogPath)) {
            Debug.LogError("Bike data log file does not exist.");
            yield break;
        }

        string content = File.ReadAllText(bikeDataLogPath);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(content);

        using (UnityWebRequest webRequest = new UnityWebRequest(rpiAPI+"/logbikedata", "POST")) {
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.certificateHandler = new BypassCertificate();
            webRequest.disposeCertificateHandlerOnDispose = true;
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);

            Debug.Log("Sending bike data to API...");
            Debug.Log("Content length: " + content.Length);
            webRequest.SetRequestHeader("Content-Type", "application/octet-stream");

            yield return webRequest.SendWebRequest();

            Debug.Log("Web request result: " + webRequest.result);
            Debug.Log("Response code: " + webRequest.responseCode);
            Debug.Log("Answer: " + webRequest.downloadHandler.text);

            if (webRequest.result == UnityWebRequest.Result.Success) {
                Debug.Log("Bike data sent successfully.");
                bikeDataSenderButtonText.text = "Sent";
            }
            else {
                Debug.LogError("Failed to send bike data: " + webRequest.error);
                bikeDataSenderButtonText.text = "Failed";
            }
        }
    }

    public IEnumerator SendHeadTransformCoroutine(){
        if (!File.Exists(headTransformLogPath)) {
            Debug.LogError("Head transform log file does not exist.");
            yield break;
        }

        string content = File.ReadAllText(headTransformLogPath);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(content);

        using (UnityWebRequest webRequest = new UnityWebRequest(rpiAPI+"/loghtf", "POST")) {
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.certificateHandler = new BypassCertificate();
            webRequest.disposeCertificateHandlerOnDispose = true;
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);

            Debug.Log("Sending head transform data to API...");
            Debug.Log("Content length: " + content.Length);
            webRequest.SetRequestHeader("Content-Type", "application/octet-stream");

            yield return webRequest.SendWebRequest();

            Debug.Log("Web request result: " + webRequest.result);
            Debug.Log("Response code: " + webRequest.responseCode);
            Debug.Log("Answer: " + webRequest.downloadHandler.text);

            if (webRequest.result == UnityWebRequest.Result.Success) {
                Debug.Log("Head transform data sent successfully.");
                headTransformSenderButtonText.text = "Sent";
            }
            else {
                Debug.LogError("Failed to send head transform data: " + webRequest.error);
                headTransformSenderButtonText.text = "Failed";
            }
        }
    }

    public IEnumerator SendArduinoDataCoroutine(){
        if (!File.Exists(arduinoLogPath)) {
            Debug.LogError("Arduino log file does not exist.");
            yield break;
        }

        string content = File.ReadAllText(arduinoLogPath);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(content);

        using (UnityWebRequest webRequest = new UnityWebRequest(rpiAPI+"/logarduino", "POST")) {
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.certificateHandler = new BypassCertificate();
            webRequest.disposeCertificateHandlerOnDispose = true;
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);

            Debug.Log("Sending arduino data to API...");
            Debug.Log("Content length: " + content.Length);
            webRequest.SetRequestHeader("Content-Type", "application/octet-stream");

            yield return webRequest.SendWebRequest();

            Debug.Log("Web request result: " + webRequest.result);
            Debug.Log("Response code: " + webRequest.responseCode);
            Debug.Log("Answer: " + webRequest.downloadHandler.text);

            if (webRequest.result == UnityWebRequest.Result.Success) {
                Debug.Log("Arduino data sent successfully.");
                headTransformSenderButtonText.text = "Sent";
            }
            else {
                Debug.LogError("Failed to send arduino data: " + webRequest.error);
                headTransformSenderButtonText.text = "Failed";
            }
        }
    }

    public IEnumerator SendPulseDataCoroutine(){
        if (!File.Exists(pulseLogPath)) {
            Debug.LogError("Pulse log file does not exist.");
            yield break;
        }

        string content = File.ReadAllText(pulseLogPath);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(content);

        using (UnityWebRequest webRequest = new UnityWebRequest(rpiAPI+"/logpulse", "POST")) {
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.certificateHandler = new BypassCertificate();
            webRequest.disposeCertificateHandlerOnDispose = true;
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);

            Debug.Log("Sending pulse data to API...");
            Debug.Log("Content length: " + content.Length);
            webRequest.SetRequestHeader("Content-Type", "application/octet-stream");

            yield return webRequest.SendWebRequest();

            Debug.Log("Web request result: " + webRequest.result);
            Debug.Log("Response code: " + webRequest.responseCode);
            Debug.Log("Answer: " + webRequest.downloadHandler.text);

            if (webRequest.result == UnityWebRequest.Result.Success) {
                Debug.Log("Pulse data sent successfully.");
                headTransformSenderButtonText.text = "Sent";
            }
            else {
                Debug.LogError("Failed to send pulse data: " + webRequest.error);
                headTransformSenderButtonText.text = "Failed";
            }
        }
    }

    public void EndSim(){
#if UNITY_EDITOR
#else
        Application.Quit();
#endif
    }
}

public class BypassCertificate : CertificateHandler{
    protected override bool ValidateCertificate(byte[] certificateData) {
        return true;
    }
}
