using UnityEngine;

[CreateAssetMenu(fileName = "GrassDataAssetDOD", menuName = "Scriptable Objects/GrassDataAssetDOD")]
public class GrassDataAssetDOD : ScriptableObject {
    // public GrassData[] matrices;
    [HideInInspector] public Vector3[] positions;
    [HideInInspector] public float[] yaws;
    [HideInInspector] public float[] scales;
}
