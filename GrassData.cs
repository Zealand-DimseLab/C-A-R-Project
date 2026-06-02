using UnityEngine;

//be able to write to disk
[System.Serializable]
public struct GrassData{
    //12 bytes. Consists of 3 floats x,y,z
    public Vector3 Position;
    //4 bytes
    public float Yaw;
    //4 bytes
    public float Scale;
}
