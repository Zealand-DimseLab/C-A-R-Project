using UnityEngine;

public class CarWheelsController : MonoBehaviour{
    [SerializeField] private WPVehicleController wPVehicleController;
    private Transform pTransform;
    private Rigidbody p_rb;

    void Awake(){
        pTransform = GetComponentInParent<Transform>();
        p_rb = GetComponentInParent<Rigidbody>();
    }

    public float speedAdder;

    float adder;
    void Update(){
        Vector3 rbSpeed = p_rb.linearVelocity;

        adder += -rbSpeed.magnitude * speedAdder * Time.deltaTime;

        if(rbSpeed.x > 0){
            transform.localRotation = Quaternion.Euler(adder,0,0);
        }
    }
}
