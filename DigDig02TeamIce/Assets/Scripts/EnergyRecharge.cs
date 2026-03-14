using Game.Core;
using System.Collections;
using UnityEngine;

public class EnergyRecharge : MonoBehaviourID, IHurtbox
{
    private SessionSaveData.EnergyRechargeData RechargeData;
    public GameObject Owner => gameObject;
    public Collider Collider { get; set; }
    public bool UseMeshCollision { get; set; } = false;
    public LayerMask LayerMask { get; set; }

    [SerializeField] private bool useMeshCollision = false;
    [SerializeField] private Collider mainCollider;
    [SerializeField] private LayerMask layerMask;

    [SerializeField] private Renderer[] crystalRenderers;

    private Player player;

    private Material[] crystalMaterials;
    private Color origCrystalBaseColor;
    private Color origCrystalTopColor;

    private Color depletedBaseColor = new Color32(51, 51, 128, 255);
    private Color depletedTopColor = new Color32(92, 113, 153, 255);

    private Coroutine depletedRoutine;

    public int energyAmount = 8;
    private int origEnergyAmount;

    public bool Depleted = false;

    public bool ShouldResetOnLoad = true;

    private void OnEnable()
    {
        HitboxManager.Register(this);
    }

    private void OnDisable()
    {
        HitboxManager.Unregister(this);
    }

    private void Start()
    {
        UseMeshCollision = useMeshCollision;
        Collider = mainCollider;
        LayerMask = layerMask;

        player = GameObject.FindObjectOfType<Player>();

        crystalMaterials = new Material[crystalRenderers.Length];
        for (int i = 0; i < crystalRenderers.Length; i++)
        {
            crystalMaterials[i] = crystalRenderers[i].material;
        }

        origCrystalBaseColor = crystalMaterials[0].GetColor("_BaseColor");
        origCrystalTopColor = crystalMaterials[0].GetColor("_TopColor");

        if (!ShouldResetOnLoad)
        {
            if (SessionSaveData.Instance.TryGet(ID, out RechargeData))
            {
                energyAmount = RechargeData.HitsLeft;
                if (energyAmount <= 0)
                {
                    Depleted = true;
                    for (int i = 0; i < crystalMaterials.Length; i++)
                    {
                        crystalMaterials[i].SetColor("_BaseColor", depletedBaseColor);
                        crystalMaterials[i].SetColor("_TopColor", depletedTopColor);
                    }
                }
            }
            else
            {
                origEnergyAmount = energyAmount;
                SessionSaveData.Instance.AddOrUpdateData(ID, energyAmount, origEnergyAmount);
            }
        }
    }

    public void OnHit(IHitbox source)
    {
        if (energyAmount <= 0)
            return;

        if (player == null)
            player = GameObject.FindObjectOfType<Player>();

        if (player != null)
            SendEnergy();
    }

    public void TakeDamage(int amount)
    {
    }

    private void SendEnergy()
    {
        player.GiveEnergy();
        ParticleSpawner.SpawnEnergy(transform);
        energyAmount--;

        if (energyAmount <= 0)
        {
            Depleted = true;
            if (depletedRoutine == null)
                depletedRoutine = StartCoroutine(DepletedRoutine());
        }

        if (!ShouldResetOnLoad)
        {
            SessionSaveData.Instance.AddOrUpdateData(ID, energyAmount, origEnergyAmount);
        }
    }

    private IEnumerator DepletedRoutine()
    {
        float time = 0f;
        const float duration = 0.6f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            Color baseColor = Color.Lerp(origCrystalBaseColor, depletedBaseColor, t);
            Color topColor = Color.Lerp(origCrystalTopColor, depletedTopColor, t);

            for (int i = 0; i < crystalMaterials.Length; i++)
            {
                crystalMaterials[i].SetColor("_BaseColor", baseColor);
                crystalMaterials[i].SetColor("_TopColor", topColor);
            }

            yield return null;
        }

        for (int i = 0; i < crystalMaterials.Length; i++)
        {
            crystalMaterials[i].SetColor("_BaseColor", depletedBaseColor);
            crystalMaterials[i].SetColor("_TopColor", depletedTopColor);
        }

        depletedRoutine = null;
    }

    public void Restore()
    {
        Depleted = false;
        energyAmount = origEnergyAmount;

        for (int i = 0; i < crystalMaterials.Length; i++)
        {
            crystalMaterials[i].SetColor("_BaseColor", origCrystalBaseColor);
            crystalMaterials[i].SetColor("_TopColor", origCrystalTopColor);
        }
    }
}
