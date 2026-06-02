using UnityEngine;

public class VehicleMaterialController : MonoBehaviour{
    [SerializeField] private Material blackMat;
    [SerializeField] private Material greyMat;
    [SerializeField] private Material whiteMat;
    [SerializeField] private Material redMat;
    [SerializeField] private Material yellowMat;
    [SerializeField] private Material greenMat;
    [SerializeField] private Material orangeMat;

    private MeshRenderer meshRenderer;

    void Awake(){
        meshRenderer = GetComponent<MeshRenderer>();
    }

    void OnEnable(){
        float rndValue = Random.Range(0,200);

        if(rndValue >= 0 && rndValue < 25){
            meshRenderer.material = blackMat;
        }
        if(rndValue >= 25 && rndValue < 50){
            meshRenderer.material = redMat;
        }
        if(rndValue >= 50 && rndValue < 75){
            meshRenderer.material = yellowMat;
        }
        if(rndValue >= 75 && rndValue < 100){
            meshRenderer.material = greenMat;
        }
        if(rndValue >= 100 && rndValue < 125){
            meshRenderer.material = greyMat;
        }
        if(rndValue >= 125 && rndValue < 150){
            meshRenderer.material = whiteMat;
        }
        if(rndValue >= 150 && rndValue < 175){
            meshRenderer.material = whiteMat;
        }
        if(rndValue >= 175 && rndValue < 200){
            meshRenderer.material = orangeMat;
        }
    }
}
