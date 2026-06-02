using UnityEngine;

public class BikeSteerController : MonoBehaviour{
    [SerializeField] private Transform questController;
    [SerializeField] private Transform bike;

    void Update(){
        transform.forward = -questController.up;
        // float finalYRot = questController.rotation.y;

        // float clamper = Mathf.Clamp(finalYRot,0,180);
        // transform.rotation = Quaternion.Euler(
        //         0,
        //         questController.rotation.y * Mathf.Rad2Deg * multiplier + rotOffset,
        //         0
        //         ) * bike.rotation;
        // transform.rotation = Quaternion.Euler(
        //         0,
        //         finalYRot * Mathf.Rad2Deg * multiplier + rotOffset,
        //         0
        //         ) * bike.rotation;
    }
}

