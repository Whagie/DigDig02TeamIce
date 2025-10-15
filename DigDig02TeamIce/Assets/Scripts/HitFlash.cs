using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitFlash : MonoBehaviour
{
    [SerializeField] private Enemy Owner;
    [SerializeField] private float Timespan = 0.1f;
    [SerializeField] private Color Tint = Color.white;
    private Renderer rend;
    private Material[] originalMats;
    private static Material hitFlash;
    private readonly string hitFlashResourcePath = "Materials/HitFlash";

    void Start()
    {
        var obj = gameObject.GetComponentInParent<Enemy>(true);
        if (obj != null)
        {
            Owner = obj;
        }
        Owner.ChildrenWithFlashEffect.Add(this);

        rend = GetComponent<Renderer>();
        originalMats = rend.sharedMaterials;
        if (hitFlash == null)
        {
            hitFlash = Resources.Load<Material>(hitFlashResourcePath);
            if (hitFlash == null)
            {
                Debug.LogWarning($"Failed to load Hit Flash at Resources/{hitFlashResourcePath}");
            }
        }
        hitFlash.color = Tint;
    }

    public void Flash()
    {
        hitFlash.color = Tint;
        // Apply your temporary override
        rend.material = hitFlash;

        // Wait a moment, then restore
        StartCoroutine(RestoreAfterDelay(rend, originalMats, Timespan));
    }

    IEnumerator RestoreAfterDelay(Renderer rend, Material[] original, float delay)
    {
        yield return new WaitForSeconds(delay);
        rend.sharedMaterials = original;
    }
}
