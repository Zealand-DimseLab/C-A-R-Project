using UnityEngine;

public class Steer : MonoBehaviour{
    [SerializeField] private ArduinoSerialReader arduinoSerialReader;
    [SerializeField] private Transform questControllerTransform;

    private float speed => arduinoSerialReader.speed;

    public float turnSpeedModifier;

    void Update(){
        Vector3 cross = Vector3.Cross(transform.forward, questControllerTransform.forward);

        if(speed > 0.3f)
            transform.rotation *= Quaternion.Euler(0,cross.y * turnSpeedModifier * Time.deltaTime,0);
    }
}
