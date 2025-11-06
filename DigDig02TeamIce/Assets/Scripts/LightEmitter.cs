using System.Collections.Generic;
using UnityEngine;

public class LightEmitter : MonoBehaviour
{
    [Header("Beam Settings")]
    public LineRenderer _lineRenderer;
    public GameObject LightObject; // Prefab containing LightEmitter + LineRenderer
    public LayerMask LayerMask;
    public float MaxDistance = 1000f;

    [Header("Hierarchy")]
    public LightEmitter FromEmitter; // Parent beam
    private List<LightEmitter> _spawnedChildren = new List<LightEmitter>();

    [Header("Appearance")]
    public Material BeamMaterial;
    public float BeamWidth = 0.05f;

    private LightReceiver receiver;
    private bool hitReflectorObject = false;

    private static GameObject p_lightBeamHit;
    private GameObject activeHitEffect;

    private static GameObject p_lightBeamReflectorHit;
    private GameObject activeReflectorEffect;

    private void Awake()
    {
        if (LayerMask == 0)
            LayerMask = LayerMask.GetMask("LightReflector");

        if (LightObject == null)
            LightObject = Resources.Load<GameObject>("Light");

        if (BeamMaterial == null)
            BeamMaterial = Resources.Load<Material>("Materials/HitFlash");

        if (p_lightBeamHit == null)
            p_lightBeamHit = Resources.Load<GameObject>("Particles/ParticleEffects/P_LightBeamHit");

        if (p_lightBeamReflectorHit == null)
            p_lightBeamReflectorHit = Resources.Load<GameObject>("Particles/ParticleEffects/P_LightBeamReflectorHit");
    }

    private void OnEnable()
    {
        if (TryGetComponent(out LineRenderer lr))
            _lineRenderer = lr;
        else
            _lineRenderer = gameObject.AddComponent<LineRenderer>();

        _lineRenderer.positionCount = 2;
        _lineRenderer.material = BeamMaterial;
        _lineRenderer.startWidth = BeamWidth;
        _lineRenderer.endWidth = BeamWidth;
    }

    private void Update()
    {
        UpdateBeam();
    }

    private void UpdateBeam()
    {
        hitReflectorObject = false; // reset every frame

        Vector3 startPos = transform.position;
        Vector3 forward = transform.forward;
        Vector3 hitPos = startPos + forward * MaxDistance;
        GameObject hitReflector = null;
        Vector3 hitNormal = Vector3.forward;

        if (Physics.Raycast(startPos, forward, out RaycastHit hit, MaxDistance, LayerMask))
        {
            hitPos = hit.point;
            hitReflector = hit.collider.gameObject;
            hitNormal = hit.normal;

            // Mark reflector hit if it's on the reflector layer
            if (hitReflector.layer == LayerMask.NameToLayer("LightReflector"))
                hitReflectorObject = true;

            // Skip if we hit our parent emitter
            if (FromEmitter != null && hitReflector == FromEmitter.gameObject)
                hitReflector = null;
        }

        // Update beam line
        _lineRenderer.SetPosition(0, startPos);
        _lineRenderer.SetPosition(1, hitPos);

        // --- Particle hit effects ---
        bool hitIsReflector = hitReflectorObject && hitReflector != null;
        bool hitIsReceiver = hitIsReflector && hitReflector.CompareTag("LightReceiver");

        if (hitIsReflector && !hitIsReceiver)
        {
            // Regular reflector (not a LightReceiver) -> use reflector hit particle
            if (p_lightBeamReflectorHit != null)
            {
                if (activeReflectorEffect == null)
                {
                    activeReflectorEffect = Instantiate(p_lightBeamReflectorHit, hitPos, Quaternion.LookRotation(forward), this.transform);
                }
                else
                {
                    if (!activeReflectorEffect.activeSelf)
                        activeReflectorEffect.SetActive(true);

                    activeReflectorEffect.transform.position = hitPos;
                    activeReflectorEffect.transform.rotation = Quaternion.LookRotation(forward);
                }
            }

            // Disable normal hit effect
            if (activeHitEffect != null)
                activeHitEffect.SetActive(false);
        }
        else
        {
            // Anything else (including LightReceiver) -> use normal hit particle
            if (p_lightBeamHit != null)
            {
                if (activeHitEffect == null)
                {
                    activeHitEffect = Instantiate(p_lightBeamHit, hitPos, Quaternion.LookRotation(forward), this.transform);
                }
                else
                {
                    if (!activeHitEffect.activeSelf)
                        activeHitEffect.SetActive(true);

                    activeHitEffect.transform.position = hitPos;
                    activeHitEffect.transform.rotation = Quaternion.LookRotation(forward);
                }
            }

            // Disable reflector hit effect
            if (activeReflectorEffect != null)
                activeReflectorEffect.SetActive(false);
        }

        // Handle receiver logic
        if (hitReflector != null && hitReflector.CompareTag("LightReceiver"))
        {
            // Hit a LightReceiver: activate it but stop the beam chain here
            receiver = hitReflector.GetComponent<LightReceiver>();
            if (receiver != null) receiver.ReceivingLight = true;

            // Stop any reflected beams — no bounce
            DestroyAllChildren();
            _spawnedChildren.Clear();
            return;
        }
        else if (receiver != null)
        {
            receiver.ReceivingLight = false;
            receiver = null;
        }

        // Handle children (reflections)
        if (hitReflectorObject && hitReflector != null)
        {
            LightEmitter child = _spawnedChildren.Count > 0 ? _spawnedChildren[0] : null;
            Vector3 reflectDir = Vector3.Reflect(forward, hitNormal);

            if (child == null)
            {
                SpawnChildBeam(hitReflector, hitPos, reflectDir);
            }
            else
            {
                child.transform.position = hitPos;
                child.transform.rotation = Quaternion.LookRotation(reflectDir);
            }
        }
        else
        {
            // If not hitting a valid reflector, destroy children
            DestroyAllChildren();
        }

        // Cleanup
        _spawnedChildren.RemoveAll(c => c == null);
    }

    private void SpawnChildBeam(GameObject reflector, Vector3 position, Vector3 direction)
    {
        if (LightObject == null)
        {
            Debug.LogWarning("LightObject prefab not assigned!");
            return;
        }

        GameObject childObj = Instantiate(LightObject, position, Quaternion.LookRotation(direction), reflector.transform);
        LightEmitter childEmitter = childObj.GetComponent<LightEmitter>();
        if (childEmitter != null)
            childEmitter.FromEmitter = this;

        _spawnedChildren.Add(childEmitter);
    }

    private void DestroyAllChildren()
    {
        foreach (var child in _spawnedChildren)
            if (child != null)
                child.DestroyBeamRecursive();

        _spawnedChildren.Clear();
    }

    public void DestroyBeamRecursive()
    {
        DestroyAllChildren();

        if (activeHitEffect != null)
            Destroy(activeHitEffect);
        if (activeReflectorEffect != null)
            Destroy(activeReflectorEffect);

        if (FromEmitter != null)
            Destroy(gameObject);
    }
}
