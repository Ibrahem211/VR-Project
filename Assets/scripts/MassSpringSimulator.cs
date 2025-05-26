using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class MassSpringSimulator : MonoBehaviour
{
    [Header("Visual")]
    public GameObject pointPrefab;
    public float pointScale = 0.05f;

    [Header("Physics")]
    public float gravity = -9.81f;
    public float globalDamping = 0.98f;
    public float springKs = 500f;

    [Header("Optimization")]
    [Tooltip("خذ كل nth نقطة من الميش لتقليل الحسابات")]
    public int samplingStep = 2;

    MassSpringMesh msm;
    Transform[] visualPoints;

    float visualUpdateTimer = 0f;
    public float visualUpdateInterval = 0.05f; 

    void Start()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;

        msm = MassSpringMesh.FromMesh(mesh, transform, springKs, samplingStep);

        GetComponent<MeshRenderer>().enabled = false;

        int count = msm.Points.Count;
        visualPoints = new Transform[count];

        for (int i = 0; i < count; i++)
        {
            var go = Instantiate(pointPrefab, msm.Points[i].Position, Quaternion.identity, transform);
            go.transform.localScale = Vector3.one * pointScale;
            visualPoints[i] = go.transform;
        }
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        int pointCount = msm.Points.Count;
        int springCount = msm.Springs.Count;

        for (int i = 0; i < pointCount; i++)
        {
            msm.Points[i].AddForce(new Vector3(0, gravity * msm.Points[i].Mass, 0));
        }

     
        for (int i = 0; i < springCount; i++)
        {
            var spring = msm.Springs[i];
            MassPoint pa = msm.Points[spring.a];
            MassPoint pb = msm.Points[spring.b];

            Vector3 dir = pb.Position - pa.Position;
            float distSqr = dir.sqrMagnitude;
            if (distSqr == 0f) continue;

            float dist = Mathf.Sqrt(distSqr);
            Vector3 force = spring.k * (dist - spring.restLen) * (dir / dist);

            pa.AddForce(force);
            pb.AddForce(-force);
        }

        
        for (int i = 0; i < pointCount; i++)
        {
            MassPoint p = msm.Points[i];
            p.Integrate(dt);
            p.Velocity *= globalDamping;
        }
    }

    void Update()
    {
        visualUpdateTimer += Time.deltaTime;
        if (visualUpdateTimer >= visualUpdateInterval)
        {
            visualUpdateTimer = 0f;
            UpdateVisualPoints();
        }
    }

    void UpdateVisualPoints()
    {
        for (int i = 0; i < visualPoints.Length; i++)
        {
            visualPoints[i].position = msm.Points[i].Position;
        }
    }
}
