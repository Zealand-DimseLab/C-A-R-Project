using UnityEngine;

public class LoggingStarter : MonoBehaviour{
    public static bool startLogging;
    private bool dummy1;
    private bool dummy2;
    private bool dummy3;

    void Start(){
        startLogging = false;
        gameObject.SetActive(true);
    }

    void OnTriggerEnter(Collider collider){
        if(collider.CompareTag("Player")){
            Debug.Log(collider.gameObject.name);
            startLogging = true;
            gameObject.SetActive(false);
        }
    }
}
