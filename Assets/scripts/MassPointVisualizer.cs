using UnityEngine;
using System.Collections.Generic;

public class MassPointVisualizer : MonoBehaviour
{
    public GameObject pointPrefab;
    private List<GameObject> visualPoints = new List<GameObject>();

    public void ShowPoints(List<Vector3> points)
    {
        foreach (Vector3 p in points)
        {
            GameObject sphere = Instantiate(pointPrefab, p, Quaternion.identity, transform);
            visualPoints.Add(sphere);
        }
    }
}
