using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class WallCrack : MonoBehaviourID
{
    [SerializeField] private GameObject Surface;
    [SerializeField] private bool liveTextureUpdate = false;

    [SerializeField] private Vector2 tiling = new Vector2(0.2f, 0.2f);
    [SerializeField] private Vector2 offset = new Vector2(0.7f, 0.1f);
    [SerializeField] private float parallaxStrength = -3.5f;
    [SerializeField] private Color glowColor = new Color(255f, 248f, 191f, 0f);
    [SerializeField] private float glowIntensity = 1.5f;
    [SerializeField] private float crackNormalStrength = 1.5f;

    [SerializeField] private Material wallCrackMaterial;
    private Material crackMaterial;

    private Vector3 center;

    private MaterialPropertyBlock mpb;
    private Renderer cachedRenderer;

    public bool ExplodeOnLoad;
    public float explodeOnLoadTimer = 0.2f;

    private SessionSaveData.SingleBoolData destroyedData;

    private void Awake()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        Vector3 localCenter = mf.sharedMesh.bounds.center;
        center = transform.TransformPoint(localCenter);
    }
    private void Start()
    {
        if (SessionSaveData.Instance.TryGet(ID, out destroyedData))
        {
            if (destroyedData.IsTrue)
            {
                Destroy(gameObject);
            }
        }
        else
        {
            SessionSaveData.Instance.AddOrUpdateData(ID, false);
        }

        if (ExplodeOnLoad)
        {
            StartCoroutine(WaitAndExplodeOnLoadRoutine(explodeOnLoadTimer));
        }

        if (wallCrackMaterial != null)
        {
            wallCrackMaterial = new Material(wallCrackMaterial);
        }
        if (crackMaterial != null)
        {
            crackMaterial = new Material(crackMaterial);
        }

        ApplyProperties(true);
    }
    private void ApplyProperties(bool runtime = false)
    {
        if (!Surface) return;

        if (!cachedRenderer)
            cachedRenderer = GetComponent<Renderer>();

        if (mpb == null)
            mpb = new MaterialPropertyBlock();

        mpb.Clear();

        var surfaceRenderer = Surface.GetComponent<Renderer>();
        if (!surfaceRenderer) return;

        Material surfaceMat;
        if (runtime)
        {
            surfaceMat = surfaceRenderer.material;
        }
        else
        {
            surfaceMat= surfaceRenderer.sharedMaterial;
        }

        mpb.SetTexture("_WallTexture", surfaceMat.mainTexture);
        mpb.SetColor("_BaseColor", surfaceMat.color);
        mpb.SetTexture("_WallNormalTexture", surfaceMat.GetTexture("_BumpMap"));
        mpb.SetFloat("_NormalStrength", surfaceMat.GetFloat("_BumpScale"));

        if (HasMetallicTexture(surfaceMat))
        {
            mpb.SetFloat("_UseMetallicTex", 1f);
            mpb.SetTexture("_WallMetallicTexture", surfaceMat.GetTexture("_MetallicGlossMap"));
        }
        else
        {
            mpb.SetFloat("_UseMetallicTex", 0f);
            mpb.SetFloat("_MetallicValue", surfaceMat.GetFloat("_Metallic"));
        }
        mpb.SetFloat("_Smoothness", surfaceMat.GetFloat("_Smoothness"));

        if (HasOcclusionTexture(surfaceMat))
        {
            mpb.SetFloat("_UseOcclusion", 1f);
            mpb.SetTexture("_AmbientOcclusionTexture", surfaceMat.GetTexture("_OcclusionMap"));
            mpb.SetFloat("_AmbientOcclusionValue", surfaceMat.GetFloat("_OcclusionStrength"));
        }
        else
        {
            mpb.SetFloat("_UseOcclusion", 0f);
        }

        if (surfaceMat.IsKeywordEnabled("_EMISSION"))
        {
            mpb.SetFloat("_UseEmission", 1f);
            mpb.SetTexture("_EmissionTexture", surfaceMat.GetTexture("_EmissionMap"));
            mpb.SetColor("_EmissionColor", surfaceMat.GetColor("_EmissionColor"));
        }
        else
        {
            mpb.SetFloat("_UseEmission", 0f);
        }

        mpb.SetVector("_WallTextureTiling", surfaceMat.mainTextureScale);
        mpb.SetVector("_WallTextureOffset", surfaceMat.mainTextureOffset);

        mpb.SetVector("_Tiling", tiling);
        mpb.SetVector("_Offset", offset);
        mpb.SetFloat("_ParallaxStrength", parallaxStrength);
        mpb.SetFloat("_GlowIntensity", glowIntensity);
        mpb.SetFloat("_CrackNormalStrength", crackNormalStrength);
        mpb.SetColor("_GlowColor", glowColor);

        cachedRenderer.SetPropertyBlock(mpb);
    }

    private new void OnValidate()
    {
        EnsureMaterial();
        ApplyProperties();
    }

    private void Update()
    {
        if (liveTextureUpdate)
            ApplyProperties(true);
    }

    private void EnsureMaterial()
    {
        if (!TryGetComponent(out Renderer renderer))
            return;

        if (renderer.sharedMaterial != wallCrackMaterial)
            renderer.sharedMaterial = wallCrackMaterial;
    }

    bool HasMetallicTexture(Material mat)
    {
        return mat.HasProperty("_MetallicGlossMap") &&
               mat.GetTexture("_MetallicGlossMap") != null;
    }
    bool HasOcclusionTexture(Material mat)
    {
        return mat.HasProperty("_OcclusionMap") &&
               mat.GetTexture("_OcclusionMap") != null;
    }

    public void Break()
    {
        SessionSaveData.Instance.AddOrUpdateData(ID, true);
        ParticleSpawner.Spawn(Particles.P_BreakableWall, center, Quaternion.Euler(transform.eulerAngles.x, 180f, transform.eulerAngles.z));
        Destroy(this.gameObject, 0.075f);
    }

    private IEnumerator WaitAndExplodeOnLoadRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        Break();
    }
}
