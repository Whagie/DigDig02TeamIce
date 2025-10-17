using Game.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeAttack : MonoBehaviour, IHitbox
{
    public bool CanBeParried => true;
    public GameObject Owner => gameObject;
    public Enemy EnemyOwner;

    public Collider hitCollider;
    public Collider Collider => hitCollider;
    public bool UseMeshCollision { get; set; } = false;
    public LayerMask LayerMask { get; set; }

    public Color gizmoColor = Color.blue;

    public int Damage { get; set; } = 1;

    public bool active;

    public bool PlayerAttack = false;
    public bool DestroyOnHit = false;

    private void Start()
    {
        //if (PlayerAttack)
        //{
        //    LayerMask = LayerMask.GetMask("Player");
        //}
        //else
        //{
        //    LayerMask = LayerMask.GetMask("Enemy");
        //}
    }

    public void Activate()
    {
        if (active) return;
        active = true;
        HitboxManager.Register(this);
        Collider.enabled = true;
        //StartCoroutine(DeactivateAfter(duration));
    }

    private IEnumerator DeactivateAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        Deactivate();
    }

    public void Deactivate()
    {
        if (!active) return;
        active = false;
        HitboxManager.Unregister(this);
        Collider.enabled = false;
    }

    public virtual void OnHit(IHurtbox target)
    {
        target.OnHit(this);
        if (DestroyOnHit)
        {
            Deactivate();
            Destroy(gameObject);
        }
    }

    public void OnParried(IHurtbox by)
    {
        EnemyOwner.HandleParried(by);
    }
    //void OnDrawGizmos()
    //{
    //    Color prevColor = Gizmos.color;
    //    Gizmos.color = gizmoColor;
    //    Gizmos.DrawWireCube(Collider.bounds.center, Collider.bounds.size);
    //    Gizmos.color = prevColor;
    //}
}
