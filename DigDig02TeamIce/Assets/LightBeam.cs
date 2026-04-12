using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class LightBeam : MonoBehaviour
{
    [Header("Beam")]
    public LineRenderer Line;
    public float MaxDistance = 1000f;
    public LayerMask ReflectorMask;
    public LayerMask ObstacleMask;

    [Header("Prefab")]
    public GameObject LightBeamPrefab;

    [Header("Visuals")]
    public Material BeamMaterial;
    public float BeamWidth = 0.05f;

    private LightBeam ParentBeam;
    private List<LightBeam> children = new List<LightBeam>();

    private LightReflectorCrystal currentCrystal;

    private LightReceiver receiver;

    private GameObject hitEffect;
    private GameObject reflectorEffect;

    private static RaycastHit[] hitBuffer = new RaycastHit[32];

    private HashSet<LightPassThrough> currentPassThroughs = new HashSet<LightPassThrough>();
    private HashSet<LightPassThrough> newPassThroughs = new HashSet<LightPassThrough>();

    private void Awake()
    {
        if (ReflectorMask == 0)
            ReflectorMask = LayerMask.GetMask("LightReflector");

        if (LightBeamPrefab == null)
            LightBeamPrefab = Resources.Load<GameObject>("Light");

        if (BeamMaterial == null)
            BeamMaterial = Resources.Load<Material>("Materials/HitFlash");
    }

    private void OnEnable()
    {
        if (!TryGetComponent(out Line))
            Line = gameObject.AddComponent<LineRenderer>();

        Line.positionCount = 2;
        Line.material = BeamMaterial;
        Line.startWidth = BeamWidth;
        Line.endWidth = BeamWidth;
    }

    private void Update()
    {
        UpdateBeam();
    }

    void UpdateBeam()
    {
        Vector3 start = transform.position;
        Vector3 dir = transform.forward;

        Vector3 hitPos = start + dir * MaxDistance;

        LightReflectorCrystal crystal = null;
        GameObject hitObject = null;

        newPassThroughs.Clear();

        int hitCount = Physics.RaycastNonAlloc(
            start,
            dir,
            hitBuffer,
            MaxDistance,
            ReflectorMask | ObstacleMask,
            QueryTriggerInteraction.Ignore
        );

        // Sort hits by distance
        System.Array.Sort(hitBuffer, 0, hitCount, Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance)));

        if (hitCount == hitBuffer.Length)
            Debug.LogWarning("LightBeam hit buffer full, consider increasing size.");

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = hitBuffer[i];
            GameObject obj = hit.collider.gameObject;

            // PASS THROUGH CRYSTAL
            if (obj.CompareTag("LightPassThrough"))
            {
                LightPassThrough pass = hit.collider.GetComponentInParent<LightPassThrough>();

                if (pass != null)
                {
                    pass.ReceivingLight = true;
                    newPassThroughs.Add(pass);
                }

                // continue beam
                continue;
            }

            // REFLECTOR
            crystal = hit.collider.GetComponent<LightReflectorCrystal>();
            if (crystal != null)
            {
                if (crystal.ParentReflector.Rotating)
                {
                    crystal = null;
                    continue;
                }
                else
                {
                    hitPos = hit.point;
                    hitObject = obj;
                    break;
                }
            }

            // LIGHT RECEIVER
            if (obj.CompareTag("LightReceiver"))
            {
                hitPos = hit.point;
                hitObject = obj;
                break;
            }

            // OBSTACLE
            if (((1 << obj.layer) & ObstacleMask) != 0)
            {
                hitPos = hit.point;
                hitObject = obj;
                break;
            }
        }

        // Disable crystals no longer hit
        foreach (var pass in currentPassThroughs)
        {
            if (!newPassThroughs.Contains(pass))
                pass.ReceivingLight = false;
        }

        currentPassThroughs.Clear();

        foreach (var pass in newPassThroughs)
            currentPassThroughs.Add(pass);

        // ----- LIGHT RECEIVER LOGIC -----

        if (hitObject != null && hitObject.CompareTag("LightReceiver"))
        {
            var newReceiver = hitObject.GetComponentInParent<LightReceiver>();

            if (newReceiver != null)
            {
                newReceiver.RegisterLightHit();
                receiver = newReceiver;
            }
            else
            {
                receiver = null;
            }
        }
        else
        {
            receiver = null;
        }

        Line.SetPosition(0, start);
        Line.SetPosition(1, hitPos);

        HandleParticles(hitPos, dir, crystal);

        if (crystal != null && receiver == null)
        {
            if (crystal.OccupyingBeam != null && crystal.OccupyingBeam != this)
            {
                DestroyChildren();
                ReleaseCrystal();
                return;
            }

            // Release previous crystal if different
            if (currentCrystal != null && currentCrystal != crystal)
            {
                if (currentCrystal.OccupyingBeam == this)
                    currentCrystal.OccupyingBeam = null;
            }

            // Assign new
            crystal.OccupyingBeam = this;
            currentCrystal = crystal;

            LightPuzzleReflector reflector = crystal.ParentReflector;
            Transform exit = reflector.GetExit(crystal);

            LightBeam child = children.Count > 0 ? children[0] : null;

            if (child == null)
                SpawnChildBeam(exit.position, exit.rotation);
            else
            {
                child.transform.position = exit.position;
                child.transform.rotation = exit.rotation;
            }
        }
        else
        {
            DestroyChildren();
            ReleaseCrystal();
        }

        children.RemoveAll(c => c == null);
    }


    void SpawnChildBeam(Vector3 pos, Quaternion rot)
    {
        GameObject obj = Instantiate(LightBeamPrefab, pos, rot);
        LightBeam beam = obj.GetComponent<LightBeam>();

        beam.ParentBeam = this;

        children.Add(beam);
    }

    void DestroyChildren()
    {
        foreach (var c in children)
            if (c != null)
                c.DestroyBeamRecursive();

        children.Clear();
    }

    void ReleaseCrystal()
    {
        if (currentCrystal != null)
        {
            if (currentCrystal.OccupyingBeam == this)
                currentCrystal.OccupyingBeam = null;

            currentCrystal = null;
        }
    }

    public void DestroyBeamRecursive()
    {
        ReleaseCrystal();
        ReleasePassThroughs();
        DestroyChildren();

        if (hitEffect != null) Destroy(hitEffect);
        if (reflectorEffect != null) Destroy(reflectorEffect);

        if (ParentBeam != null)
            Destroy(gameObject);
    }

    void ReleasePassThroughs()
    {
        foreach (var pass in currentPassThroughs)
        {
            if (pass != null)
                pass.ReceivingLight = false;
        }

        currentPassThroughs.Clear();
        newPassThroughs.Clear();
    }

    void HandleParticles(Vector3 hitPos, Vector3 forward, LightReflectorCrystal crystal)
    {
        bool hittingReflector = crystal != null;

        if (hittingReflector)
        {
            if (reflectorEffect == null)
                reflectorEffect = Instantiate(Particles.P_LightBeamReflectorHit, hitPos, Quaternion.LookRotation(forward));
            else
            {
                reflectorEffect.SetActive(true);
                reflectorEffect.transform.position = hitPos;
            }

            if (hitEffect != null)
                hitEffect.SetActive(false);
        }
        else
        {
            if (hitEffect == null)
                hitEffect = Instantiate(Particles.P_LightBeamHit, hitPos, Quaternion.LookRotation(forward));
            else
            {
                hitEffect.SetActive(true);
                hitEffect.transform.position = hitPos;
            }

            if (reflectorEffect != null)
                reflectorEffect.SetActive(false);
        }
    }
}