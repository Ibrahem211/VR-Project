using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MassSpringMesh
{
    public readonly List<MassPoint> Points = new();
    public readonly List<(int a, int b, float restLen, float k)> Springs = new();
    public readonly List<bool> IsInternal = new();
    public OctreeNode RootOctree; // جذر Octree لتخزين النقاط الداخلية

    public static MassSpringMesh FromMesh(Mesh mesh, Transform transform, float k = 500f, int samplingStep = 1, float internalPointSpacing = 0.1f, bool fillInterior = false)
    {
        var msm = new MassSpringMesh();
        Vector3[] verts = mesh.vertices;
        int[] tris = mesh.triangles;

        var sampledIndices = new Dictionary<int, int>();

        for (int i = 0; i < verts.Length; i += samplingStep)
        {
            Vector3 pos = transform.TransformPoint(verts[i]);
            sampledIndices[i] = msm.Points.Count;
            msm.Points.Add(new MassPoint(pos));
            msm.IsInternal.Add(false);
        }

        var set = new HashSet<(int, int)>();
        void AddSpring(int i, int j)
        {
            if (!sampledIndices.ContainsKey(i) || !sampledIndices.ContainsKey(j)) return;

            int newI = sampledIndices[i];
            int newJ = sampledIndices[j];
            if (newI > newJ) (newI, newJ) = (newJ, newI);

            if (set.Add((newI, newJ)))
            {
                float restLen = Vector3.Distance(msm.Points[newI].Position, msm.Points[newJ].Position);
                msm.Springs.Add((newI, newJ, restLen, k));
            }
        }

        for (int t = 0; t < tris.Length; t += 3)
        {
            AddSpring(tris[t], tris[t + 1]);
            AddSpring(tris[t + 1], tris[t + 2]);
            AddSpring(tris[t + 2], tris[t]);
            AddSpring(tris[t], tris[t + 2]);
            AddSpring(tris[t], tris[t + 1]);
        }

        msm.AddAngularSprings(k * 0.5f);
        msm.AddTorsionalSprings(mesh, transform, k * 0.5f, sampledIndices);

        if (fillInterior)
        {
            msm.GenerateInternalPointsWithOctree(mesh, transform, internalPointSpacing, maxDepth: 5);
        }

        return msm;
    }

    public void GenerateInternalPointsWithOctree(Mesh mesh, Transform transform, float spacing, int maxDepth = 5)
    {
        Bounds bounds = new Bounds(transform.TransformPoint(mesh.vertices[0]), Vector3.zero);
        foreach (var v in mesh.vertices)
            bounds.Encapsulate(transform.TransformPoint(v));

        OctreeNode root = new OctreeNode(bounds, 0);
        root.MaxDepth = maxDepth;

        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        int nx = Mathf.CeilToInt((max.x - min.x) / spacing);
        int ny = Mathf.CeilToInt((max.y - min.y) / spacing);
        int nz = Mathf.CeilToInt((max.z - min.z) / spacing);

        for (int x = 0; x <= nx; x++)
        {
            for (int y = 0; y <= ny; y++)
            {
                for (int z = 0; z <= nz; z++)
                {
                    Vector3 pos = min + new Vector3(x * spacing, y * spacing, z * spacing);

                    if (IsPointInsideMeshMultiRay(pos, mesh, transform))
                    {
                        int index = Points.Count;
                        Points.Add(new MassPoint(pos));
                        IsInternal.Add(true);
                        root.Insert(pos, index);
                    }
                }
            }
        }

        RootOctree = root; // خزّن جذر الـ Octree
    }

    // دالة البحث عن نقاط قريبة (ستستخدمها Octree)
    public List<int> FindNearbyPoints(Vector3 pos, float radius)
    {
        List<int> results = new();
        if (RootOctree != null)
        {
            RootOctree.FindNearbyPoints(pos, radius, results, Points.Select(p => p.Position).ToList());
        }
        return results;
    }

    public void ConnectInternalPoints(float k, float maxDistance)
    {
        if (RootOctree == null) return;

        var connected = new HashSet<(int, int)>();
        int maxNeighbors = 6;

        for (int i = 0; i < Points.Count; i++)
        {
            if (!IsInternal[i]) continue;

            List<int> nearby = FindNearbyPoints(Points[i].Position, maxDistance);
            int neighborCount = 0;

            foreach (var j in nearby)
            {
                if (i >= j) continue; // لتجنب التكرار

                float dist = Vector3.Distance(Points[i].Position, Points[j].Position);
                if (dist <= maxDistance)
                {
                    if (connected.Add((i, j)))
                    {
                        Springs.Add((i, j, dist, k));
                        neighborCount++;
                        if (neighborCount >= maxNeighbors)
                            break;
                    }
                }
            }
        }
    }

    static bool IsPointInsideMeshMultiRay(Vector3 point, Mesh mesh, Transform transform)
    {
        Vector3[] directions = new Vector3[]
        {
            Vector3.up, Vector3.down, Vector3.left,
            Vector3.right, Vector3.forward, Vector3.back
        };

        int insideCount = 0;

        foreach (var dir in directions)
        {
            if (RayIntersectsMesh(point, dir, mesh, transform))
                insideCount++;
        }

        return insideCount > directions.Length / 2;
    }

    static bool RayIntersectsMesh(Vector3 origin, Vector3 direction, Mesh mesh, Transform transform)
    {
        int hitCount = 0;
        Vector3[] verts = mesh.vertices;
        int[] tris = mesh.triangles;

        Vector3 localOrigin = transform.InverseTransformPoint(origin);
        Vector3 localDir = transform.InverseTransformDirection(direction);

        for (int i = 0; i < tris.Length; i += 3)
        {
            Vector3 v0 = verts[tris[i]];
            Vector3 v1 = verts[tris[i + 1]];
            Vector3 v2 = verts[tris[i + 2]];

            if (RayIntersectsTriangle(localOrigin, localDir, v0, v1, v2))
                hitCount++;
        }

        return (hitCount % 2) == 1;
    }

    static bool RayIntersectsTriangle(Vector3 rayOrigin, Vector3 rayVector, Vector3 vertex0, Vector3 vertex1, Vector3 vertex2)
    {
        const float EPSILON = 0.0000001f;
        Vector3 edge1, edge2, h, s, q;
        float a, f, u, v;
        edge1 = vertex1 - vertex0;
        edge2 = vertex2 - vertex0;
        h = Vector3.Cross(rayVector, edge2);
        a = Vector3.Dot(edge1, h);
        if (a > -EPSILON && a < EPSILON)
            return false;
        f = 1.0f / a;
        s = rayOrigin - vertex0;
        u = f * Vector3.Dot(s, h);
        if (u < 0.0 || u > 1.0)
            return false;
        q = Vector3.Cross(s, edge1);
        v = f * Vector3.Dot(rayVector, q);
        if (v < 0.0 || u + v > 1.0)
            return false;
        float t = f * Vector3.Dot(edge2, q);
        return t > EPSILON;
    }

    public void AddAngularSprings(float k)
    {
        var connectedTo = new Dictionary<int, List<int>>();

        foreach (var (a, b, _, _) in Springs)
        {
            if (!connectedTo.ContainsKey(a)) connectedTo[a] = new List<int>();
            if (!connectedTo.ContainsKey(b)) connectedTo[b] = new List<int>();

            connectedTo[a].Add(b);
            connectedTo[b].Add(a);
        }

        var added = new HashSet<(int, int)>();

        foreach (var kvp in connectedTo)
        {
            int center = kvp.Key;
            var neighbors = kvp.Value;

            for (int i = 0; i < neighbors.Count; i++)
            {
                for (int j = i + 1; j < neighbors.Count; j++)
                {
                    int a = neighbors[i];
                    int b = neighbors[j];

                    if (a > b) (a, b) = (b, a);

                    if (added.Contains((a, b))) continue;
                    added.Add((a, b));

                    float restLen = Vector3.Distance(Points[a].Position, Points[b].Position);
                    Springs.Add((a, b, restLen, k));
                }
            }
        }
    }

    public void AddTorsionalSprings(Mesh mesh, Transform transform, float k, Dictionary<int, int> sampledIndices)
    {
        var edgeToTriangles = new Dictionary<(int, int), List<int>>();

        int[] tris = mesh.triangles;

        for (int i = 0; i < tris.Length; i += 3)
        {
            int a = tris[i];
            int b = tris[i + 1];
            int c = tris[i + 2];

            AddEdge(a, b, i);
            AddEdge(b, c, i);
            AddEdge(c, a, i);
        }

        void AddEdge(int i, int j, int triangleIndex)
        {
            if (i > j) (i, j) = (j, i);
            var edge = (i, j);
            if (!edgeToTriangles.ContainsKey(edge))
                edgeToTriangles[edge] = new List<int>();
            edgeToTriangles[edge].Add(triangleIndex);
        }

        var added = new HashSet<(int, int)>();

        foreach (var kvp in edgeToTriangles)
        {
            var trianglesList = kvp.Value;
            if (trianglesList.Count < 2) continue;

            int triA = trianglesList[0];
            int triB = trianglesList[1];

            int a0 = tris[triA];
            int a1 = tris[triA + 1];
            int a2 = tris[triA + 2];
            int b0 = tris[triB];
            int b1 = tris[triB + 1];
            int b2 = tris[triB + 2];

            int sharedA = kvp.Key.Item1;
            int sharedB = kvp.Key.Item2;

            int oppA = new[] { a0, a1, a2 }.First(x => x != sharedA && x != sharedB);
            int oppB = new[] { b0, b1, b2 }.First(x => x != sharedA && x != sharedB);

            if (!sampledIndices.ContainsKey(oppA) || !sampledIndices.ContainsKey(oppB)) continue;

            int idxA = sampledIndices[oppA];
            int idxB = sampledIndices[oppB];

            if (idxA > idxB) (idxA, idxB) = (idxB, idxA);

            if (!added.Contains((idxA, idxB)))
            {
                float restLen = Vector3.Distance(Points[idxA].Position, Points[idxB].Position);
                Springs.Add((idxA, idxB, restLen, k));
                added.Add((idxA, idxB));
            }
        }
    }
}