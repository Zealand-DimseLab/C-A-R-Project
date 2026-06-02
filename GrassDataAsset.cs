using UnityEngine;

[CreateAssetMenu(fileName = "GrassDataAsset", menuName = "Scriptable Objects/GrassDataAsset")]
public class GrassDataAsset : ScriptableObject {
    [HideInInspector] public GrassData[] matrices;
}
