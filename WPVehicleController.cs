// WPVehicleController.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WPVehicleController : MonoBehaviour {
    private GroundSensor groundSensor;

    public enum WaypointSpeed { Cruise, Slow }

    public float CurrentSpeedMps => _currentSpeedMps;
    public float CurrentSpeedKmh => _currentSpeedMps * 3.6f;

    [System.Serializable]
    public class WaypointBehaviour {
        public WaypointSpeed speed = WaypointSpeed.Cruise;

        public bool stopHere = false;
        public float stopSeconds = 10f;

        public bool startTimedDrive = false;
        public bool despawnHere = false;

        [Header("Traffic signal")]
        [Tooltip("Stop at this waypoint if signal is Red (and optionally Yellow), wait until Green.")]
        public bool waitForGreenHere = false;

        [Tooltip("Assign the RoadSignalDurationController that controls the light.")]
        public RoadSignalDurationController signal;

        [Tooltip("If true, treat Yellow as STOP as well.")]
        public bool stopOnYellow = true;

        [Tooltip("Optional: make this waypoint easier to hit (multiplies global waypointRadius). 1 = normal.")]
        public float radiusMultiplier = 2f;
    }

    [Header("Path")]
    public Transform pathRoot;

    [Header("Waypoint Behaviours (index matches path order)")]
    public List<WaypointBehaviour> waypointBehaviours = new List<WaypointBehaviour>();

    [Header("Speeds (km/h)")]
    public float cruiseSpeedKmh = 50f;
    public float slowSpeedKmh = 15f;

    [Tooltip("How quickly the vehicle reaches target speed (km/h per second).")]
    public float accelerationKmhPerSec = 10f;

    [Header("Level 2 Braking (distance-based)")]
    [Tooltip("Braking strength (km/h per second). Normal traffic: ~10-25. Higher = more aggressive.")]
    public float brakeDecelKmhPerSec = 18f;

    [Tooltip("Extra buffer distance added to stopping distance so we stop slightly before the point (meters).")]
    public float stopBufferMeters = 1.0f;

    [Tooltip("When very close to a stop target, clamp to 0 to avoid creeping/jitter (meters).")]
    public float stopSnapDistanceMeters = 0.35f;

    [Tooltip("Optional tiny speed to allow slow roll (km/h). Set 0 to disable.")]
    public float creepSpeedKmh = 0.0f;

    [Tooltip("How many upcoming waypoints to scan for an active stop (stopHere / red light). Higher = earlier braking.")]
    public int stopLookAheadWaypoints = 10;

    [Header("Vehicle Geometry (for pass distance)")]
    [Tooltip("Optional: transform placed on the car's RIGHT side (edge). Used to measure car half-width automatically.")]
    public Transform rightSideReference;

    [Tooltip("Fallback half-width (meters) used if Right Side Reference is not set. Typical car ~0.85-1.0.")]
    public float fallbackHalfWidthMeters = 0.9f;

    [Header("Scenario Start")]
    [Tooltip("If enabled, the vehicle will not move until TriggerStart() is called (e.g. from a trigger plate).")]
    public bool startOnlyWhenTriggered = false;

    [Tooltip("If true, this GameObject is deactivated on Start and activated when triggered.")]
    public bool deactivateUntilTriggered = false;

    [Tooltip("If true, ResetToStart() is called when triggered.")]
    public bool resetToStartOnTrigger = true;

    [Header("Turning")]
    public float maxYawDegPerSec = 60f;
    public float lookAheadDistance = 4f;

    [Header("Waypoint")]
    public float waypointRadius = 2.0f;

    [Header("Timed Drive")]
    public float timedDriveSeconds = 60f;

    [Header("Debug")]
    public bool debugLogs = false;

    Transform[] _path;
    int _i = 1;

    // internal units (m/s)
    float _currentSpeedMps;

    bool _waiting;
    bool _timedDrive;
    float _timedDriveTimer;
    Vector3 _timedDriveDir;

    RoadSignalDurationController _activeSignal;
    bool _waitingForGreen;
    int _signalStopIndex = -1;

    Coroutine _stopRoutine;
    bool _started;

    // ---------- unit conversions ----------
    const float KMH_TO_MPS = 1f / 3.6f;

    float CruiseMps => Mathf.Max(0f, cruiseSpeedKmh) * KMH_TO_MPS;
    float SlowMps => Mathf.Max(0f, slowSpeedKmh) * KMH_TO_MPS;

    // (km/h)/s -> m/s^2
    float AccelMps2 => Mathf.Max(0f, accelerationKmhPerSec) * KMH_TO_MPS;
    float BrakeMps2 => Mathf.Max(0.01f, brakeDecelKmhPerSec) * KMH_TO_MPS;

    float CreepMps => Mathf.Max(0f, creepSpeedKmh) * KMH_TO_MPS;

    void Awake() {
        groundSensor = GetComponent<GroundSensor>();
    }

    void Start() {
        BuildPath();
        SyncBehaviourList();

        if (startOnlyWhenTriggered) {
            _started = false;
            _waiting = true;
            _currentSpeedMps = 0f;

            if (deactivateUntilTriggered)
                gameObject.SetActive(false);

            return;
        }

        _started = true;
        ResetToStart();
    }

    void OnDisable() {
        UnsubscribeSignal();
    }

    /// <summary>
    /// Call this from a trigger plate (bike enters) to begin driving the route.
    /// </summary>
    public void TriggerStart() {
        if (_started) return;

        _started = true;

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        _waiting = false;
        _waitingForGreen = false;

        if (resetToStartOnTrigger)
            ResetToStart();
    }

    //There must be atleast 2 waypoints to define a path and facing direction.
    void BuildPath() {
        if (pathRoot == null || pathRoot.childCount < 2) {
            Debug.LogError($"{name}: PathRoot must have at least 2 children.");
            enabled = false;
            return;
        }

        int childCount = pathRoot.childCount;

        _path = new Transform[pathRoot.childCount];
        for (int i = 0; i < pathRoot.childCount; i++)
            _path[i] = pathRoot.GetChild(i);
    }

    void SyncBehaviourList() {
        while (_path != null && waypointBehaviours.Count < _path.Length)
            waypointBehaviours.Add(new WaypointBehaviour());
    }

    void Update() {
        if (_path == null) return;
        if (startOnlyWhenTriggered && !_started) return;
        if (_waiting) return;

        if (_timedDrive) {
            TimedDriveUpdate();
            return;
        }

        // --- Determine target speed (m/s) from current waypoint behaviour ---
        WaypointBehaviour behaviour = waypointBehaviours[_i];
        float targetCruiseMps = (behaviour.speed == WaypointSpeed.Slow) ? SlowMps : CruiseMps;

        // --- Check if we must stop ahead (stopHere or red/yellow light) ---
        bool mustStopAhead = TryGetActiveStopTarget(out int stopIndex, out StopReason stopReason, out RoadSignalDurationController stopSignal);

        if (mustStopAhead) {
            Vector3 stopPos = _path[stopIndex].position;
            float distToStop = FlatDistance(transform.position, stopPos);

            float v = Mathf.Max(0f, _currentSpeedMps);
            float a = BrakeMps2;

            // stopping distance: d = v^2 / (2a)
            float stoppingDist = (v * v) / (2f * a) + Mathf.Max(0f, stopBufferMeters);

            if (debugLogs)
                Debug.Log($"{name} stopAhead idx={stopIndex} reason={stopReason} dist={distToStop:F2} stopDist={stoppingDist:F2} v={v:F2}m/s");

            if (distToStop <= stoppingDist) {
                // brake toward creep or 0
                float brakeTarget = (CreepMps > 0f) ? CreepMps : 0f;
                _currentSpeedMps = Mathf.MoveTowards(_currentSpeedMps, brakeTarget, a * Time.deltaTime);

                // Snap and commit stop
                if (distToStop <= stopSnapDistanceMeters || _currentSpeedMps <= 0.02f) {
                    _currentSpeedMps = 0f;

                    if (stopReason == StopReason.Signal) {
                        StartWaitForGreen(stopSignal, waypointBehaviours[stopIndex].stopOnYellow, stopIndex);
                        return;
                    }
                    else if (stopReason == StopReason.StopHere) {
                        StartTimedStop(waypointBehaviours[stopIndex].stopSeconds, stopIndex);
                        return;
                    }
                }
            }
            else {
                // Not braking yet: accelerate toward cruise/slow
                _currentSpeedMps = Mathf.MoveTowards(_currentSpeedMps, targetCruiseMps, AccelMps2 * Time.deltaTime);
            }
        }
        else {
            // Normal driving
            _currentSpeedMps = Mathf.MoveTowards(_currentSpeedMps, targetCruiseMps, AccelMps2 * Time.deltaTime);
        }

        // --- Steering / facing ---
        Vector3 aimPoint = GetAimPoint();
        Vector3 toAim = aimPoint - transform.position;
        toAim.y = 0f;

        if (toAim.sqrMagnitude > 0.001f)
            FaceDirectionSmooth(toAim.normalized);

        // --- Move ---
        transform.position += transform.forward * _currentSpeedMps * Time.deltaTime;

        // --- Waypoint reach (fallback / non-stop triggers) ---
        Transform wp = _path[_i];

        Vector3 p = transform.position; p.y = 0f;
        Vector3 w = wp.position; w.y = 0f;

        float effectiveRadius = waypointRadius * Mathf.Max(0.1f, behaviour.radiusMultiplier);

        if (Vector3.Distance(p, w) <= effectiveRadius) {
            if (debugLogs) Debug.Log($"{name} reached WP index {_i} ({wp.name})");

            if (behaviour.despawnHere) {
                if (debugLogs) Debug.Log($"{name} despawn at {_i}");
                Destroy(gameObject);
                return;
            }

            // Safety fallback if we reached without braking properly.
            if (behaviour.waitForGreenHere) {
                if (behaviour.signal == null) {
                    Debug.LogWarning($"{name}: Waypoint {_i} has waitForGreenHere enabled but no signal assigned. Continuing.");
                }
                else {
                    StartWaitForGreen(behaviour.signal, behaviour.stopOnYellow, _i);
                    return;
                }
            }

            if (behaviour.stopHere) {
                StartTimedStop(behaviour.stopSeconds, _i);
                return;
            }

            if (behaviour.startTimedDrive) {
                if (debugLogs) Debug.Log($"{name} start timed drive at {_i}");
                StartTimedDrive();
                return;
            }

            _i = Mathf.Min(_i + 1, _path.Length - 1);
        }
    }

    // ---------------- Stop-ahead detection ----------------

    enum StopReason { None, StopHere, Signal }

    bool TryGetActiveStopTarget(out int stopIndex, out StopReason reason, out RoadSignalDurationController signal) {
        stopIndex = -1;
        reason = StopReason.None;
        signal = null;

        // Scan forward so braking can begin BEFORE we reach the stop waypoint.
        int start = Mathf.Clamp(_i, 0, _path.Length - 1);
        int lookAhead = Mathf.Max(0, stopLookAheadWaypoints);
        int end = Mathf.Min(_path.Length - 1, start + lookAhead);

        for (int idx = start; idx <= end; idx++) {
            WaypointBehaviour b = waypointBehaviours[idx];

            // 1) Traffic signal stop if red/yellow
            if (b.waitForGreenHere && b.signal != null) {
                bool mustStop =
                    b.signal.Current == RoadSignals.Red ||
                    (b.stopOnYellow && b.signal.Current == RoadSignals.Yellow);

                if (mustStop) {
                    stopIndex = idx;
                    reason = StopReason.Signal;
                    signal = b.signal;
                    return true;
                }
            }

            // 2) Timed stop
            if (b.stopHere) {
                stopIndex = idx;
                reason = StopReason.StopHere;
                return true;
            }
        }

        return false;
    }

    // ---------------- Signal waiting ----------------
    void StartWaitForGreen(RoadSignalDurationController signal, bool stopOnYellow, int stopIndex) {
        UnsubscribeSignal();

        bool mustStop =
            signal.Current == RoadSignals.Red ||
            (stopOnYellow && signal.Current == RoadSignals.Yellow);

        if (!mustStop) {
            _i = Mathf.Min(stopIndex + 1, _path.Length - 1);
            return;
        }

        _waiting = true;
        _waitingForGreen = true;
        _currentSpeedMps = 0f;

        _signalStopIndex = stopIndex;

        _activeSignal = signal;
        _activeSignal.eventHandler.AddListener(OnSignalChanged);

        if (debugLogs) Debug.Log($"{name} WAITING for Green at wp={stopIndex}");
    }

    void OnSignalChanged(RoadSignals s) {
        if (!_waitingForGreen) return;

        if (debugLogs) Debug.Log($"{name} signal changed: {s}");

        if (s == RoadSignals.Green) {
            _waitingForGreen = false;
            _waiting = false;

            UnsubscribeSignal();

            // If we were braking toward a future stopIndex, advance from that index.
            int next = (_signalStopIndex >= 0) ? (_signalStopIndex + 1) : (_i + 1);
            _signalStopIndex = -1;
            _i = Mathf.Min(next, _path.Length - 1);
        }
    }

    void UnsubscribeSignal()
    {
        if (_activeSignal != null)
            _activeSignal.eventHandler.RemoveListener(OnSignalChanged);

        _activeSignal = null;
        _signalStopIndex = -1;
    }

    // ---------------- Timed stop ----------------

    void StartTimedStop(float seconds, int stopIndex) {
        if (_stopRoutine != null) StopCoroutine(_stopRoutine);
        _stopRoutine = StartCoroutine(StopCoroutine(seconds, stopIndex));
    }

    IEnumerator StopCoroutine(float seconds, int stopIndex) {
        _waiting = true;
        _currentSpeedMps = 0f;

        if (debugLogs) Debug.Log($"{name} STOP for {seconds}s at wp={stopIndex}");

        yield return new WaitForSeconds(seconds);

        _waiting = false;
        _i = Mathf.Min(stopIndex + 1, _path.Length - 1);
    }

    // ---------------- Movement ----------------

    Vector3 GetAimPoint() {
        if (_i + 1 >= _path.Length)
            return _path[_i].position;

        Transform cur = _path[_i];
        Transform next = _path[_i + 1];

        float d = Vector3.Distance(transform.position, cur.position);
        float t = Mathf.InverseLerp(lookAheadDistance, 0f, d);
        return Vector3.Lerp(cur.position, next.position, t);
    }

    void FaceDirectionSmooth(Vector3 dir) {
        Quaternion target = Quaternion.LookRotation(dir, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, maxYawDegPerSec * Time.deltaTime);
    }

    // ---------------- Timed drive ----------------

    void StartTimedDrive() {
        _timedDrive = true;
        _timedDriveTimer = 0f;

        Vector3 a = _path[_path.Length - 2].position;
        Vector3 b = _path[_path.Length - 1].position;
        _timedDriveDir = (b - a).normalized;
    }

    void TimedDriveUpdate() {
        _currentSpeedMps = Mathf.MoveTowards(_currentSpeedMps, CruiseMps, AccelMps2 * Time.deltaTime);

        FaceDirectionSmooth(_timedDriveDir);
        transform.position += transform.forward * _currentSpeedMps * Time.deltaTime;

        _timedDriveTimer += Time.deltaTime;
        if (_timedDriveTimer >= timedDriveSeconds)
            ResetToStart();
    }

    // ---------------- Optional: Pass distance (bike -> car RIGHT side) ----------------

    /// <summary>
    /// Adjusts WP0/WP1 laterally so the vehicle passes the bike at a desired distance measured
    /// from the BIKE to the CAR'S RIGHT SIDE (right edge), assuming the car passes on the left of the bike.
    /// Keeps each waypoint's along-road placement (only moves sideways).
    /// </summary>
    public void AdjustFirstTwoWaypointsForPassDistanceFromBikeToCarRightSide(
        Transform bikeRoot,
        float desiredBikeToCarRightSideMeters
    ) {
        if (bikeRoot == null) { Debug.LogWarning($"{name}: bikeRoot is null"); return; }
        if (_path == null || _path.Length < 2) { Debug.LogWarning($"{name}: Path missing/too short."); return; }

        // Road forward from existing waypoints (preserves authored direction)
        Vector3 fwd = _path[1].position - _path[0].position;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.0001f)
        {
            fwd = transform.forward;
            fwd.y = 0f;
        }
        fwd.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
        Vector3 left = -right;

        // Measure car center -> right edge distance (half-width)
        float halfWidth = Mathf.Max(0.05f, fallbackHalfWidthMeters);
        if (rightSideReference != null) {
            Vector3 center = transform.position; center.y = 0f;
            Vector3 refPos = rightSideReference.position; refPos.y = 0f;

            float signed = Vector3.Dot(refPos - center, right); // right edge should be negative
            float measured = Mathf.Abs(signed);
            if (measured >= 0.05f) halfWidth = measured;
        }

        float desired = Mathf.Max(0f, desiredBikeToCarRightSideMeters);

        // To achieve bike->carRightEdge = desired, place car centerline further right by halfWidth
        float bikeToCenterline = desired + halfWidth;

        Vector3 bikePos = bikeRoot.position;
        bikePos.y = 0f;

        for (int k = 0; k < 2; k++) {
            Vector3 wp = _path[k].position;

            Vector3 wpFlat = wp; wpFlat.y = 0f;
            Vector3 rel = wpFlat - bikePos;

            float along = Vector3.Dot(rel, fwd);

            Vector3 newFlat = bikePos + fwd * along + left * bikeToCenterline;

            // Preserve original waypoint height
            _path[k].position = new Vector3(newFlat.x, wp.y, newFlat.z);
        }

        if (debugLogs)
            Debug.Log($"{name}: Pass distance set. bike->carRightEdge={desired:F2}m, halfWidth={halfWidth:F2}m, bike->centerline={bikeToCenterline:F2}m");
    }

    // ---------------- Helpers ----------------

    static float FlatDistance(Vector3 a, Vector3 b) {
        a.y = 0f; b.y = 0f;
        return Vector3.Distance(a, b);
    }

    // ---------------- Reset ----------------

    void ResetToStart() {
        if (_path == null || _path.Length < 2) return;

        UnsubscribeSignal();
        _waitingForGreen = false;

        transform.position = _path[0].position;

        Vector3 dir = _path[1].position - _path[0].position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);

        _currentSpeedMps = CruiseMps;
        _i = 1;
        _waiting = false;
        _timedDrive = false;
    }
}
