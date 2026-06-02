using UnityEngine;

public class WorldGridData_Tree : MonoBehaviour{
    public bool showGizmos;

    [Header("Resources")]
    public Mesh meshLOD0;
    public Mesh meshLOD1;
    public Mesh meshLOD2;
    public Material trunkMaterial;
    public Material leavesMaterial;
    public ComputeShader cullingShader;

    public static int gridCount;

    public GameObject treePlacer;

    public Vector2 gridSize;
    public int gridWidth;
    public int gridLength;

    public float LOD0Distance;
    public float LOD1Distance;
    public float MaxDistance;

    public string folderName;

    [ContextMenu("Bake All Trees")]
    void BakeAllTrees(){
        for(int i = 0; i < gridWidth; i++){
            for(int l = 0; l < gridLength; l++){
                GameObject inst = Instantiate(
                        treePlacer,
                        new Vector3(transform.position.x + i * gridSize.x, 0,transform.position.z + l * gridSize.y),
                        Quaternion.identity);

                PoissonIndirectPlacer_Bake_Tree pb = inst.GetComponent<PoissonIndirectPlacer_Bake_Tree>();

                pb.newFileName = $"{transform.name}" + gridCount++;
                pb.folderName = this.folderName;
                pb.regionSize = gridSize;

                pb.meshLOD0 = this.meshLOD0;
                pb.meshLOD1 = this.meshLOD1;
                pb.meshLOD2 = this.meshLOD2;
                pb.trunkMaterial = this.trunkMaterial;
                pb.leavesMaterial = this.leavesMaterial;
                pb.cullingShader = this.cullingShader;

                pb.maxDistance = MaxDistance;
                pb.lod0Distance = LOD0Distance;
                pb.lod1Distance = LOD1Distance;

                pb.BakeTree();

                inst.transform.SetParent(transform);
            }
        }
    }

    void OnDrawGizmos(){
        if(!showGizmos) return;

        Gizmos.color = Color.yellow;
        Vector3 gridCenter = new(
                transform.position.x + (gridSize.x * gridWidth * 0.5f) - gridSize.x * 0.5f,
                0,
                transform.position.z + (gridSize.y * gridLength * 0.5f) - gridSize.y * 0.5f
                );
        Gizmos.DrawWireCube(gridCenter, new Vector3(gridWidth * gridSize.x, 0.1f, gridLength * gridSize.y));
    }
}

