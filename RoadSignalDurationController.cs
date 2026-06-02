using UnityEngine;

public class RoadSignalDurationController : MonoBehaviour
{
    public RoadSignalEventHandler eventHandler = new();

    [Header("Durations (seconds)")]
    public float greenSeconds = 10f;
    public float yellowSeconds = 3f;
    public float redSeconds = 10f;

    // Optional: force a state for testing
    public enum SignalOverride { None, ForceRed, ForceYellow, ForceGreen }
    [Header("TEST OVERRIDE")]
    public SignalOverride overrideSignal = SignalOverride.None;

    public RoadSignals Current { get; private set; } = RoadSignals.Green;

    float _stateStartTime;

    void Start()
    {
        SetSignal(RoadSignals.Green);
        _stateStartTime = Time.time;
    }

    void Update()
    {
        // --- TEST OVERRIDE ---
        if (overrideSignal != SignalOverride.None)
        {
            switch (overrideSignal)
            {
                case SignalOverride.ForceRed: SetSignal(RoadSignals.Red); break;
                case SignalOverride.ForceYellow: SetSignal(RoadSignals.Yellow); break;
                case SignalOverride.ForceGreen: SetSignal(RoadSignals.Green); break;
            }
            return;
        }

        float elapsed = Time.time - _stateStartTime;

        switch (Current)
        {
            case RoadSignals.Green:
                if (elapsed >= greenSeconds) SwitchTo(RoadSignals.Yellow);
                break;

            case RoadSignals.Yellow:
                if (elapsed >= yellowSeconds) SwitchTo(RoadSignals.Red);
                break;

            case RoadSignals.Red:
                if (elapsed >= redSeconds) SwitchTo(RoadSignals.Green);
                break;
        }
    }

    public bool IsRed() => Current == RoadSignals.Red;

    void SwitchTo(RoadSignals next)
    {
        _stateStartTime = Time.time;
        SetSignal(next);
    }

    void SetSignal(RoadSignals next)
    {
        if (Current == next) return;   // only fire on change
        Current = next;
        eventHandler.Invoke(Current);
    }


#if UNITY_EDITOR
void OnDrawGizmos()
    {
        // Draw above the signal so it's visible
        Vector3 pos = transform.position + Vector3.up * 2.5f;

        switch (Current)
        {
            case RoadSignals.Red:
                Gizmos.color = Color.red;
                break;

            case RoadSignals.Yellow:
                Gizmos.color = Color.yellow;
                break;

            case RoadSignals.Green:
                Gizmos.color = Color.green;
                break;
        }

        Gizmos.DrawSphere(pos, 0.35f);
    }
#endif

}
