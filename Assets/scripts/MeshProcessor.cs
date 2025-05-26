using UnityEngine;
using System.Collections.Generic;

public class MeshProcessor
{
    public static List<Vector3> ExtractVertices(Mesh mesh)
    {
        return new List<Vector3>(mesh.vertices);
    }

    public static List<(int, int)> ExtractSpringsFromTriangles(int[] triangles)
    {
        var springs = new HashSet<(int, int)>();

        for (int i = 0; i < triangles.Length; i += 3)
        {
            int a = triangles[i];
            int b = triangles[i + 1];
            int c = triangles[i + 2];

            AddSpring(springs, a, b);
            AddSpring(springs, b, c);
            AddSpring(springs, c, a);
        }

        return new List<(int, int)>(springs);
    }

    private static void AddSpring(HashSet<(int, int)> springs, int i, int j)
    {
        if (i > j) (i, j) = (j, i);
        springs.Add((i, j));
    }
}
