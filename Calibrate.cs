using UnityEngine;

public class SteerFromMountedController : MonoBehaviour {
    public Transform trackedController; // XR tracked controller
    public Transform bikeRoot;          // rotates bike/world
    public Transform forwardRef;        // a ref that defines "forward" at calibration

    float zeroYaw;

    [SerializeField] private OVRManager oVRManager;

    [ContextMenu("Calibrate Center")]
    public void Calibrate()
    {
        zeroYaw = GetYaw(trackedController.rotation) - GetYaw(forwardRef.rotation);
    }
    void Start(){
        // OVRManager oVRManager = new();
    }

    void Update()
    {
        float yaw = GetYaw(trackedController.rotation) - zeroYaw;
        bikeRoot.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    static float GetYaw(Quaternion q)
    {
        Vector3 fwd = Vector3.ProjectOnPlane(q * Vector3.forward, Vector3.up).normalized;
        return Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg;
    }

    public void TryToCal(){
    }
}
