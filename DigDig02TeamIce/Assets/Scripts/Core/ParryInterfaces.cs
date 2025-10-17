using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    public interface IHitbox
    {
        GameObject Owner { get; }
        bool CanBeParried { get; }
        Collider Collider { get; }
        LayerMask LayerMask { get; }
        int Damage { get; }
        bool UseMeshCollision { get; set; }
        void OnHit(IHurtbox target);
        void OnParried(IHurtbox by);
    }

    public interface IHurtbox
    {
        GameObject Owner { get; }
        Collider Collider { get; }
        LayerMask LayerMask { get; }
        bool UseMeshCollision { get; set; }
        void OnHit(IHitbox source);
        void TakeDamage(int damage);
    }
}