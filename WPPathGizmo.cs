using UnityEngine;

[ExecuteAlways]
public class WPPathGizmo : MonoBehaviour
{
    [Header("Display")]
    public bool drawGizmos = true;
    public bool drawLabels = true;
    public float sphereRadius = 0.25f;
    public float labelYOffset = 0.5f;

    [Header("Line")]
    public bool drawLines = true;
    public bool loopLine = false;   // visual only

    void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        int count = transform.childCount;
        if (count == 0) return;

        // Draw spheres + lines
        for (int i = 0; i < count; i++)
        {
            Transform wp = transform.GetChild(i);
            if (wp == null) continue;

            Gizmos.DrawSphere(wp.position, sphereRadius);

            if (drawLines && i < count - 1)
            {
                Transform next = transform.GetChild(i + 1);
                if (next != null)
                    Gizmos.DrawLine(wp.position, next.position);
            }
        }

        // Optional loop line (purely visual)
        if (drawLines && loopLine && count > 1)
        {
            Transform first = transform.GetChild(0);
            Transform last = transform.GetChild(count - 1);
            if (first != null && last != null)
                Gizmos.DrawLine(last.position, first.position);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!drawGizmos || !drawLabels) return;

        int count = transform.childCount;
        for (int i = 0; i < count; i++)
        {
            Transform wp = transform.GetChild(i);
            if (wp == null) continue;

            Vector3 pos = wp.position + Vector3.up * labelYOffset;
            UnityEditor.Handles.Label(pos, $"{i}: {wp.name}");
        }
    }
#endif
}
