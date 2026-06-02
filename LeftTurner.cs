using UnityEngine;

public class LeftTurner : MonoBehaviour{
    [SerializeField] private Transform bikeTransform;
    [SerializeField] private Transform questControllerTransform;
    [SerializeField] private ArduinoSerialReader arduinoSerialReader;

    private float speed => arduinoSerialReader.speed;

    public float turnSpeed;

    void OnTriggerStay(Collider collider){
        if(speed <= 0) return;

        if(collider.CompareTag("TurnPole")){
            bikeTransform.rotation *= Quaternion.Euler(0f, -turnSpeed, 0f);

            // bikeTransform.rotation = Quaternion.RotateTowards(bikeTransform.rotation, currentRot, deltaRot * Time.deltaTime);
        }
    }
}
