using UnityEngine;

public class BikeWheelController : MonoBehaviour{
    [SerializeField] private Transform bikeSteer;

    void Update(){
        transform.rotation = bikeSteer.rotation;
        // transform.rotation = Quaternion.Euler(0,bikeSteer.rotation.y * Mathf.Rad2Deg,0);
    }
}
