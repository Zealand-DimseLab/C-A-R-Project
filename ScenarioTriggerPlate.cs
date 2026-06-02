// ScenarioTriggerPlate.cs
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

[DisallowMultipleComponent]
public class ScenarioTriggerPlate : MonoBehaviour {
    [SerializeField] private BoxCollider boxCollider;

    [Header("Python Connectivity")]
    public bool sendUDP;
    private UdpClient client;
    public string ip = "127.0.0.1";
    public int port = 8765;

    [Header("Bike Reference (drag your bike root here)")]
    public Transform bikeRoot;

    [Header("Trigger")]
    public bool triggerOnce = true;

    [Header("Vehicles to start (drag vehicle GameObjects here)")]
    public GameObject[] vehicleObjects;

    [Header("Optional: Offset first 2 waypoints relative to bike")]
    public bool offsetFirstTwoWaypoints = false;

    [Tooltip("Meters to the LEFT of the bike (example: 0.75 / 1.5 / 2.0)")]
    public float firstTwoWpLateralOffsetMeters = 1.5f;

    [Header("Debug")]
    public bool debugLogs = false;

    bool _fired;

    void Start(){
        client = new UdpClient();
    }

    void Reset() {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other) {
        if (triggerOnce && _fired) return;

        if (bikeRoot == null) {
            Debug.LogWarning($"{name}: No bikeRoot assigned.");
            return;
        }

        // Trigger if collider belongs to bikeRoot hierarchy
        if (!(other.transform == bikeRoot || other.transform.IsChildOf(bikeRoot)))
            return;

        if (debugLogs) Debug.Log($"{name}: Triggered by {other.name}");

        if (vehicleObjects != null) {
            foreach (var go in vehicleObjects) {
                if (go == null) continue;

                var v = go.GetComponent<WPVehicleController>();
                if (v == null) {
                    Debug.LogWarning($"{name}: {go.name} has no WPVehicleController component.");
                    continue;
                }

                if (offsetFirstTwoWaypoints)
                    v.AdjustFirstTwoWaypointsForPassDistanceFromBikeToCarLeftSide(bikeRoot, firstTwoWpLateralOffsetMeters);

                v.TriggerStart();

                if (debugLogs) Debug.Log($"{name}: Started {go.name}");
            }
        }

        _fired = true;

        if(sendUDP){
            byte[] data = Encoding.UTF8.GetBytes(transform.gameObject.name);
            client.Send(data,data.Length, ip, port);
            Debug.Log("Sent to Python");
        }
    }

    void OnDisable(){
        client.Close();
    }

    void OnDrawGizmos()
    {
        if (boxCollider == null) return;

        Gizmos.color = Color.green;
        Gizmos.matrix = boxCollider.transform.localToWorldMatrix;
        Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
    }
}
