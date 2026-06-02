using UnityEngine;

public class MaterialFitter : MonoBehaviour{
    private Material mat;

    void Awake(){
        mat = GetComponent<MeshRenderer>().material;
    }

    void Start(){
    }
}
