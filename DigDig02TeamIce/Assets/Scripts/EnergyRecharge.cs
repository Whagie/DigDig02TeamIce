using Game.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnergyRecharge : MonoBehaviour, IHurtbox
{
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
    private bool colorFaded = false;

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
        origEnergyAmount = energyAmount;

        crystalMaterials = new Material[crystalRenderers.Length];
        for (int i = 0; i < crystalRenderers.Length; i++)
        {
            crystalMaterials[i] = crystalRenderers[i].material;
        }

        origCrystalBaseColor = crystalMaterials[0].GetColor("_BaseColor");
        origCrystalTopColor = crystalMaterials[0].GetColor("_TopColor");
    }

    private void Update()
    {
        if (colorFaded && UserInput.InteractPressed)
        {
            Restore();
        }
    }

    public void OnHit(IHitbox source)
    {
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
        if (energyAmount <= 0)
            return;

        player.GiveEnergy();
        ParticleSpawner.SpawnEnergy(transform);
        energyAmount--;

        if (energyAmount <= 0)
        {
            Depleted = true;
            if (depletedRoutine == null)
                depletedRoutine = StartCoroutine(DepletedRoutine());
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

        colorFaded = true;
        depletedRoutine = null;
    }

    public void Restore()
    {
        energyAmount = origEnergyAmount;

        for (int i = 0; i < crystalMaterials.Length; i++)
        {
            crystalMaterials[i].SetColor("_BaseColor", origCrystalBaseColor);
            crystalMaterials[i].SetColor("_TopColor", origCrystalTopColor);
        }

        Depleted = false;
        colorFaded = false;
    }
}
