using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PoissonOOP{
    public float minDistance;
    public Vector2 regionSize;
    public Transform transform;
    public LayerMask obstacleMask;
    public Vector2 scaleRange;
    public string newFileName;
    public string folderName;

    public GrassDataAsset BakeGrass(){
#if UNITY_EDITOR
        Debug.Log("Starter bagning af græs...");
        List<Vector2> points = GeneratePoissonPoints(minDistance, regionSize, 30, 0);
        List<GrassData> matrixList = new();

        foreach (var p in points) {
            Vector3 rayStart = new Vector3(
                    this.transform.position.x - this.regionSize.x * 0.5f + p.x,
                    this.transform.position.y + 500f,
                    this.transform.position.z - this.regionSize.y * 0.5f + p.y
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

        GrassDataAsset asset = ScriptableObject.CreateInstance<GrassDataAsset>();
        asset.matrices = matrixList.ToArray();

        if(newFileName.Length <= 2) this.newFileName = transform.name;

        string path = $"Assets/{folderName}/BakedGrassData_{this.newFileName}.asset";
        UnityEditor.AssetDatabase.CreateAsset(asset, path);
        UnityEditor.AssetDatabase.SaveAssets();

        return asset;
#else
        return null;
#endif
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
}
