using UnityEngine;

[DisallowMultipleComponent]
public class WPWheelVisuals : MonoBehaviour
{
    [Header("Source")]
    [Tooltip("WPVehicleController that drives this vehicle.")]
    public WPVehicleController controller;

    [Header("Steer pivots (front wheels)")]
    [Tooltip("Empty parent of the FL wheel mesh. Rotates around local Y for steering.")]
    public Transform frontLeftSteerPivot;

    [Tooltip("Empty parent of the FR wheel mesh. Rotates around local Y for steering.")]
    public Transform frontRightSteerPivot;

    [Header("Wheel meshes (spin/roll)")]
    public Transform frontLeftWheelMesh;
    public Transform frontRightWheelMesh;
    public Transform rearLeftWheelMesh;
    public Transform rearRightWheelMesh;

    [Header("Wheel / steering settings")]
    [Tooltip("Wheel radius in meters. Typical ~0.30 - 0.36.")]
    public float wheelRadiusMeters = 0.34f;

    [Tooltip("If speed is below this (m/s), wheels stop rotating.")]
    public float stopEpsilonMps = 0.02f;

    [Tooltip("Max visual steering angle (degrees).")]
    public float maxSteerAngleDeg = 28f;

    [Tooltip("How quickly steering visuals respond (deg/sec).")]
    public float steerResponseDegPerSec = 180f;

    [Tooltip("If yaw rate is below this (deg/sec), steer visual returns to center.")]
    public float yawRateDeadzoneDegPerSec = 2f;

    [Tooltip("Yaw rate at which we hit full steering angle. Lower = more steering for small turns.")]
    public float yawRateForMaxSteerDegPerSec = 60f;

    [Header("Auto-find")]
    public bool autoFindController = true;

    // Internals
    Quaternion _flSteerBase;
    Quaternion _frSteerBase;

    float _steerAngleCurrent;
    float _lastYaw;
    bool _hasLastYaw;

    void Awake()
    {
        if (autoFindController && controller == null)
            controller = GetComponentInParent<WPVehicleController>();

        // Store “straight ahead” baseline so we can add steer on top
        if (frontLeftSteerPivot != null) _flSteerBase = frontLeftSteerPivot.localRotation;
        if (frontRightSteerPivot != null) _frSteerBase = frontRightSteerPivot.localRotation;
    }

    void Update()
    {
        if (controller == null) return;

        UpdateWheelSpin();
        UpdateSteeringVisual();
    }

    void UpdateWheelSpin()
    {
        float speedMps = Mathf.Abs(controller.CurrentSpeedMps);
        if (speedMps < stopEpsilonMps) return;

        float dist = speedMps * Time.deltaTime;
        float circumference = 2f * Mathf.PI * Mathf.Max(0.001f, wheelRadiusMeters);
        float degrees = (dist / circumference) * 360f;

        // Your model spins around LOCAL X
        RotateWheelX(frontLeftWheelMesh, degrees);
        RotateWheelX(frontRightWheelMesh, degrees);
        RotateWheelX(rearLeftWheelMesh, degrees);
        RotateWheelX(rearRightWheelMesh, degrees);
    }

    void RotateWheelX(Transform wheel, float degrees)
    {
        if (wheel == null) return;
        wheel.Rotate(Vector3.right, degrees, Space.Self);
    }

    void UpdateSteeringVisual()
    {
        // We approximate steering angle from yaw rate (how fast the car is turning).
        float yaw = transform.eulerAngles.y;

        if (!_hasLastYaw)
        {
            _lastYaw = yaw;
            _hasLastYaw = true;
            return;
        }

        float yawDelta = Mathf.DeltaAngle(_lastYaw, yaw);
        _lastYaw = yaw;

        float yawRate = yawDelta / Mathf.Max(Time.deltaTime, 0.0001f); // deg/sec

        // Deadzone -> return to center
        float targetSteer = 0f;

        if (Mathf.Abs(yawRate) > yawRateDeadzoneDegPerSec)
        {
            // Map yawRate to steer angle
            float t = Mathf.Clamp(yawRate / Mathf.Max(1f, yawRateForMaxSteerDegPerSec), -1f, 1f);
            targetSteer = t * maxSteerAngleDeg;
        }

        // Smooth steer motion
        _steerAngleCurrent = Mathf.MoveTowards(
            _steerAngleCurrent,
            targetSteer,
            steerResponseDegPerSec * Time.deltaTime
        );

        // Apply to steer pivots around LOCAL Y (baseline + yaw)
        if (frontLeftSteerPivot != null)
            frontLeftSteerPivot.localRotation = _flSteerBase * Quaternion.Euler(0f, _steerAngleCurrent, 0f);

        if (frontRightSteerPivot != null)
            frontRightSteerPivot.localRotation = _frSteerBase * Quaternion.Euler(0f, _steerAngleCurrent, 0f);
    }
}
