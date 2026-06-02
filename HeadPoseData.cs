using UnityEngine;

//be able to write to disk
//total bytes 32 bytes
[System.Serializable]
public struct HeadPoseData{
    // //consists of 3 floats x,y,z
    // public Vector3 headPos;
    //12 bytes total
    public float xPos;
    public float yPos;
    public float zPos;
    // //consists of 4 floats x,y,z,w
    // public Quaternion headRot;
    //16 bytes total
    public float xRot;
    public float yRot;
    public float zRot;
    public float wRot;
}
