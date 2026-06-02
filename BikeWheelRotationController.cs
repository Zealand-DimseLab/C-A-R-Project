using UnityEngine;

public class BikeWheelRotationController : MonoBehaviour{
    [SerializeField] private ArduinoSerialReader arduinoReader;

    private float speed => arduinoReader.speed;

    float xRot;
    void Update(){
        xRot += speed;
        transform.localRotation = Quaternion.Euler(xRot,0,0);
    }
}
