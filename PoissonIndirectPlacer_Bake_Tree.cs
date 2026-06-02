using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PoissonIndirectPlacer_Bake_Tree : MonoBehaviour {
    [Header("Resources")]
    public Mesh meshLOD0;
    public Mesh meshLOD1;
    public Mesh meshLOD2;
    public Material trunkMaterial;
    public Material leavesMaterial;
    public ComputeShader cullingShader;
    public TreeDataAsset bakedData; // Træk din gemte fil herind

    [Header("LOD & Settings")]
    public float lod0Distance = 30f;
    public float lod1Distance = 60f;
    public float maxDistance = 150f;
    public Vector2 regionSize = new Vector2(100, 100);
    public float minDistance = 1.5f;

    private ComputeBuffer positionBuffer;

    private ComputeBuffer visibleBufferLOD0;
    private ComputeBuffer visibleBufferLOD1;
    private ComputeBuffer visibleBufferLOD2;

    private ComputeBuffer argsBufferTrunkLOD0;
    private ComputeBuffer argsBufferTrunkLOD1;
    private ComputeBuffer argsBufferTrunkLOD2;

    private ComputeBuffer argsBufferLeavesLOD0;
    private ComputeBuffer argsBufferLeavesLOD1;
    private ComputeBuffer argsBufferLeavesLOD2;

    private MaterialPropertyBlock propBlock;

    private bool initialized = false;
    private int instanceCount = 0;

    [Header("Layer Masks")]
    public LayerMask groundMask;
    public LayerMask obstacleMask;
    public float roadClearanceRadius = 1.0f;

    [Header("Variationer")]
    public Vector2 scaleRange;

    private Camera mainCam;

    private List<TreeDataAsset> allGrass = new();
    private List<ComputeBuffer> allPositionBuffers = new();
    private int counter;
    public string newFileName;
    public string folderName;

    // --- BAGE FUNKTION (KØRES KUN I EDITOR) ---
    [ContextMenu("Bage Træ Data")]
    public void BakeTree() {
#if UNITY_EDITOR
        Debug.Log("Starter bagning af træ...");
        List<Vector2> points = GeneratePoissonPoints(minDistance, regionSize, 30, 0);
        List<TreeData> matrixList = new();

        foreach (var p in points) {
            Vector3 rayStart = new Vector3(
                    transform.position.x - regionSize.x * 0.5f + p.x,
                    transform.position.y + 500f,
                    transform.position.z - regionSize.y * 0.5f + p.y
                    );

            if (Physics.Raycast(rayStart, Vector3.down, 1000f,obstacleMask)) 
                continue;
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 1000f)) {
                if (Physics.CheckSphere(hit.point + Vector3.up * 0.5f, roadClearanceRadius, obstacleMask))
                    continue;

                TreeData gd = new() {Position = hit.point, Yaw = Random.Range(0,360), Scale = Random.Range(scaleRange.x,scaleRange.y) };
                matrixList.Add(gd);
            }
        }

        TreeDataAsset asset = ScriptableObject.CreateInstance<TreeDataAsset>();
        asset.matrices = matrixList.ToArray();

        if(newFileName.Length <= 2) newFileName = $"{transform.name}";
        string path = $"Assets/{this.folderName}/BakedTreeData_{this.newFileName}.asset";
        UnityEditor.AssetDatabase.CreateAsset(asset, path);
        UnityEditor.AssetDatabase.SaveAssets();

        this.bakedData = asset;
        Debug.Log("Bagning færdig! Gemt til: " + path + " - Antal: " + asset.matrices.Length);
        EditorUtility.SetDirty(asset);
        EditorUtility.SetDirty(this);
        if(PrefabUtility.IsPartOfPrefabAsset(this)){
            PrefabUtility.RecordPrefabInstancePropertyModifications(this);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
#else
        Debug.Log("Baking can only be done in the Unity Editor!");
#endif
    }

    [ContextMenu("Create colliders")]
    public void CreateColliders(){
        for(int i = 0; i < this.bakedData.matrices.Length - 1; i++){
            GameObject go = new();
            go.transform.position = this.bakedData.matrices[i].Position;
            go.transform.SetParent(this.transform);
            // GameObject inst = Instantiate(go, this.bakedData.matrices[i].Position, Quaternion.identity);
            // inst.transform.SetParent(this.transform);

            CapsuleCollider bc = go.AddComponent<CapsuleCollider>();
            bc.center = new Vector3(0f,3f,0f);
            bc.radius = 0.7f;
            bc.height = 6f;
        }
        Debug.Log($"Created collisions for {this.bakedData.matrices.Length} objects");
    }

    void Start() {
        mainCam = Camera.main;
        if(bakedData.matrices.Length <= 0) return;
        if (bakedData == null){
            Debug.LogError("Ingen bage-data fundet! Højreklik på komponenten og vælg 'Bage Græs Data'.");
            return;
        }

        instanceCount = bakedData.matrices.Length;

        // Nu tager det 0 sekunder at starte, fordi vi bare uploader den færdige liste
        positionBuffer = new ComputeBuffer(instanceCount, 20);
        positionBuffer.SetData(bakedData.matrices);

        // Initialisér resten af dine buffere som før...
        visibleBufferLOD0 = new ComputeBuffer(instanceCount, 20, ComputeBufferType.Append);
        visibleBufferLOD1 = new ComputeBuffer(instanceCount, 20, ComputeBufferType.Append);
        visibleBufferLOD2 = new ComputeBuffer(instanceCount, 20, ComputeBufferType.Append);

        argsBufferTrunkLOD0 = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBufferTrunkLOD0.SetData(new uint[] { meshLOD0.GetIndexCount(0), 0, meshLOD0.GetIndexStart(0), meshLOD0.GetBaseVertex(0), 0 });

        argsBufferLeavesLOD0 = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBufferLeavesLOD0.SetData(new uint[] {
                meshLOD0.GetIndexCount(1),
                0,
                meshLOD0.GetIndexStart(1),
                meshLOD0.GetBaseVertex(1),
                0
                });

        argsBufferTrunkLOD1 = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBufferTrunkLOD1.SetData(new uint[] {
                meshLOD1.GetIndexCount(0),
                0,
                meshLOD1.GetIndexStart(0),
                meshLOD1.GetBaseVertex(0),
                0
                });

        argsBufferLeavesLOD1 = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBufferLeavesLOD1.SetData(new uint[] {
                meshLOD1.GetIndexCount(1),
                0,
                meshLOD1.GetIndexStart(1),
                meshLOD1.GetBaseVertex(1),
                0
                });

        argsBufferTrunkLOD2 = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBufferTrunkLOD2.SetData(new uint[] {
                meshLOD2.GetIndexCount(0),
                0,
                meshLOD2.GetIndexStart(0),
                meshLOD2.GetBaseVertex(0),
                0
                });

        argsBufferLeavesLOD2 = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBufferLeavesLOD2.SetData(new uint[] {
                meshLOD2.GetIndexCount(1),
                0,
                meshLOD2.GetIndexStart(1),
                meshLOD2.GetBaseVertex(1),
                0
                });

        trunkMaterial.EnableKeyword("UNITY_PROCEDURAL_INSTANCING_ENABLED");
        leavesMaterial.EnableKeyword("UNITY_PROCEDURAL_INSTANCING_ENABLED");

        propBlock = new MaterialPropertyBlock();
        initialized = true;
    }

    private static List<Vector2> GeneratePoissonPoints(float radius, Vector2 region, int rejectSamples, int seed) {
        if (radius <= 0f) radius = 0.01f;

        System.Random prng = (seed == 0) ? new System.Random() : new System.Random(seed);

        float cellSize = radius / Mathf.Sqrt(2f);
        int gridW = Mathf.CeilToInt(region.x / cellSize);
        int gridH = Mathf.CeilToInt(region.y / cellSize);

        int[,] grid = new int[gridW, gridH];
        for (int x = 0; x < gridW; x++)
            for (int y = 0; y < gridH; y++)
                grid[x, y] = -1;

        List<Vector2> points = new List<Vector2>();
        List<Vector2> spawnPoints = new List<Vector2>();

        Vector2 first = new Vector2(
                (float)prng.NextDouble() * region.x,
                (float)prng.NextDouble() * region.y
                );

        points.Add(first);
        spawnPoints.Add(first);
        grid[(int)(first.x / cellSize), (int)(first.y / cellSize)] = 0;

        while (spawnPoints.Count > 0) {
            int spawnIndex = prng.Next(0, spawnPoints.Count);
            Vector2 centre = spawnPoints[spawnIndex];
            bool accepted = false;

            for (int i = 0; i < rejectSamples; i++) {
                float angle = (float)prng.NextDouble() * Mathf.PI * 2f;
                Vector2 dir = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
                float dist = radius * (1f + (float)prng.NextDouble()); // radius..2*radius
                Vector2 candidate = centre + dir * dist;

                if (IsValid(candidate, region, cellSize, radius, points, grid)) {
                    points.Add(candidate);
                    spawnPoints.Add(candidate);
                    grid[(int)(candidate.x / cellSize), (int)(candidate.y / cellSize)] = points.Count - 1;
                    accepted = true;
                    break;
                }
            }

            if (!accepted)
                spawnPoints.RemoveAt(spawnIndex);
        }

        return points;
    }

    private static bool IsValid(Vector2 c, Vector2 region, float cellSize, float radius, List<Vector2> points, int[,] grid) {
        if (c.x < 0 || c.y < 0 || c.x >= region.x || c.y >= region.y)
            return false;

        int cellX = (int)(c.x / cellSize);
        int cellY = (int)(c.y / cellSize);

        int startX = Mathf.Max(0, cellX - 2);
        int endX = Mathf.Min(grid.GetLength(0) - 1, cellX + 2);
        int startY = Mathf.Max(0, cellY - 2);
        int endY = Mathf.Min(grid.GetLength(1) - 1, cellY + 2);

        float r2 = radius * radius;

        for (int x = startX; x <= endX; x++) {
            for (int y = startY; y <= endY; y++) {
                int idx = grid[x, y];
                if (idx != -1) {
                    Vector2 p = points[idx];
                    if ((c - p).sqrMagnitude < r2)
                        return false;
                }
            }
        }

        return true;
    }

    // Update() forbliver præcis som den var før...
    void Update() {
        if (!initialized || mainCam == null) return;
        if (propBlock == null) propBlock = new MaterialPropertyBlock();

        float distSq = (mainCam.transform.position - transform.position).sqrMagnitude;
        if(distSq > (maxDistance + regionSize.x) * (maxDistance + regionSize.x)) return;

        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(mainCam);
        Vector4[] frustumPlanes = new Vector4[6];
        for(int i = 0; i < 6; i++){
            frustumPlanes[i] = new Vector4(planes[i].normal.x, planes[i].normal.y, planes[i].normal.z, planes[i].distance);
        }

        // Nulstil tællere
        visibleBufferLOD0.SetCounterValue(0);
        visibleBufferLOD1.SetCounterValue(0);
        visibleBufferLOD2.SetCounterValue(0);

        // 1. Dispatch Compute Shader
        int kernel = cullingShader.FindKernel("CSMain");
        cullingShader.SetBuffer(kernel, "_InputBuffer", positionBuffer);
        cullingShader.SetBuffer(kernel, "_OutputBufferLOD0", visibleBufferLOD0);
        cullingShader.SetBuffer(kernel, "_OutputBufferLOD1", visibleBufferLOD1);
        cullingShader.SetBuffer(kernel, "_OutputBufferLOD2", visibleBufferLOD2);

        cullingShader.SetVectorArray("_FrustumPlanes", frustumPlanes);

        cullingShader.SetVector("_CameraPosition", mainCam.transform.position);
        cullingShader.SetFloat("_LOD0Distance", lod0Distance);
        cullingShader.SetFloat("_LOD1Distance", lod1Distance);
        cullingShader.SetFloat("_MaxDistance", maxDistance);
        cullingShader.SetInt("_InstanceCount", instanceCount);

        int groups = Mathf.CeilToInt(instanceCount / 64f);
        cullingShader.Dispatch(kernel, groups, 1, 1);

        // 2. CopyCount
        ComputeBuffer.CopyCount(visibleBufferLOD0, argsBufferTrunkLOD0, 4);
        ComputeBuffer.CopyCount(visibleBufferLOD0, argsBufferLeavesLOD0, 4);

        ComputeBuffer.CopyCount(visibleBufferLOD1, argsBufferTrunkLOD1, 4);
        ComputeBuffer.CopyCount(visibleBufferLOD1, argsBufferLeavesLOD1, 4);

        ComputeBuffer.CopyCount(visibleBufferLOD2, argsBufferTrunkLOD2, 4);
        ComputeBuffer.CopyCount(visibleBufferLOD2, argsBufferLeavesLOD2, 4);

        Bounds drawBounds = new Bounds(transform.position, new Vector3(regionSize.x * 2f, 2000f, regionSize.y * 2f));

        // 3. Tegn LOD 0 (Tæt på)
        propBlock.SetBuffer("_InstanceBuffer", visibleBufferLOD0);
        Graphics.DrawMeshInstancedIndirect(meshLOD0, 0, trunkMaterial, drawBounds, argsBufferTrunkLOD0, 0, propBlock);
        Graphics.DrawMeshInstancedIndirect(meshLOD0, 1, leavesMaterial, drawBounds, argsBufferLeavesLOD0, 0, propBlock);

        // 4. Tegn LOD 1 (Langt væk)
        propBlock.SetBuffer("_InstanceBuffer", visibleBufferLOD1);
        Graphics.DrawMeshInstancedIndirect(meshLOD1, 0, trunkMaterial, drawBounds, argsBufferTrunkLOD1, 0, propBlock);
        Graphics.DrawMeshInstancedIndirect(meshLOD1, 1, leavesMaterial, drawBounds, argsBufferLeavesLOD1, 0, propBlock);

        propBlock.SetBuffer("_InstanceBuffer", visibleBufferLOD2);
        Graphics.DrawMeshInstancedIndirect(meshLOD2, 0, trunkMaterial, drawBounds, argsBufferTrunkLOD2, 0, propBlock);
        Graphics.DrawMeshInstancedIndirect(meshLOD2, 1, leavesMaterial, drawBounds, argsBufferLeavesLOD2, 0, propBlock);
    }

    void OnDisable() {
        positionBuffer?.Release();
        visibleBufferLOD0?.Release();
        visibleBufferLOD1?.Release();
        visibleBufferLOD2?.Release();
        argsBufferTrunkLOD0?.Release();
        argsBufferTrunkLOD1?.Release();
        argsBufferTrunkLOD2?.Release();
        argsBufferLeavesLOD0?.Release();
        argsBufferLeavesLOD1?.Release();
        argsBufferLeavesLOD2?.Release();
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(regionSize.x, 0.1f, regionSize.y));
    }
}
