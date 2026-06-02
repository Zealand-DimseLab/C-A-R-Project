using UnityEngine;

public class GroundSensor : MonoBehaviour{
    public bool isGrounded;
    public LayerMask ground;

    public float length;

    void Update(){
        isGrounded = Physics.Raycast(transform.position,-Vector3.up, length, ground);
    }

    void OnDrawGizmos(){
        Gizmos.DrawRay(transform.position,-Vector3.up * length);
    }
}
