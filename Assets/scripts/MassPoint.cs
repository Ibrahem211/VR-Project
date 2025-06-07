using UnityEngine;

public class MassPoint
{
    public Vector3 Position;
    public Vector3 Velocity;
    public readonly float Mass;

    private Vector3 accumulatedForces = Vector3.zero;

    public MassPoint(Vector3 startPos, float mass = 1f)
    {
        Position = startPos;
        Mass = mass;
        Velocity = Vector3.zero;
    }

    public void AddForce(Vector3 f)
    {
        accumulatedForces += f;
    }

    public void Integrate(float dt)
    {
        if (Mass <= 0f) return;

        Vector3 acceleration = accumulatedForces / Mass;
        Velocity += acceleration * dt;
        Position += Velocity * dt;

        accumulatedForces = Vector3.zero;
    }
}
