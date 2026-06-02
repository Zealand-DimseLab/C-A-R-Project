using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PoissonIndirectPlacer_Bake : MonoBehaviour {
    private PoissonOOP poissonOOP = new();

    [Header("Resources")]
    public Mesh meshLOD0;
    public Mesh meshLOD1;
    public Mesh meshLOD2;
    public Mesh meshLOD3;
    public Material material;
    public ComputeShader cullingShader;
    public GrassDataAsset bakedData; // Træk din gemte fil herind

    [Header("LOD & Settings")]
    public float lod0Distance = 30f;
    public float lod1Distance = 30f;
    public float lod2Distance = 30f;
    public float maxDistance = 150f;
    public Vector2 regionSize = new Vector2(100, 100);
    public float minDistance = 1.5f;

    private ComputeBuffer positionBuffer;
    private ComputeBuffer visibleBufferLOD0;
    private ComputeBuffer visibleBufferLOD1;
    private ComputeBuffer visibleBufferLOD2;
    private ComputeBuffer visibleBufferLOD3;
    private ComputeBuffer argsBufferLOD0;
    private ComputeBuffer argsBufferLOD1;
    private ComputeBuffer argsBufferLOD2;
    private ComputeBuffer argsBufferLOD3;
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

    private List<GrassDataAsset> allGrass = new();
    private List<ComputeBuffer> allPositionBuffers = new();
    private int counter;
    public string newFileName;
    public string folderName;

    public bool showGizmos;

    [ContextMenu("Bage Grid")]
    public void BakeGrid(){
#if UNITY_EDITOR
        List<GrassData> matrixList = new();

        for(float i = 0; i < regionSize.x; i = i + 0.3f){
            for(float l = 0; l < regionSize.y; l = l + 0.6f){
                Vector3 rayStart = new Vector3(
                        this.transform.position.x - this.regionSize.x * 0.5f + i,
                        this.transform.position.y + 500f,
                        this.transform.position.z - this.regionSize.y * 0.5f + l
                        );

                if (Physics.Raycast(rayStart, Vector3.down, 1000f,this.obstacleMask)) 
                    continue;

                float fScale = Random.Range(this.scaleRange.x,this.scaleRange.y);
                if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 1000f)) {
                    if (Physics.CheckSphere(hit.point + Vector3.up * 0.5f, fScale, this.obstacleMask))
                        continue;

                    Vector3 scale = new Vector3(Random.Range(this.scaleRange.x,this.scaleRange.y),0.7f,Random.Range(this.scaleRange.x,this.scaleRange.y));
                    float fYaw = Random.Range(0,360);

                    GrassData gd = new() {Position = hit.point, Yaw = fYaw, Scale = fScale };
                    matrixList.Add(gd);
                }

            }
        }

        GrassDataAsset asset = ScriptableObject.CreateInstance<GrassDataAsset>();
        asset.matrices = matrixList.ToArray();

        if(newFileName.Length <= 2) this.newFileName = transform.name;

        string path = $"Assets/{folderName}/BakedGrassData_{this.newFileName}.asset";
        UnityEditor.AssetDatabase.CreateAsset(asset, path);
        UnityEditor.AssetDatabase.SaveAssets();

        this.bakedData = asset;

#if UNITY_EDITOR

        EditorUtility.SetDirty(asset);
        EditorUtility.SetDirty(this);

        if(PrefabUtility.IsPartOfPrefabAsset(this)){
            PrefabUtility.RecordPrefabInstancePropertyModifications(this);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
#endif

        Debug.Log("Bagning færdig! Gemt til: " + path + " - Antal: " + asset.matrices.Length);
#else
        Debug.Log("Baking can only be done in the Unity Editor!");
#endif
    }

    // --- BAGE FUNKTION (KØRES KUN I EDITOR) ---
    [ContextMenu("Bage Græs Data")]
    public void BakeGrass() {
#if UNITY_EDITOR
        System.Diagnostics.Stopwatch stopwatch = new();
        stopwatch.Start();
// #if UNITY_EDITOR
//         Debug.Log("Starter bagning af græs...");
//         List<Vector2> points = GeneratePoissonPoints(minDistance, regionSize, 30, 0);
//         List<GrassData> matrixList = new();
//
//         foreach (var p in points) {
//             Vector3 rayStart = new Vector3(
//                     this.transform.position.x - this.regionSize.x * 0.5f + p.x,
//                     this.transform.position.y + 500f,
//                     this.transform.position.z - this.regionSize.y * 0.5f + p.y
//                     );
//
//             if (Physics.Raycast(rayStart, Vector3.down, 1000f,this.obstacleMask)) 
//                 continue;
//
//             float fScale = Random.Range(this.scaleRange.x,this.scaleRange.y);
//             if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 1000f)) {
//                 if (Physics.CheckSphere(hit.point + Vector3.up * 0.5f, fScale, this.obstacleMask))
//                     continue;
//
//                 Vector3 scale = new Vector3(Random.Range(this.scaleRange.x,this.scaleRange.y),0.7f,Random.Range(this.scaleRange.x,this.scaleRange.y));
//                 float fYaw = Random.Range(0,360);
//
//                 GrassData gd = new() {Position = hit.point, Yaw = fYaw, Scale = fScale };
//                 matrixList.Add(gd);
//             }
//         }
//
//         GrassDataAsset asset = ScriptableObject.CreateInstance<GrassDataAsset>();
//         asset.matrices = matrixList.ToArray();
//
//         if(newFileName.Length <= 2) this.newFileName = transform.name;
//
//         string path = $"Assets/{folderName}/BakedGrassData_{this.newFileName}.asset";
//         UnityEditor.AssetDatabase.CreateAsset(asset, path);
//         UnityEditor.AssetDatabase.SaveAssets();
//
//         this.bakedData = asset;

// #if UNITY_EDITOR

        poissonOOP.folderName = folderName;
        poissonOOP.newFileName = newFileName;
        poissonOOP.scaleRange = scaleRange;
        poissonOOP.obstacleMask = obstacleMask;
        poissonOOP.regionSize = regionSize;
        poissonOOP.minDistance = minDistance;
        poissonOOP.transform = transform;

        GrassDataAsset baked = poissonOOP.BakeGrass();
        this.bakedData = baked;

        // EditorUtility.SetDirty(asset);
        EditorUtility.SetDirty(baked);
        EditorUtility.SetDirty(this);

        if(PrefabUtility.IsPartOfPrefabAsset(this)){
            PrefabUtility.RecordPrefabInstancePropertyModifications(this);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
// #endif

        // Debug.Log("Bagning færdig! Gemt til: " + path + " - Antal: " + asset.matrices.Length);
// #else
//         Debug.Log("Baking can only be done in the Unity Editor!");
// #endif
        stopwatch.Stop();
        Debug.Log(stopwatch.Elapsed);
#else
        Debug.Log("Only available in the editor");
#endif
    }

    void Start() {
        this.mainCam = Camera.main;
        if (this.bakedData == null) {
            Debug.LogError("Ingen bage-data fundet! Højreklik på komponenten og vælg 'Bage Græs Data'. " + gameObject.name);
            return;
        }

        this.instanceCount = this.bakedData.matrices.Length;

        if(this.instanceCount > 0){
            // Nu tager det 0 sekunder at starte, fordi vi bare uploader den færdige liste
            this.positionBuffer = new ComputeBuffer(instanceCount, 20);
            this.positionBuffer.SetData(this.bakedData.matrices);

            // Initialisér resten af dine buffere som før...
            this.visibleBufferLOD0 = new ComputeBuffer(instanceCount, 20, ComputeBufferType.Append);
            this.visibleBufferLOD1 = new ComputeBuffer(instanceCount, 20, ComputeBufferType.Append);
            this.visibleBufferLOD2 = new ComputeBuffer(instanceCount, 20, ComputeBufferType.Append);
            this.visibleBufferLOD3 = new ComputeBuffer(instanceCount, 20, ComputeBufferType.Append);

            this.argsBufferLOD0 = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
            this.argsBufferLOD0.SetData(new uint[] { meshLOD0.GetIndexCount(0), 0, meshLOD0.GetIndexStart(0), meshLOD0.GetBaseVertex(0), 0 });

            this.argsBufferLOD1 = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
            this.argsBufferLOD1.SetData(new uint[] { meshLOD1.GetIndexCount(0), 0, meshLOD1.GetIndexStart(0), meshLOD1.GetBaseVertex(0), 0 });

            this.argsBufferLOD2 = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
            this.argsBufferLOD2.SetData(new uint[] { meshLOD2.GetIndexCount(0), 0, meshLOD2.GetIndexStart(0), meshLOD2.GetBaseVertex(0), 0 });

            this.argsBufferLOD3 = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
            this.argsBufferLOD3.SetData(new uint[] { meshLOD3.GetIndexCount(0), 0, meshLOD3.GetIndexStart(0), meshLOD3.GetBaseVertex(0), 0 });

            this.material.EnableKeyword("UNITY_PROCEDURAL_INSTANCING_ENABLED");
            this.propBlock = new MaterialPropertyBlock();
            this.initialized = true;
        }
        else{
            Debug.Log("Instance count was not above 0: "+gameObject);
        }
    }

    // private static List<Vector2> GeneratePoissonPoints(float radius, Vector2 region, int rejectSamples, int seed) {
    //     if (radius <= 0f) radius = 0.01f;
    //
    //     System.Random prng = (seed == 0) ? new System.Random() : new System.Random(seed);
    //
    //     float cellSize = radius / Mathf.Sqrt(2f);
    //     int gridW = Mathf.CeilToInt(region.x / cellSize);
    //     int gridH = Mathf.CeilToInt(region.y / cellSize);
    //
    //     int[,] grid = new int[gridW, gridH];
    //     for (int x = 0; x < gridW; x++)
    //         for (int y = 0; y < gridH; y++)
    //             grid[x, y] = -1;
    //
    //     List<Vector2> points = new List<Vector2>();
    //     List<Vector2> spawnPoints = new List<Vector2>();
    //
    //     Vector2 first = new Vector2(
    //             (float)prng.NextDouble() * region.x,
    //             (float)prng.NextDouble() * region.y
    //             );
    //
    //     points.Add(first);
    //     spawnPoints.Add(first);
    //     grid[(int)(first.x / cellSize), (int)(first.y / cellSize)] = 0;
    //
    //     while (spawnPoints.Count > 0) {
    //         int spawnIndex = prng.Next(0, spawnPoints.Count);
    //         Vector2 centre = spawnPoints[spawnIndex];
    //         bool accepted = false;
    //
    //         for (int i = 0; i < rejectSamples; i++) {
    //             float angle = (float)prng.NextDouble() * Mathf.PI * 2f;
    //             Vector2 dir = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
    //             float dist = radius * (1f + (float)prng.NextDouble()); // radius..2*radius
    //             Vector2 candidate = centre + dir * dist;
    //
    //             if (IsValid(candidate, region, cellSize, radius, points, grid)) {
    //                 points.Add(candidate);
    //                 spawnPoints.Add(candidate);
    //                 grid[(int)(candidate.x / cellSize), (int)(candidate.y / cellSize)] = points.Count - 1;
    //                 accepted = true;
    //                 break;
    //             }
    //         }
    //
    //         if (!accepted)
    //             spawnPoints.RemoveAt(spawnIndex);
    //     }
    //
    //     return points;
    // }
    //
    // private static bool IsValid(Vector2 c, Vector2 region, float cellSize, float radius, List<Vector2> points, int[,] grid) {
    //     if (c.x < 0 || c.y < 0 || c.x >= region.x || c.y >= region.y)
    //         return false;
    //
    //     int cellX = (int)(c.x / cellSize);
    //     int cellY = (int)(c.y / cellSize);
    //
    //     int startX = Mathf.Max(0, cellX - 2);
    //     int endX = Mathf.Min(grid.GetLength(0) - 1, cellX + 2);
    //     int startY = Mathf.Max(0, cellY - 2);
    //     int endY = Mathf.Min(grid.GetLength(1) - 1, cellY + 2);
    //
    //     float r2 = radius * radius;
    //
    //     for (int x = startX; x <= endX; x++) {
    //         for (int y = startY; y <= endY; y++) {
    //             int idx = grid[x, y];
    //             if (idx != -1) {
    //                 Vector2 p = points[idx];
    //                 if ((c - p).sqrMagnitude < r2)
    //                     return false;
    //             }
    //         }
    //     }
    //
    //     return true;
    // }

    // Update() forbliver præcis som den var før...
    void Update() {
        if (!this.initialized || this.mainCam == null) return;
        if (this.propBlock == null) return;

        float distSq = (this.mainCam.transform.position - this.transform.position).sqrMagnitude;
        if(distSq > (this.maxDistance + this.regionSize.x) * (this.maxDistance + this.regionSize.x)) return;

        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(this.mainCam);
        Vector4[] frustumPlanes = new Vector4[6];
        for(int i = 0; i < 6; i++){
            frustumPlanes[i] = new Vector4(planes[i].normal.x, planes[i].normal.y, planes[i].normal.z, planes[i].distance);
        }

        // Nulstil tællere
        this.visibleBufferLOD0.SetCounterValue(0);
        this.visibleBufferLOD1.SetCounterValue(0);
        this.visibleBufferLOD2.SetCounterValue(0);
        this.visibleBufferLOD3.SetCounterValue(0);

        // 1. Dispatch Compute Shader
        int kernel = cullingShader.FindKernel("CSMain");
        this.cullingShader.SetBuffer(kernel, "_InputBuffer", positionBuffer);
        this.cullingShader.SetBuffer(kernel, "_OutputBufferLOD0", visibleBufferLOD0);
        this.cullingShader.SetBuffer(kernel, "_OutputBufferLOD1", visibleBufferLOD1);
        this.cullingShader.SetBuffer(kernel, "_OutputBufferLOD2", visibleBufferLOD2);
        this.cullingShader.SetBuffer(kernel, "_OutputBufferLOD3", visibleBufferLOD3);

        this.cullingShader.SetVectorArray("_FrustumPlanes", frustumPlanes);

        this.cullingShader.SetVector("_CameraPosition", mainCam.transform.position);
        this.cullingShader.SetFloat("_LOD0Distance", lod0Distance);
        this.cullingShader.SetFloat("_LOD1Distance", lod1Distance);
        this.cullingShader.SetFloat("_LOD2Distance", lod2Distance);
        this.cullingShader.SetFloat("_MaxDistance", maxDistance);
        this.cullingShader.SetInt("_InstanceCount", instanceCount);

        int groups = Mathf.CeilToInt(instanceCount / 64f);
        this.cullingShader.Dispatch(kernel, groups, 1, 1);

        // 2. CopyCount
        ComputeBuffer.CopyCount(visibleBufferLOD0, argsBufferLOD0, 4);
        ComputeBuffer.CopyCount(visibleBufferLOD1, argsBufferLOD1, 4);
        ComputeBuffer.CopyCount(visibleBufferLOD2, argsBufferLOD2, 4);
        ComputeBuffer.CopyCount(visibleBufferLOD3, argsBufferLOD3, 4);

        Bounds drawBounds = new Bounds(transform.position, new Vector3(regionSize.x * 2f, 2000f, regionSize.y * 2f));

        // 3. Tegn LOD 0 (Tæt på)
        this.propBlock.SetBuffer("_InstanceBuffer", visibleBufferLOD0);
        Graphics.DrawMeshInstancedIndirect(meshLOD0, 0, material, drawBounds, argsBufferLOD0, 0, propBlock);

        // 4. Tegn LOD 1 (Langt væk)
        this.propBlock.SetBuffer("_InstanceBuffer", visibleBufferLOD1);
        Graphics.DrawMeshInstancedIndirect(meshLOD1, 0, material, drawBounds, argsBufferLOD1, 0, propBlock);

        this.propBlock.SetBuffer("_InstanceBuffer", visibleBufferLOD2);
        Graphics.DrawMeshInstancedIndirect(meshLOD2, 0, material, drawBounds, argsBufferLOD2, 0, propBlock);

        this.propBlock.SetBuffer("_InstanceBuffer", visibleBufferLOD3);
        Graphics.DrawMeshInstancedIndirect(meshLOD3, 0, material, drawBounds, argsBufferLOD3, 0, propBlock);
    }

    void OnDisable() {
        this.positionBuffer?.Release();
        this.visibleBufferLOD0?.Release();
        this.visibleBufferLOD1?.Release();
        this.visibleBufferLOD2?.Release();
        this.visibleBufferLOD3?.Release();
        this.argsBufferLOD0?.Release();
        this.argsBufferLOD1?.Release();
        this.argsBufferLOD2?.Release();
        this.argsBufferLOD3?.Release();
    }

    private void OnDrawGizmos() {
        if(!showGizmos) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(regionSize.x, 0.1f, regionSize.y));

        // if(batchList == null) return;
        // Gizmos.color = Color.red;
        // foreach(var batch in batchList){
        //     foreach(var matrix in batch){
        //         Gizmos.DrawLine(matrix.GetColumn(3),matrix.GetColumn(3) + Vector4.one * 2f);
        //     }
        // }
    }
}
