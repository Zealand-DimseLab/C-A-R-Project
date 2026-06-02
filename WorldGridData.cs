using UnityEngine;

public class WorldGridData : MonoBehaviour{
    public bool showGizmos;

    [Header("Resources")]
    public GameObject gameObject1;
    public GameObject[] allGameObjects;
    public Mesh meshLOD0;
    public Mesh meshLOD1;
    public Mesh meshLOD2;
    public Mesh meshLOD3;
    public Material material;
    public ComputeShader cullingShader;
    public Vector2 scaleRange;
    public float minDistance;
    public float roadClearanceRadius;

    public static int gridCount;

    public GameObject grassPlacerOOP;
    public GameObject grassPlacerDOD;

    public Vector2 gridSize;
    public int gridWidth;
    public int gridLength;

    public float LOD0Distance;
    public float LOD1Distance;
    public float LOD2Distance;
    public float MaxDistance;

    [SerializeField] private string folderName;

    public LayerMask wantedLayer;
    public LayerMask obstacleLayer;

    [ContextMenu("Bake All Grass OOP")]
    void BakeAllGrassOOP(){
#if UNITY_EDITOR
        for(int i = 0; i < gridWidth; i++){
            for(int l = 0; l < gridLength; l++){
                GameObject inst = Instantiate(
                        grassPlacerOOP,
                        new Vector3(transform.position.x + i * gridSize.x, 0,transform.position.z + l * gridSize.y),
                        Quaternion.identity);

                PoissonIndirectPlacer_Bake pb = inst.GetComponent<PoissonIndirectPlacer_Bake>();

                pb.newFileName = $"{transform.name}" + gridCount++;
                pb.folderName = this.folderName;
                pb.regionSize = gridSize;
                pb.scaleRange = this.scaleRange;

                pb.meshLOD0 = this.meshLOD0;
                pb.meshLOD1 = this.meshLOD1;
                pb.meshLOD2 = this.meshLOD2;
                pb.meshLOD3 = this.meshLOD3;
                pb.material = this.material;
                pb.cullingShader = this.cullingShader;
                pb.minDistance = this.minDistance;
                pb.roadClearanceRadius = this.roadClearanceRadius;
                pb.groundMask = wantedLayer;
                pb.obstacleMask = this.obstacleLayer;

                pb.maxDistance = MaxDistance;
                pb.lod0Distance = LOD0Distance;
                pb.lod1Distance = LOD1Distance;
                pb.lod2Distance = LOD2Distance;

                pb.BakeGrass();

                inst.transform.SetParent(transform);
                inst.name = $"GrassPlacer_{gridCount}";
            }
        }
#endif
    }

    [ContextMenu("Bake All Grass DOD")]
    void BakeAllGrassDOD(){
#if UNITY_EDITOR
        for(int i = 0; i < gridWidth; i++){
            for(int l = 0; l < gridLength; l++){
                GameObject inst = Instantiate(
                        grassPlacerDOD,
                        new Vector3(transform.position.x + i * gridSize.x, 0,transform.position.z + l * gridSize.y),
                        Quaternion.identity);

                PoissonIndirectPlacer_Bake_DOD pb = inst.GetComponent<PoissonIndirectPlacer_Bake_DOD>();

                pb.newFileName = $"{transform.name}" + gridCount++;
                pb.folderName = this.folderName;
                pb.regionSize = gridSize;
                pb.scaleRange = this.scaleRange;
                pb.minDistance = this.minDistance;
                pb.roadClearanceRadius = this.roadClearanceRadius;
                pb.groundMask = wantedLayer;
                pb.obstacleMask = this.obstacleLayer;

                pb.gameObject1 = this.gameObject1;
                pb.allGameObjects = this.allGameObjects;
                // pb.meshLOD0 = this.meshLOD0;
                // pb.meshLOD1 = this.meshLOD1;
                // pb.meshLOD2 = this.meshLOD2;
                // pb.meshLOD3 = this.meshLOD3;
                pb.material = this.material;
                pb.cullingShader = this.cullingShader;

                pb.maxDistance = MaxDistance;
                pb.lod0Distance = LOD0Distance;
                pb.lod1Distance = LOD1Distance;
                pb.lod2Distance = LOD2Distance;

                pb.BakeGrass();

                inst.transform.SetParent(transform);
                inst.name = $"GrassPlacer_{gridCount}";
            }
        }
#endif
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
