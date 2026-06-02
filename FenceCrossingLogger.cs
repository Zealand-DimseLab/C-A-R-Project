using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[DisallowMultipleComponent]
public class FenceTouchLogger : MonoBehaviour
{
    [Header("Fence (children are points; segments are between consecutive points)")]
    public Transform fenceRoot;

    [Header("Bike")]
    [Tooltip("Drag Player_CykelScan_V2 here (the object that moves).")]
    public Transform bikeRoot;

    [Header("Touch settings")]
    [Tooltip("Touch distance to the fence line in meters (XZ plane). Example: 0.10 = 10cm.")]
    public float touchDistanceMeters = 0.15f;

    public float minMoveDistanceMeters = 0.005f;
    public float cooldownSeconds = 0.5f;

    [Header("Wall height band (relative to bike sample point)")]
    public float wallHalfHeightMeters = 1.0f;
    public float wallHalfDepthMeters = 1.0f;

    [Header("Saving")]
    public string fileName = "fence_touches.json";
    public bool saveOnEachEvent = true;

    [Header("Debug")]
    public bool debugLogs = false;

    Vector3 _lastPos;
    bool _hasLast;
    float _cooldown;

    CharacterController _cc; // auto-found

    FenceLog _log = new FenceLog();
    string FilePath => Path.Combine(Application.persistentDataPath, fileName);

    [Serializable]
    public class FenceTouchEvent
    {
        public int segmentIndex;
        public string timeIso8601;
        public float distanceMeters;
        public float x, y, z;
    }

    [Serializable]
    public class FenceLog
    {
        public List<FenceTouchEvent> events = new List<FenceTouchEvent>();
    }

    void Awake()
    {
        if (bikeRoot != null)
            _cc = bikeRoot.GetComponent<CharacterController>();
    }

    void Start()
    {
        if (bikeRoot != null)
        {
            _lastPos = GetBikeSamplePoint();
            _hasLast = true;
        }

        if (debugLogs)
            Debug.Log($"{name}: PersistentDataPath = {Application.persistentDataPath}");
    }

    void Update()
    {
        if (bikeRoot == null) return;
        if (fenceRoot == null || fenceRoot.childCount < 2) return;

        float dt = Time.deltaTime;
        if (_cooldown > 0f) _cooldown -= dt;

        Vector3 cur = GetBikeSamplePoint();

        if (!_hasLast)
        {
            _lastPos = cur;
            _hasLast = true;
            return;
        }

        if ((cur - _lastPos).sqrMagnitude < minMoveDistanceMeters * minMoveDistanceMeters)
            return;

        if (_cooldown > 0f)
        {
            _lastPos = cur;
            return;
        }

        float yMin = cur.y - wallHalfDepthMeters;
        float yMax = cur.y + wallHalfHeightMeters;

        Vector2 p = new Vector2(cur.x, cur.z);

        float bestDist = float.PositiveInfinity;
        Vector2 bestPoint = default;
        int bestSeg = -1;

        for (int i = 0; i < fenceRoot.childCount - 1; i++)
        {
            Vector3 a3 = fenceRoot.GetChild(i).position;
            Vector3 b3 = fenceRoot.GetChild(i + 1).position;

            float segMinY = Mathf.Min(a3.y, b3.y);
            float segMaxY = Mathf.Max(a3.y, b3.y);
            if (segMaxY < yMin || segMinY > yMax)
                continue;

            Vector2 a = new Vector2(a3.x, a3.z);
            Vector2 b = new Vector2(b3.x, b3.z);

            Vector2 closest = ClosestPointOnSegment2D(a, b, p);
            float dist = Vector2.Distance(p, closest);

            if (dist < bestDist)
            {
                bestDist = dist;
                bestPoint = closest;
                bestSeg = i;
            }
        }

        if (bestSeg >= 0 && bestDist <= Mathf.Max(0f, touchDistanceMeters))
        {
            float y = cur.y;

            var ev = new FenceTouchEvent
            {
                segmentIndex = bestSeg,
                timeIso8601 = DateTime.UtcNow.ToString("o"),
                distanceMeters = bestDist,
                x = bestPoint.x,
                y = y,
                z = bestPoint.y
            };

            _log.events.Add(ev);

            if (debugLogs)
                Debug.Log($"{name}: Fence TOUCH seg={ev.segmentIndex} dist={ev.distanceMeters:F3} at ({ev.x:F2},{ev.y:F2},{ev.z:F2})");

            if (saveOnEachEvent)
                SaveToJson();

            _cooldown = Mathf.Max(0f, cooldownSeconds);
        }

        _lastPos = cur;
    }

    Vector3 GetBikeSamplePoint()
    {
        // If a CharacterController exists, use its world-space capsule center
        if (_cc != null)
            return _cc.transform.TransformPoint(_cc.center);

        return bikeRoot.position;
    }

    public void SaveToJson()
    {
        try
        {
            string json = JsonUtility.ToJson(_log, true);
            File.WriteAllText(FilePath, json);
            Debug.Log($"{name}: Saved fence log -> {FilePath} (events={_log.events.Count})");
        }
        catch (Exception e)
        {
            Debug.LogError($"{name}: Failed to write JSON to {FilePath}\n{e}");
        }
    }

    static Vector2 ClosestPointOnSegment2D(Vector2 a, Vector2 b, Vector2 p)
    {
        Vector2 ab = b - a;
        float denom = Vector2.Dot(ab, ab);

        if (denom < 1e-8f)
            return a;

        float t = Vector2.Dot(p - a, ab) / denom;
        t = Mathf.Clamp01(t);
        return a + t * ab;
    }
}
