using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class MeshTester : MonoBehaviour
{
    public GameObject pointPrefab; 
    public float pointScale = 0.05f;

    void Start()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;

        MassSpringMesh springMesh = MassSpringMesh.FromMesh(mesh, transform);

        GetComponent<MeshRenderer>().enabled = false;

        Debug.Log($"Points: {springMesh.Points.Count}, Springs: {springMesh.Springs.Count}");

        for (int i = 0; i < Mathf.Min(5, springMesh.Points.Count); i++)
        {
            Debug.Log($"Points[{i}]: {springMesh.Points[i].Position}");
        }

        foreach (var point in springMesh.Points)
        {
            GameObject visual = Instantiate(pointPrefab, point.Position, Quaternion.identity);
            visual.transform.localScale = Vector3.one * pointScale;
            visual.transform.SetParent(this.transform);
        }
    }
}
