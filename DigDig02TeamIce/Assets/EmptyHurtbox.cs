using Game.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmptyHurtbox : MonoBehaviour, IHurtbox
{
    public GameObject Owner => gameObject;
    public Collider Collider { get; set; }
    public bool UseMeshCollision { get; set; } = false;
    public LayerMask LayerMask { get; set; }

    [SerializeField] private bool useMeshCollision = false;
    [SerializeField] private Collider mainCollider;
    [SerializeField] private LayerMask layerMask;

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
    }

    public void OnHit(IHitbox source)
    {

    }

    public void TakeDamage(int amount)
    {

    }
}
