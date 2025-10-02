using UnityEngine;

public class FogDisturbance : MonoBehaviour
{
    public ParticleSystem fogSystem;
    public float radius = 2f;
    public float force = 2f;
    public float settleStrength = 1f; // pullback inside margin
    public float damping = 0.95f;     // velocity damping
    public Vector3 boxMargin = new Vector3(0.5f, 0.5f, 0.5f);
    public float fadeDistance = 1f;   // distance past margin to fully fade out

    ParticleSystem.Particle[] particles;
    Vector3 lastPosition;

    void LateUpdate()
    {
        if (fogSystem == null) return;

        // player velocity (XZ only)
        Vector3 velocity = (transform.position - lastPosition) / Time.deltaTime;
        lastPosition = transform.position;
        float moveSpeed = new Vector2(velocity.x, velocity.z).magnitude;

        if (particles == null || particles.Length < fogSystem.main.maxParticles)
            particles = new ParticleSystem.Particle[fogSystem.main.maxParticles];

        int count = fogSystem.GetParticles(particles);

        var shape = fogSystem.shape;
        Vector3 boxSize = shape.scale;
        Vector3 boxCenter = fogSystem.transform.position + shape.position;
        Vector3 halfExtents = (boxSize * 0.5f) + boxMargin;

        for (int i = 0; i < count; i++)
        {
            Vector3 particlePos = particles[i].position;

            // --- disturbance from player ---
            if (moveSpeed > 0.01f)
            {
                Vector3 toParticle = particlePos - transform.position;
                toParticle.y = 0; // XZ only
                float dist = toParticle.magnitude;

                if (dist < radius && dist > 0.001f)
                {
                    float strength = 1f - (dist / radius);
                    particles[i].velocity += toParticle.normalized * force * strength * (moveSpeed * 0.1f);
                }
            }

            // --- containment + fade ---
            Vector3 local = particlePos - boxCenter;
            Vector3 clamped = new Vector3(
                Mathf.Clamp(local.x, -halfExtents.x, halfExtents.x),
                Mathf.Clamp(local.y, -halfExtents.y, halfExtents.y),
                Mathf.Clamp(local.z, -halfExtents.z, halfExtents.z)
            );

            Vector3 diff = clamped - local;
            float distOutside = diff.magnitude;

            // fade alpha progressively outside the margin
            float alphaFactor = 1f;
            if (distOutside > 0f)
            {
                alphaFactor = Mathf.Clamp01(1f - (distOutside / fadeDistance));
                // gently push them back inside while still visible
                particles[i].velocity += diff * settleStrength * Time.deltaTime;
            }

            // apply alpha scaling
            Color c = particles[i].startColor;
            c.a = c.a * alphaFactor;
            particles[i].startColor = c;

            // --- respawn if fully faded ---
            if (alphaFactor <= 0.001f)
            {
                // kill & respawn this particle
                particles[i].remainingLifetime = -1f;
            }

            // --- damping (always applied) ---
            particles[i].velocity *= damping;
        }

        fogSystem.SetParticles(particles, count);
    }
}
