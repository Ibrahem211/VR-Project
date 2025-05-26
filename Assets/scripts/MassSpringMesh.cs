using System.Collections.Generic;
using UnityEngine;

public class MassSpringMesh
{
    public readonly List<MassPoint> Points = new();
    public readonly List<(int a, int b, float restLen, float k)> Springs = new();

    public static MassSpringMesh FromMesh(Mesh mesh, Transform transform, float k = 500f, int samplingStep = 1)
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
        }

        return msm;
    }
}
