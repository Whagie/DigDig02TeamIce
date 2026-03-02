using System.Collections.Generic;
using UnityEngine;

public class FogDisturbance : MonoBehaviour
{
    [Header("Disturbance")]
    public float radius = 2f;
    public float force = 2f;
    public float settleStrength = 1f;
    public float damping = 0.95f;

    [Header("Containment")]
    public Vector3 boxMargin = new Vector3(0.5f, 0.5f, 0.5f);
    public float fadeDistance = 1f;

    // Internal container per fog system
    class FogData
    {
        public ParticleSystem system;
        public ParticleSystem.Particle[] particles;
    }

    List<FogData> fogSystems = new List<FogData>();
    Vector3 lastPosition;

    void Start()
    {
        // Find all ParticleSystems with tag "Fog"
        GameObject[] fogObjects = GameObject.FindGameObjectsWithTag("Fog");

        foreach (var obj in fogObjects)
        {
            var ps = obj.GetComponent<ParticleSystem>();
            if (ps == null) continue;

            fogSystems.Add(new FogData
            {
                system = ps,
                particles = new ParticleSystem.Particle[ps.main.maxParticles]
            });
        }

        lastPosition = transform.position;
    }

    void LateUpdate()
    {
        if (fogSystems.Count == 0) return;

        // Player velocity (XZ only)
        Vector3 velocity = (transform.position - lastPosition) / Time.deltaTime;
        lastPosition = transform.position;
        float moveSpeed = new Vector2(velocity.x, velocity.z).magnitude;

        foreach (var fog in fogSystems)
        {
            var fogSystem = fog.system;
            if (fogSystem == null) continue;

            var shape = fogSystem.shape;

            // Guaranteed box shape
            Vector3 boxSize = shape.scale;
            Vector3 boxCenter = fogSystem.transform.position + shape.position;

            // +2 on extents as requested
            Vector3 halfExtents = (boxSize * 0.5f) + boxMargin + Vector3.one * 2f;

            // --- Player inside expanded box? ---
            Vector3 playerLocal = transform.position - boxCenter;

            bool inside =
                Mathf.Abs(playerLocal.x) <= halfExtents.x &&
                Mathf.Abs(playerLocal.y) <= halfExtents.y &&
                Mathf.Abs(playerLocal.z) <= halfExtents.z;

            // Skip this fog system entirely if player is outside
            if (!inside)
                continue;

            // Ensure particle buffer is large enough
            if (fog.particles.Length < fogSystem.main.maxParticles)
                fog.particles = new ParticleSystem.Particle[fogSystem.main.maxParticles];

            int count = fogSystem.GetParticles(fog.particles);

            for (int i = 0; i < count; i++)
            {
                Vector3 particlePos = fog.particles[i].position;

                // --- disturbance from player ---
                if (moveSpeed > 0.01f)
                {
                    Vector3 toParticle = particlePos - transform.position;
                    toParticle.y = 0;

                    float dist = toParticle.magnitude;

                    if (dist < radius && dist > 0.001f)
                    {
                        float strength = 1f - (dist / radius);
                        fog.particles[i].velocity +=
                            toParticle.normalized * force * strength * (moveSpeed * 0.1f);
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

                float alphaFactor = 1f;

                if (distOutside > 0f)
                {
                    alphaFactor = Mathf.Clamp01(1f - (distOutside / fadeDistance));
                    fog.particles[i].velocity += diff * settleStrength * Time.deltaTime;
                }

                Color c = fog.particles[i].startColor;
                c.a *= alphaFactor;
                fog.particles[i].startColor = c;

                if (alphaFactor <= 0.001f)
                {
                    fog.particles[i].remainingLifetime = -1f;
                }

                fog.particles[i].velocity *= damping;
            }

            fogSystem.SetParticles(fog.particles, count);
        }
    }
}