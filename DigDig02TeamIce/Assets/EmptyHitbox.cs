using Game.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmptyHitbox : MonoBehaviour, IHitbox
{
    public GameObject Owner => gameObject;
    public Collider Collider { get; set; }
    public bool UseMeshCollision { get; set; } = false;
    public bool CanBeParried { get; set; } = false;
    public LayerMask LayerMask { get; set; }
    public int Damage { get; set; } = 0;

    [SerializeField] private bool canBeParried = false;
    [SerializeField] private bool useMeshCollision = false;
    [SerializeField] private Collider mainCollider;
    [SerializeField] private LayerMask layerMask;
    [SerializeField] private int damage = 0;

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
        CanBeParried = canBeParried;
        UseMeshCollision = useMeshCollision;
        Collider = mainCollider;
        LayerMask = layerMask;
        Damage = damage;
    }

    public void OnHit(IHurtbox source)
    {

    }

    public void OnParried(IHurtbox source)
    {

    }
}
