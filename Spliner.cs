using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

[RequireComponent(typeof(SplineContainer))]
public class SineSplineGenerator : MonoBehaviour
{
    [Header("Sine Settings")]
    public int pointCount = 20;
    public float spacing = 1.0f;
    public float amplitude = 2.0f;
    public float frequency = 0.5f;

    [ContextMenu("Generate Sine Spline")]
    public void Generate()
    {
        var container = GetComponent<SplineContainer>();
        Spline spline = container.Spline;

        // Clear existing knots
        spline.Clear();

        for (int i = 0; i < pointCount; i++)
        {
            float x = i * spacing;
            float y = amplitude * Mathf.Sin(frequency * x);

            // Calculate the slope (derivative) for precise tangents: dy/dx = A * k * cos(k * x)
            float slope = amplitude * frequency * Mathf.Cos(frequency * x);
            float3 tangent = math.normalize(new float3(1, slope, 0)) * (spacing * 0.33f);

            BezierKnot knot = new BezierKnot
            {
                Position = new float3(x, y, 0),
                TangentIn = -tangent,
                TangentOut = tangent
            };

            spline.Add(knot, TangentMode.Continuous);
        }

        // Optional: Set tangent mode to Auto to smooth the wave
        for (int i = 0; i < spline.Count; i++)
        {
            spline.SetTangentMode(i, TangentMode.AutoSmooth);
        }
    }
}
