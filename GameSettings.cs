using UnityEngine.SceneManagement;
using UnityEngine;

public class GameSettings : MonoBehaviour{
    [SerializeField] private GameObject currentBike;
    [SerializeField] private Transform scenarioOnePoint;
    [SerializeField] private Transform scenarioTwoPoint;
    [SerializeField] private Transform scenarioThreePoint;
    [SerializeField] private Transform scenarioFourPoint;
    public Scene endScene;

    void Start(){
        Screen.SetResolution(1920,1080, false);
        // Application.targetFrameRate = 120;
    }

    void Update(){
        if(Input.GetKeyDown(KeyCode.F1)){
            currentBike.transform.position = scenarioOnePoint.position + new Vector3(0,0.3f,0);
            currentBike.transform.rotation = scenarioOnePoint.rotation;
        }
        if(Input.GetKeyDown(KeyCode.F2)){
            currentBike.transform.position = scenarioTwoPoint.position + new Vector3(0,0.3f,0);
            currentBike.transform.rotation = scenarioTwoPoint.rotation;
        }
        if(Input.GetKeyDown(KeyCode.F3)){
            if(scenarioThreePoint != null){
                currentBike.transform.position = scenarioThreePoint.position + new Vector3(0,1,0);
                currentBike.transform.rotation = scenarioThreePoint.rotation;
            }
        }
        if(Input.GetKeyDown(KeyCode.F4)){
            if(scenarioFourPoint != null){
                currentBike.transform.position = scenarioFourPoint.position + new Vector3(0,1,0);
                currentBike.transform.rotation = scenarioFourPoint.rotation;
            }
        }

        if(Input.GetKeyDown(KeyCode.R)){
            SceneManager.LoadScene("ForestRoad");
        }
    }

    public void StopGame(){
        // Application.Quit();
        SceneManager.LoadScene("EndScene",LoadSceneMode.Single);
    }
}
