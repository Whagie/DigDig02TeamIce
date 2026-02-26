using UnityEngine;

public class ParticleControl : MonoBehaviour
{
    private ParticleSystem[] systems;

    void Awake()
    {
        // Grab all ParticleSystems in children
        systems = GetComponentsInChildren<ParticleSystem>();
    }

    void Update()
    {
        // Check if any child system is still alive
        foreach (var ps in systems)
        {
            if (ps != null && ps.IsAlive(true))
                return;
        }

        // All systems are dead -> destroy parent
        Destroy(gameObject);
    }
}
