using UnityEngine;

public class RoadSignalController : MonoBehaviour
{
    [Header("Signal Source")]
    public RoadSignalDurationController durationController;

    [Header("Light Objects")]
    [SerializeField] private GameObject redLightObj;
    [SerializeField] private GameObject yellowLightObj;
    [SerializeField] private GameObject greenLightObj;

    [Header("Materials")]
    [SerializeField] private Material noSignalMat;
    [SerializeField] private Material redLightMat;
    [SerializeField] private Material yellowLightMat;
    [SerializeField] private Material greenLightMat;

    private Renderer redRenderer;
    private Renderer yellowRenderer;
    private Renderer greenRenderer;

    public RoadSignals CurrentSignal { get; private set; } = RoadSignals.Green;

    void Awake()
    {
        redRenderer = redLightObj.GetComponent<Renderer>();
        yellowRenderer = yellowLightObj.GetComponent<Renderer>();
        greenRenderer = greenLightObj.GetComponent<Renderer>();
    }

    void OnEnable()
    {
        if (durationController != null)
        {
            durationController.eventHandler.AddListener(OnSignalChanged);
            // Apply current state immediately
            OnSignalChanged(durationController.Current);
        }
    }

    void OnDisable()
    {
        if (durationController != null)
            durationController.eventHandler.RemoveListener(OnSignalChanged);
    }

    void OnSignalChanged(RoadSignals signal)
    {
        CurrentSignal = signal;

        // Reset all to off first
        redRenderer.material = noSignalMat;
        yellowRenderer.material = noSignalMat;
        greenRenderer.material = noSignalMat;

        // Turn on the correct lamp
        switch (signal)
        {
            case RoadSignals.Green:
                greenRenderer.material = greenLightMat;
                break;

            case RoadSignals.Yellow:
                yellowRenderer.material = yellowLightMat;
                break;

            case RoadSignals.Red:
                redRenderer.material = redLightMat;
                break;
        }
    }

    public bool IsRed()
    {
        return CurrentSignal == RoadSignals.Red;
    }
}
